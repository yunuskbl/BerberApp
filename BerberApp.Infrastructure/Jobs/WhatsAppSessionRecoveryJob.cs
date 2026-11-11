using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Services;
using BerberApp.Application.Common.Settings;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BerberApp.Infrastructure.Jobs;

/// <summary>
/// WhatsApp oturumlarının sessiz kurtarılması — beş dakikada bir.
///
/// Sunucudaki tarayıcı kapandığında (bellek sıkışması, WPPConnect'in yeniden
/// başlaması, çökme) oturum "CLOSED" görünüyor. Telefonda cihaz hâlâ bağlıysa
/// aynı oturum aynı adla yeniden başlatıldığında kayıtlı tarayıcı profilinden
/// QR'sız geri geliyor. Bu iş bunu kimse bir şeye basmadan yapıyor:
///
///  1. Merkezi hat ve kendi numarasını bağlamış her aktif işletme taranır.
///  2. Kapalı olan oturum aynı adla başlatılır ve en fazla 90 sn izlenir.
///  3. Bağlanırsa iş biter. WhatsApp QR isterse cihaz telefondan düşmüştür:
///     tarayıcı kapatılır (QR'ı okutacak kimse yokken bellek yemesin) ve oturum
///     12 saat rahat bırakılır; paneldeki "WhatsApp hattı koptu" uyarısı
///     işletmeyi yeniden bağlanmaya yönlendirir.
///  4. "Başlıyor" durumunda 15 dakikadan uzun takılan tarayıcı kapatılır,
///     bir sonraki turda temiz başlatılır.
///
/// Elle bağlanma/kesme sürerken (QR ekranı açıkken) oturuma dokunulmaz —
/// <see cref="WhatsAppSessionRecoveryState.Suppress"/>.
/// </summary>
public class WhatsAppSessionRecoveryJob
{
    /// <summary>Bir turda en fazla bu kadar oturum yeniden başlatılır: her biri bir Chromium demek.</summary>
    private const int MaxRestoresPerRun = 3;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RestoreWait  = TimeSpan.FromSeconds(90);

    private readonly IAppDbContext _db;
    private readonly IWppConnectManagementService _wpp;
    private readonly IOptionsMonitor<WppConnectSettings> _cfg;
    private readonly WhatsAppSessionRecoveryState _state;
    private readonly ILogger<WhatsAppSessionRecoveryJob> _log;

    public WhatsAppSessionRecoveryJob(
        IAppDbContext db,
        IWppConnectManagementService wpp,
        IOptionsMonitor<WppConnectSettings> cfg,
        WhatsAppSessionRecoveryState state,
        ILogger<WhatsAppSessionRecoveryJob> log)
    {
        _db    = db;
        _wpp   = wpp;
        _cfg   = cfg;
        _state = state;
        _log   = log;
    }

    private sealed record Line(string Label, string Session, string Token);

    // Tur periyodik; başarısız turu Hangfire'ın yeniden denemesine gerek yok.
    // Bir tur 5 dakikayı aşarsa ikincisi üst üste binmesin.
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task RunAsync()
    {
        var lines = await CollectLinesAsync();
        _state.Forget(lines.Select(l => l.Session));

        var restores = 0;
        foreach (var line in lines)
        {
            if (_state.IsSuppressed(line.Session)) continue;

            var status = await _wpp.GetSessionStatusAsync(line.Session, line.Token);

            // WPPConnect'in kendisine ulaşılamıyorsa (kapalı / yeniden başlıyor)
            // bu turda hiçbir hattı yorma; ayağa kalkınca kendi oturumlarını açacak.
            if (status.Value == "UNREACHABLE")
            {
                _log.LogWarning("[WA-Recovery] WPPConnect'e ulaşılamıyor; bu tur atlandı.");
                return;
            }

            switch (WhatsAppSessionRecoveryState.Classify(status))
            {
                case SessionCondition.Healthy:
                    _state.MarkHealthy(line.Session);
                    break;

                case SessionCondition.Starting:
                    var startingFor = _state.NoteStarting(line.Session);
                    if (startingFor >= WhatsAppSessionRecoveryState.StuckStartingAfter)
                    {
                        _log.LogWarning(
                            "[WA-Recovery] {Label} {Minutes:F0} dakikadır \"{Status}\" durumunda takılı; tarayıcı kapatılıyor, sonraki turda yeniden başlatılacak ({Session}).",
                            line.Label, startingFor.TotalMinutes, status.Value, line.Session);
                        await CloseQuietlyAsync(line);
                        _state.ClearStarting(line.Session);
                    }
                    break;

                case SessionCondition.AwaitingQr:
                    // QR'ı biz başlatmadıysak büyük ihtimalle biri şu an okutuyor.
                    if (_state.StartedRecently(line.Session))
                        await GiveUpAsync(line);
                    break;

                case SessionCondition.Closed:
                    _state.ClearStarting(line.Session);
                    if (!_state.ShouldAttempt(line.Session) || restores >= MaxRestoresPerRun) break;
                    restores++;
                    await RestoreAsync(line);
                    break;

                case SessionCondition.Unknown:
                    // Tek bir oturumun HTTP hatası (ör. geçersiz token) — diğerlerine devam.
                    break;
            }
        }
    }

    private async Task<List<Line>> CollectLinesAsync()
    {
        var lines = new List<Line>();

        // Merkezi hat önce: telefon doğrulama kodları buna bağlı.
        var central = _cfg.CurrentValue;
        if (!string.IsNullOrWhiteSpace(central.Session) && !string.IsNullOrWhiteSpace(central.Token))
            lines.Add(new Line("Merkezi hat", central.Session, central.Token));

        var tenants = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive && t.WppConnectSession != null && t.WppConnectToken != null)
            .Select(t => new { t.Subdomain, t.WppConnectSession, t.WppConnectToken })
            .ToListAsync();

        lines.AddRange(tenants
            .Where(t => !string.IsNullOrWhiteSpace(t.WppConnectSession) && !string.IsNullOrWhiteSpace(t.WppConnectToken))
            .Select(t => new Line($"İşletme hattı ({t.Subdomain})", t.WppConnectSession!, t.WppConnectToken!)));

        return lines;
    }

    private async Task RestoreAsync(Line line)
    {
        _log.LogInformation("[WA-Recovery] {Label} kapalı; aynı oturumla yeniden başlatılıyor ({Session}).",
            line.Label, line.Session);

        if (!await _wpp.TriggerStartAsync(line.Session, line.Token))
        {
            _state.Record(line.Session, RecoveryOutcome.StartFailed);
            _log.LogWarning("[WA-Recovery] {Label} için başlatma isteği reddedildi ({Session}).", line.Label, line.Session);
            return;
        }

        var deadline = DateTime.UtcNow + RestoreWait;
        var last = "CLOSED";
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollInterval);
            var status = await _wpp.GetSessionStatusAsync(line.Session, line.Token);
            last = status.Value;

            switch (WhatsAppSessionRecoveryState.Classify(status))
            {
                case SessionCondition.Healthy:
                    _state.Record(line.Session, RecoveryOutcome.Recovered);
                    _log.LogInformation("[WA-Recovery] {Label} QR'sız geri geldi ({Session}).", line.Label, line.Session);
                    return;

                case SessionCondition.AwaitingQr:
                    await GiveUpAsync(line);
                    return;

                // Starting / Closed (istek henüz işlenmedi) / Unknown: beklemeye devam.
            }
        }

        _state.Record(line.Session, RecoveryOutcome.StillStarting);
        _log.LogWarning("[WA-Recovery] {Label} {Seconds:F0} sn içinde bağlanmadı (son durum: {Status}); sonraki turda yeniden bakılacak ({Session}).",
            line.Label, RestoreWait.TotalSeconds, last, line.Session);
    }

    /// <summary>
    /// WhatsApp QR istiyor: cihaz telefondan düşmüş, sessiz kurtarma mümkün değil.
    /// Tarayıcı kapatılır ama oturum silinmez — işletme aynı ekrandan yeniden bağlanabilir.
    /// </summary>
    private async Task GiveUpAsync(Line line)
    {
        await CloseQuietlyAsync(line);
        _state.Record(line.Session, RecoveryOutcome.NeedsQr);
        _log.LogWarning("[WA-Recovery] {Label}: WhatsApp QR istiyor, cihaz telefondan düşmüş. Elle yeniden bağlanması gerekiyor; {Hours:F0} saat yeniden denenmeyecek ({Session}).",
            line.Label, WhatsAppSessionRecoveryState.NeedsQrRetryAfter.TotalHours, line.Session);
    }

    private async Task CloseQuietlyAsync(Line line)
    {
        try { await _wpp.CloseSessionAsync(line.Session, line.Token); }
        catch (Exception ex) { _log.LogWarning(ex, "[WA-Recovery] Oturum kapatılamadı ({Session}).", line.Session); }
    }
}
