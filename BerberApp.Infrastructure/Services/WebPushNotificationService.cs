using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Settings;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BerberApp.Infrastructure.Services;

/// <summary>
/// Anlık bildirim gönderimi (Web Push, RFC 8030/8291/8292) — harici kütüphane yok.
///
/// Yaygın NuGet paketi eski bir BouncyCastle sürümüne bağlı; gereken her şey
/// .NET 8'de var: ECDH P-256 ile ortak sır, HKDF ile anahtar türetme,
/// AES-128-GCM ile şifreleme (aes128gcm içerik kodlaması) ve ES256 imzalı VAPID
/// kimliği. Bildirim içeriği Angular service worker'ın beklediği biçimde
/// ("notification" + tıklanınca açılacak sayfa).
/// </summary>
public sealed class WebPushNotificationService : IPushNotificationService
{
    private const int RecordSize = 4096;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private readonly IAppDbContext _db;
    private readonly HttpClient _http;
    private readonly WebPushSettings _cfg;
    private readonly ILogger<WebPushNotificationService> _log;

    public WebPushNotificationService(
        IAppDbContext db, HttpClient http, IOptions<WebPushSettings> cfg, ILogger<WebPushNotificationService> log)
    {
        _db = db; _http = http; _cfg = cfg.Value; _log = log;
    }

    public bool IsEnabled => _cfg.IsConfigured;

    public async Task<int> SendToTenantAsync(Guid tenantId, PushMessage message, Guid? staffId = null, CancellationToken ct = default)
    {
        if (!IsEnabled) return 0;
        var subs = await _db.PushSubscriptions
            .Include(s => s.User)
            .Where(s => s.TenantId == tenantId &&
                        (s.User.Role == UserRole.Admin || (staffId != null && s.User.StaffId == staffId)))
            .ToListAsync(ct);
        return await SendAllAsync(subs, message, ct);
    }

    public async Task<int> SendToUserAsync(Guid userId, PushMessage message, CancellationToken ct = default)
    {
        if (!IsEnabled) return 0;
        var subs = await _db.PushSubscriptions.Include(s => s.User).Where(s => s.UserId == userId).ToListAsync(ct);
        return await SendAllAsync(subs, message, ct);
    }

    /// <summary>
    /// Personel ayrı bir panel kullanıyor (/staff-panel). İşletme sahibinin
    /// adresi açılsa koruma onu panelin ana sayfasına atardı; bildirime
    /// dokununca doğrudan kendi randevu listesi açılsın.
    /// </summary>
    private static PushMessage ForRecipient(PushSubscription sub, PushMessage m)
        => sub.User?.Role == UserRole.Staff
            ? m with { Url = m.Url.StartsWith("/appointments") ? "/staff-panel/appointments" : "/staff-panel" }
            : m;

    private async Task<int> SendAllAsync(List<PushSubscription> subs, PushMessage message, CancellationToken ct)
    {
        if (subs.Count == 0) return 0;
        var payloads = new Dictionary<PushMessage, byte[]>();
        var delivered = 0;

        foreach (var sub in subs)
        {
            try
            {
                var m = ForRecipient(sub, message);
                if (!payloads.TryGetValue(m, out var payload))
                    payloads[m] = payload = BuildPayload(m);
                var status = await SendAsync(sub, payload, ct);
                if (status is HttpStatusCode.Created or HttpStatusCode.OK or HttpStatusCode.Accepted)
                {
                    sub.LastSuccessAt = DateTime.UtcNow;
                    sub.FailureCount = 0;
                    delivered++;
                }
                else if (status is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                {
                    // Kullanıcı izni kaldırdı ya da uygulamayı sildi: abonelik artık yok.
                    _db.PushSubscriptions.Remove(sub);
                    _log.LogInformation("[Push] Geçersiz abonelik silindi ({Status}).", (int)status);
                }
                else
                {
                    sub.FailureCount++;
                    _log.LogWarning("[Push] Gönderilemedi: HTTP {Status}", (int)status);
                }
            }
            catch (Exception ex)
            {
                sub.FailureCount++;
                _log.LogWarning(ex, "[Push] Gönderim hatası.");
            }
        }

        await _db.SaveChangesAsync(ct);
        return delivered;
    }

    /// <summary>Angular service worker biçimi: tıklanınca ilgili sayfa açılır ya da öne gelir.</summary>
    private static byte[] BuildPayload(PushMessage m)
    {
        var body = new
        {
            notification = new
            {
                title = m.Title,
                body = m.Body,
                icon = "/icon-maskable-192.png",
                badge = "/badge-96.png",   // Android durum çubuğu: yalnızca saydamlık kullanılır, beyaz işaret
                tag = m.Tag,
                renotify = m.Tag != null,
                data = new
                {
                    onActionClick = new
                    {
                        @default = new { operation = "navigateLastFocusedOrOpen", url = m.Url },
                    },
                },
            },
        };
        return JsonSerializer.SerializeToUtf8Bytes(body, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
    }

    private async Task<HttpStatusCode> SendAsync(PushSubscription sub, byte[] payload, CancellationToken ct)
    {
        var endpoint = new Uri(sub.Endpoint);
        var body = Encrypt(Base64Url.Decode(sub.P256dh), Base64Url.Decode(sub.Auth), payload);
        var audience = $"{endpoint.Scheme}://{endpoint.Authority}";

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Content = new ByteArrayContent(body);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        req.Content.Headers.ContentEncoding.Add("aes128gcm");
        req.Headers.TryAddWithoutValidation("TTL", ((int)Ttl.TotalSeconds).ToString());
        req.Headers.TryAddWithoutValidation("Urgency", "high");
        req.Headers.TryAddWithoutValidation("Authorization",
            $"vapid t={CreateVapidJwt(audience)}, k={_cfg.PublicKey}");

        using var res = await _http.SendAsync(req, ct);
        return res.StatusCode;
    }

    // ── VAPID (RFC 8292) ─────────────────────────────────────────────────────

    private string CreateVapidJwt(string audience)
    {
        var header = Base64Url.Encode(Encoding.UTF8.GetBytes("{\"typ\":\"JWT\",\"alg\":\"ES256\"}"));
        var exp = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds();
        var claims = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new { aud = audience, exp, sub = _cfg.Subject }));
        var signingInput = Encoding.ASCII.GetBytes($"{header}.{claims}");

        using var ecdsa = ECDsa.Create(VapidParameters());
        var sig = ecdsa.SignData(signingInput, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{header}.{claims}.{Base64Url.Encode(sig)}";
    }

    private ECParameters VapidParameters()
    {
        var pub = Base64Url.Decode(_cfg.PublicKey);
        return new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = Base64Url.Decode(_cfg.PrivateKey),
            Q = new ECPoint { X = pub[1..33], Y = pub[33..65] },
        };
    }

    // ── İçerik şifreleme: aes128gcm (RFC 8188 + RFC 8291) ────────────────────

    /// <summary>
    /// Tek kayıtlık aes128gcm gövdesi:
    /// salt(16) | kayıt boyu(4) | anahtar uzunluğu(1) | sunucu açık anahtarı(65) | şifreli metin + etiket.
    /// </summary>
    internal static byte[] Encrypt(byte[] uaPublic, byte[] authSecret, byte[] plaintext,
                                   ECDiffieHellman? serverKey = null, byte[]? salt = null)
    {
        if (uaPublic.Length != 65 || uaPublic[0] != 0x04)
            throw new ArgumentException("Geçersiz p256dh anahtarı.");
        if (authSecret.Length != 16)
            throw new ArgumentException("Geçersiz auth sırrı.");

        using var asKey = serverKey ?? ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var asParams = asKey.ExportParameters(false);
        var asPublic = new byte[65];
        asPublic[0] = 0x04;
        asParams.Q.X!.CopyTo(asPublic, 1);
        asParams.Q.Y!.CopyTo(asPublic, 33);

        using var uaKey = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = uaPublic[1..33], Y = uaPublic[33..65] },
        });
        var ecdhSecret = asKey.DeriveRawSecretAgreement(uaKey.PublicKey);

        // IKM = HKDF(auth_secret, ecdh_secret, "WebPush: info" || 0x00 || ua_public || as_public, 32)
        var keyInfo = Concat(Encoding.ASCII.GetBytes("WebPush: info\0"), uaPublic, asPublic);
        var ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, ecdhSecret, 32, authSecret, keyInfo);

        salt ??= RandomNumberGenerator.GetBytes(16);
        var cek   = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 16, salt, Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0"));
        var nonce = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 12, salt, Encoding.ASCII.GetBytes("Content-Encoding: nonce\0"));

        // Tek (son) kayıt: düz metin + 0x02 sınırlayıcı.
        var padded = new byte[plaintext.Length + 1];
        plaintext.CopyTo(padded, 0);
        padded[^1] = 0x02;
        if (padded.Length + 16 > RecordSize - 86)
            throw new ArgumentException("Bildirim içeriği çok uzun.");

        var cipher = new byte[padded.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(cek, 16))
            aes.Encrypt(nonce, padded, cipher, tag);

        var header = new byte[16 + 4 + 1 + 65];
        salt.CopyTo(header, 0);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(16, 4), RecordSize);
        header[20] = 65;
        asPublic.CopyTo(header, 21);

        return Concat(header, cipher, tag);
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var result = new byte[parts.Sum(p => p.Length)];
        var offset = 0;
        foreach (var p in parts) { p.CopyTo(result, offset); offset += p.Length; }
        return result;
    }
}

/// <summary>RFC 4648 §5 base64url (dolgusuz).</summary>
internal static class Base64Url
{
    public static string Encode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] Decode(string s)
    {
        var b = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(b.PadRight(b.Length + (4 - b.Length % 4) % 4, '='));
    }
}
