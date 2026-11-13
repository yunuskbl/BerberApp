using BerberApp.Application.Common.Settings;
using BerberApp.Infrastructure.Jobs;
using Hangfire;
using Microsoft.Extensions.Options;

namespace BerberApp.Api.Services;

/// <summary>
/// İşletme sahibine (ve ilgili personele) anlık bildirim gerektiren olaylar.
///
/// Yalnızca müşterinin yaptığı ve işletmenin anında bilmesi gereken olaylar:
/// randevu sayfasından gelen randevu, müşterinin iptali, müşterinin seansı
/// takip sayfasından onaylaması. İşletmenin kendi panelde yaptıkları
/// bildirim üretmez. Anahtarlar tanımlı değilse hiçbir şey kuyruğa girmez.
/// </summary>
public class PushEvents
{
    private static readonly string[] Months = ["Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"];
    private static readonly TimeZoneInfo Istanbul = FindIstanbul();

    private readonly bool _enabled;
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<PushEvents> _log;

    public PushEvents(IOptions<WebPushSettings> cfg, IBackgroundJobClient jobs, ILogger<PushEvents> log)
    {
        _enabled = cfg.Value.IsConfigured;
        _jobs = jobs;
        _log = log;
    }

    public void AppointmentBooked(Guid tenantId, Guid staffId, string customerName, string serviceName, DateTime startUtc, bool fromPackage)
        => Enqueue(tenantId, staffId,
            fromPackage ? "📦 Paket seansı randevusu" : "Yeni randevu",
            $"{customerName} · {serviceName} · {When(startUtc)}",
            "/appointments", tag: null);

    public void AppointmentCancelledByCustomer(Guid tenantId, Guid staffId, string customerName, DateTime startUtc)
        => Enqueue(tenantId, staffId, "Randevu iptal edildi",
            $"{customerName} · {When(startUtc)} randevusunu iptal etti", "/appointments", tag: null);

    public void PackageSessionConfirmed(Guid tenantId, Guid packageId, string customerFirstName, string packageName, int? sessionNumber)
        => Enqueue(tenantId, null, "Seans onaylandı",
            $"{customerFirstName} · {packageName}{(sessionNumber is { } n ? $" · {n}. seans" : "")}",
            $"/packages?id={packageId}", tag: $"pkg-{packageId}");

    private void Enqueue(Guid tenantId, Guid? staffId, string title, string body, string url, string? tag)
    {
        if (!_enabled) return;
        try
        {
            _jobs.Enqueue<PushNotificationJob>(j => j.SendToTenantAsync(tenantId, staffId, title, body, url, tag));
        }
        catch (Exception ex)
        {
            // Bildirim kuyruğa giremezse asıl işlem (randevu, onay) etkilenmemeli.
            _log.LogWarning(ex, "[Push] Bildirim kuyruğa alınamadı.");
        }
    }

    /// <summary>"14 Kas 10:00" — kültür verisine bağlı kalmadan (konteyner ICU'suz olabilir).</summary>
    private static string When(DateTime utc)
    {
        var t = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Istanbul);
        return $"{t.Day} {Months[t.Month - 1]} {t:HH\\:mm}";
    }

    private static TimeZoneInfo FindIstanbul()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}
