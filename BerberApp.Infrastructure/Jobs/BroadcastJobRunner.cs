using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BerberApp.Infrastructure.Jobs;

/// <summary>
/// Toplu mesajı arka planda gönderir.
///
/// Gönderim eskiden HTTP isteğinin içinde, alıcılar sırayla dolaşılarak
/// yapılıyordu. Alıcı sayısı arttıkça istek ters proxy sınırını aşıp düşüyor,
/// mesajların bir kısmı gitmiş olmasına rağmen kullanıcı hata görüyordu.
/// Ayrıca başarısızlık sebepleri çıplak bir catch içinde kayboluyordu.
///
/// Artık istek anında dönüyor, gönderim burada yürüyor ve her alıcının sonucu
/// (sebebiyle birlikte) BroadcastRecipient satırına yazılıyor.
/// </summary>
public class BroadcastJobRunner
{
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsApp;
    private readonly IWppConnectManagementService _wppManagement;
    private readonly ILogger<BroadcastJobRunner> _log;

    public BroadcastJobRunner(
        IAppDbContext context,
        IWhatsAppService whatsApp,
        IWppConnectManagementService wppManagement,
        ILogger<BroadcastJobRunner> log)
    {
        _context       = context;
        _whatsApp      = whatsApp;
        _wppManagement = wppManagement;
        _log           = log;
    }

    /// <summary>
    /// WhatsApp, kısa aralıkla art arda yeni sohbet açan hattı spam sayıp
    /// engelleyebiliyor. Alıcılar arasına konan bu bekleme, hattın korunması
    /// için gönderim hızından daha değerli — iş zaten arka planda.
    /// </summary>
    private static readonly TimeSpan Pacing = TimeSpan.FromMilliseconds(1200);

    /// <summary>
    /// Yeniden deneme yok: kısmen gönderilmiş bir toplu mesajın baştan
    /// çalıştırılması, ulaşmış alıcılara mesajı ikinci kez göndermek demek.
    /// DisableConcurrentExecution aynı işin iki worker'da paralel koşup
    /// mükerrer mesaj üretmesini engeller.
    /// </summary>
    [Queue("whatsapp")]
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RunAsync(Guid broadcastJobId)
    {
        var job = await _context.BroadcastJobs
            .Include(j => j.Recipients)
            .FirstOrDefaultAsync(j => j.Id == broadcastJobId);

        if (job is null)
        {
            _log.LogWarning("[Toplu mesaj] İş bulunamadı: {JobId}", broadcastJobId);
            return;
        }

        if (job.Status != BroadcastStatus.Queued)
        {
            _log.LogWarning("[Toplu mesaj] İş zaten işlenmiş, atlanıyor: {JobId} ({Status})", job.Id, job.Status);
            return;
        }

        job.Status = BroadcastStatus.Running;
        await _context.SaveChangesAsync();

        IWhatsAppService wa;
        try
        {
            wa = await WhatsAppLineResolver.RequireTenantLineAsync(
                _context, _whatsApp, _wppManagement, job.TenantId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Hat hiç çözülemedi — tek tek denemenin anlamı yok, sebebi kaydet.
            job.Status      = BroadcastStatus.Failed;
            job.Error       = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _log.LogWarning(ex, "[Toplu mesaj] Hat çözülemedi: {JobId}", job.Id);
            return;
        }

        foreach (var recipient in job.Recipients.Where(r => r.Status == BroadcastRecipientStatus.Pending))
        {
            if (string.IsNullOrWhiteSpace(recipient.Phone))
            {
                recipient.Status = BroadcastRecipientStatus.Skipped;
                recipient.Error  = "Telefon numarası kayıtlı değil.";
                job.Skipped++;
                continue;
            }

            try
            {
                await wa.SendCustomMessageAsync(recipient.Phone, job.Message, job.ImageUrl);
                recipient.Status = BroadcastRecipientStatus.Sent;
                recipient.SentAt = DateTime.UtcNow;
                job.Sent++;
            }
            catch (Exception ex)
            {
                recipient.Status = BroadcastRecipientStatus.Failed;
                recipient.Error  = Truncate(ex.Message, 500);
                job.Failed++;
                _log.LogWarning(ex, "[Toplu mesaj] Gönderilemedi: {Phone} ({JobId})", recipient.Phone, job.Id);
            }

            // Her alıcıdan sonra kaydet: arayüz ilerlemeyi canlı okuyabilsin ve
            // iş ortasında durursa o ana kadarki sonuç kaybolmasın.
            await _context.SaveChangesAsync();

            await Task.Delay(Pacing);
        }

        job.Status      = BroadcastStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _log.LogInformation(
            "[Toplu mesaj] Tamamlandı {JobId}: {Sent} gönderildi, {Failed} başarısız, {Skipped} atlandı",
            job.Id, job.Sent, job.Failed, job.Skipped);
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
