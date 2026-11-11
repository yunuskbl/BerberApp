using System.Collections.Concurrent;
using BerberApp.Application.Common.Interfaces;

namespace BerberApp.Application.Common.Services;

/// <summary>WPPConnect oturumunun kurtarma açısından anlamı.</summary>
public enum SessionCondition
{
    /// <summary>Gönderime hazır.</summary>
    Healthy,
    /// <summary>Tarayıcı açılıyor / WhatsApp Web yükleniyor.</summary>
    Starting,
    /// <summary>Oturum çalışıyor ama WhatsApp QR istiyor: cihaz telefondan düşmüş.</summary>
    AwaitingQr,
    /// <summary>Sunucuda tarayıcı çalışmıyor. Telefonda cihaz bağlıysa QR'sız geri getirilebilir.</summary>
    Closed,
    /// <summary>Durum okunamadı (WPPConnect'e ulaşılamadı ya da HTTP hatası).</summary>
    Unknown,
}

/// <summary>Bir kurtarma denemesinin sonucu.</summary>
public enum RecoveryOutcome
{
    /// <summary>Oturum QR'sız geri geldi.</summary>
    Recovered,
    /// <summary>WhatsApp QR istedi; elle yeniden bağlanmak gerekiyor.</summary>
    NeedsQr,
    /// <summary>Bekleme süresi içinde bağlanmadı; sonraki turda yeniden bakılacak.</summary>
    StillStarting,
    /// <summary>WPPConnect başlatma isteğini kabul etmedi.</summary>
    StartFailed,
}

public sealed record SessionRecoveryInfo(
    string Session,
    int FailedAttempts,
    DateTime? NextAttemptUtc,
    DateTime? NeedsQrSinceUtc,
    DateTime? LastAttemptUtc,
    RecoveryOutcome? LastOutcome);

/// <summary>
/// WhatsApp oturumlarının sessiz kurtarma durumu (bellekte, API ömrü boyunca).
///
/// Neden var: sunucudaki tarayıcı kapandığında (bellek sıkışması, yeniden
/// başlatma, çökme) oturum "CLOSED" görünüyor ama telefonda cihaz hâlâ bağlı
/// oluyor; aynı oturum yeniden başlatılırsa QR'sız geri geliyor. Bunu yapan
/// hiçbir şey yoktu: işletme "Bağlan"a bastığında eski cihaz WhatsApp'tan
/// tamamen düşürülüp yeni oturum açılıyor, QR kesin isteniyordu.
///
/// Bu sınıf kurtarma işinin (WhatsAppSessionRecoveryJob) kararlarını tutuyor:
/// hangi oturum ne zaman yeniden denenebilir, hangisi elle bağlanıyor olduğu
/// için rahat bırakılmalı, hangisi için WhatsApp QR istediğinden denemenin
/// boşuna olduğu. Tek API örneği çalıştığı için bellekte tutmak yeterli;
/// API yeniden başlarsa her oturum en fazla bir kez daha denenir.
/// </summary>
public sealed class WhatsAppSessionRecoveryState
{
    /// <summary>Elle bağlanma/kesme sırasında kurtarma işi bu süre oturuma dokunmaz.</summary>
    public static readonly TimeSpan ManualSuppression = TimeSpan.FromMinutes(10);

    /// <summary>WhatsApp QR istediyse bir sonraki deneme ancak bu kadar sonra.</summary>
    public static readonly TimeSpan NeedsQrRetryAfter = TimeSpan.FromHours(12);

    /// <summary>Kurtarma işinin başlattığı bir oturum bu süre içinde QR'a düşerse "QR gerekli" sayılır.</summary>
    public static readonly TimeSpan RecentAttemptWindow = TimeSpan.FromMinutes(15);

    /// <summary>"Başlıyor" durumunda bundan uzun kalan oturum takılmış sayılır.</summary>
    public static readonly TimeSpan StuckStartingAfter = TimeSpan.FromMinutes(15);

    /// <summary>Art arda başarısız denemeler arasındaki bekleme: 5, 10, 20, 40, 80, 120 dk.</summary>
    private static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromMinutes(5),  TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20),
        TimeSpan.FromMinutes(40), TimeSpan.FromMinutes(80), TimeSpan.FromMinutes(120),
    ];

    private static readonly string[] ClosedValues =
        ["CLOSED", "DISCONNECTED", "browserClose", "serverClose", "desconnectedMobile", "autocloseCalled"];

    private static readonly string[] QrValues = ["QRCODE", "notLogged"];

    private readonly Func<DateTime> _clock;
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public WhatsAppSessionRecoveryState() : this(() => DateTime.UtcNow) { }

    /// <param name="clock">UTC saat kaynağı; testlerde zamanı ilerletmek için.</param>
    public WhatsAppSessionRecoveryState(Func<DateTime> clock) => _clock = clock;

    /// <summary>WPPConnect durum değerini kurtarma kararı için sınıflar.</summary>
    public static SessionCondition Classify(WppConnectStatus status)
    {
        if (!status.IsReachable) return SessionCondition.Unknown;
        if (status.IsConnected) return SessionCondition.Healthy;
        if (QrValues.Contains(status.Value, StringComparer.OrdinalIgnoreCase)) return SessionCondition.AwaitingQr;
        if (ClosedValues.Contains(status.Value, StringComparer.OrdinalIgnoreCase)) return SessionCondition.Closed;
        if (string.Equals(status.Value, "UNKNOWN", StringComparison.OrdinalIgnoreCase)) return SessionCondition.Unknown;
        // INITIALIZING, STARTING ve WPPConnect'in ara durumları
        return SessionCondition.Starting;
    }

    /// <summary>
    /// Elle bağlanma, QR görüntüleme ya da bağlantı kesme başladı: kurtarma işi
    /// bu oturuma bir süre dokunmasın ve önceki başarısızlıklar unutulsun.
    /// </summary>
    public void Suppress(string? session)
    {
        if (string.IsNullOrWhiteSpace(session)) return;
        var e = Get(session);
        lock (e)
        {
            e.SuppressedUntilUtc = _clock() + ManualSuppression;
            e.FailedAttempts     = 0;
            e.NextAttemptUtc     = DateTime.MinValue;
            e.NeedsQrSinceUtc    = null;
            e.StartingSinceUtc   = null;
        }
    }

    public bool IsSuppressed(string session)
        => _entries.TryGetValue(session, out var e) && Locked(e, x => _clock() < x.SuppressedUntilUtc);

    /// <summary>Kapalı bir oturum için şimdi kurtarma denenebilir mi?</summary>
    public bool ShouldAttempt(string session)
    {
        if (!_entries.TryGetValue(session, out var e)) return true;
        var now = _clock();
        return Locked(e, x =>
            now >= x.SuppressedUntilUtc &&
            now >= x.NextAttemptUtc &&
            (x.NeedsQrSinceUtc is null || now - x.NeedsQrSinceUtc >= NeedsQrRetryAfter));
    }

    /// <summary>
    /// Bu oturumu kısa süre önce kurtarma işi mi başlattı (ve sonuç henüz
    /// belli olmadan mı kaldı)? Öyleyse QR'a düşmesi "cihaz telefondan düşmüş"
    /// demektir; değilse QR'ı büyük ihtimalle bağlanmakta olan kişi okutuyor.
    /// </summary>
    public bool StartedRecently(string session)
    {
        if (!_entries.TryGetValue(session, out var e)) return false;
        var now = _clock();
        return Locked(e, x =>
            x.LastOutcome == RecoveryOutcome.StillStarting &&
            x.LastAttemptUtc is { } at && now - at <= RecentAttemptWindow);
    }

    /// <summary>Oturum sağlıklı görüldü: başarısızlık ve "QR gerekli" kayıtları silinir.</summary>
    public void MarkHealthy(string session)
    {
        if (!_entries.TryGetValue(session, out var e)) return;
        lock (e)
        {
            e.FailedAttempts   = 0;
            e.NextAttemptUtc   = DateTime.MinValue;
            e.NeedsQrSinceUtc  = null;
            e.StartingSinceUtc = null;
        }
    }

    /// <summary>
    /// Oturum "başlıyor" durumunda görüldü. Ne kadar süredir bu durumda
    /// olduğunu döndürür; takılmış tarayıcıları ayırt etmek için.
    /// </summary>
    public TimeSpan NoteStarting(string session)
    {
        var e = Get(session);
        var now = _clock();
        lock (e)
        {
            e.StartingSinceUtc ??= now;
            return now - e.StartingSinceUtc.Value;
        }
    }

    /// <summary>"Başlıyor" sayacını sıfırlar (oturum başka bir duruma geçti ya da kapatıldı).</summary>
    public void ClearStarting(string session)
    {
        if (_entries.TryGetValue(session, out var e))
            lock (e) e.StartingSinceUtc = null;
    }

    /// <summary>Kurtarma denemesinin sonucunu kaydeder ve bir sonraki deneme zamanını belirler.</summary>
    public void Record(string session, RecoveryOutcome outcome)
    {
        var e = Get(session);
        var now = _clock();
        lock (e)
        {
            e.LastAttemptUtc   = now;
            e.LastOutcome      = outcome;
            e.StartingSinceUtc = null;

            switch (outcome)
            {
                case RecoveryOutcome.Recovered:
                    e.FailedAttempts  = 0;
                    e.NextAttemptUtc  = DateTime.MinValue;
                    e.NeedsQrSinceUtc = null;
                    break;

                case RecoveryOutcome.NeedsQr:
                    e.FailedAttempts  = 0;
                    e.NeedsQrSinceUtc = now;
                    e.NextAttemptUtc  = now + NeedsQrRetryAfter;
                    break;

                default:
                    e.FailedAttempts++;
                    e.NextAttemptUtc = now + Backoff[Math.Min(e.FailedAttempts, Backoff.Length) - 1];
                    break;
            }
        }
    }

    /// <summary>Artık hiçbir hatta ait olmayan oturumların kayıtlarını siler.</summary>
    public void Forget(IEnumerable<string> activeSessions)
    {
        var keep = new HashSet<string>(activeSessions, StringComparer.Ordinal);
        foreach (var key in _entries.Keys)
            if (!keep.Contains(key)) _entries.TryRemove(key, out _);
    }

    public IReadOnlyList<SessionRecoveryInfo> Snapshot()
        => _entries.Select(kv => Locked(kv.Value, e => new SessionRecoveryInfo(
                kv.Key,
                e.FailedAttempts,
                e.NextAttemptUtc == DateTime.MinValue ? null : e.NextAttemptUtc,
                e.NeedsQrSinceUtc,
                e.LastAttemptUtc,
                e.LastOutcome)))
            .ToList();

    private Entry Get(string session) => _entries.GetOrAdd(session, _ => new Entry());

    private static T Locked<T>(Entry e, Func<Entry, T> read)
    {
        lock (e) return read(e);
    }

    private sealed class Entry
    {
        public DateTime  SuppressedUntilUtc = DateTime.MinValue;
        public int       FailedAttempts;
        public DateTime  NextAttemptUtc = DateTime.MinValue;
        public DateTime? NeedsQrSinceUtc;
        public DateTime? LastAttemptUtc;
        public RecoveryOutcome? LastOutcome;
        public DateTime? StartingSinceUtc;
    }
}
