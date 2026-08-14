using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BerberApp.Infrastructure.Services;

/// <summary>
/// Hibrit WhatsApp yönlendirici:
/// - İşletme kendi telefonunu bağlamışsa (WppConnectSession + Token dolu) → WPPConnect (kendi numarası)
/// - Bağlamamışsa → merkezi Meta Cloud API (ayarlıyo numarası)
///
/// Doğrudan çağrılar (ForTenant olmadan) merkezi Meta'ya gider.
/// Çağıranlar göndermeden önce ForTenant(session, token) çağırdığı için
/// yönlendirme otomatik gerçekleşir.
///
/// İstisna — SendCustomMessageAsync: serbest metin, Meta'da yalnızca alıcının
/// son 24 saat içinde bize yazdığı durumda iletilebilir (müşteri hizmeti
/// penceresi). İşletme sahipleri Meta numarasına yazmadığı için bu mesajlar
/// çoğunlukla reddedilirdi; ayrıca Cloud API trafiği telefondaki WhatsApp
/// Business uygulamasında görünmez. Bu yüzden serbest metin önce merkezi
/// WPPConnect hattından (QR ile bağlı numara) gönderilir, hat kapalıysa
/// Meta'ya düşülür.
/// </summary>
public class HybridWhatsAppService : IWhatsAppService
{
    private readonly WhatsAppService _meta;
    private readonly WppConnectWhatsAppService _wpp;
    private readonly ILogger<HybridWhatsAppService> _log;

    public HybridWhatsAppService(WhatsAppService meta, WppConnectWhatsAppService wpp, ILogger<HybridWhatsAppService> log)
    {
        _meta = meta;
        _wpp  = wpp;
        _log  = log;
    }

    public IWhatsAppService ForTenant(string? session, string? token)
        => (!string.IsNullOrWhiteSpace(session) && !string.IsNullOrWhiteSpace(token))
            ? _wpp.ForTenant(session, token)   // işletmenin kendi numarası
            : _meta;                            // merkezi ayarlıyo numarası

    // ── Doğrudan çağrılar merkezi Meta'ya delege edilir ──────────────────────

    public Task SendAppointmentConfirmedAsync(string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => _meta.SendAppointmentConfirmedAsync(phone, customerName, serviceName, staffName, startTime, salonName, mapsUrl, bookingUrl);

    public Task SendAppointmentReminderAsync(string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "", string staffName = "")
        => _meta.SendAppointmentReminderAsync(phone, customerName, serviceName, startTime, salonName, mapsUrl, bookingUrl, staffName);

    public Task SendAppointmentReminder1hAsync(string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "", string staffName = "")
        => _meta.SendAppointmentReminder1hAsync(phone, customerName, serviceName, startTime, salonName, mapsUrl, bookingUrl, staffName);

    public Task SendAppointmentCancelledAsync(string phone, string customerName, DateTime startTime,
        string salonName = "", string bookingUrl = "")
        => _meta.SendAppointmentCancelledAsync(phone, customerName, startTime, salonName, bookingUrl);

    public Task SendOtpAsync(string phone, string otp, string salonName = "")
        => _meta.SendOtpAsync(phone, otp, salonName);

    public Task SendNewAppointmentRequestAsync(string staffPhone, string customerName, string customerPhone,
        string serviceName, DateTime startTime, int sequenceNumber)
        => _meta.SendNewAppointmentRequestAsync(staffPhone, customerName, customerPhone, serviceName, startTime, sequenceNumber);

    public Task SendAppointmentCompletedAsync(string phone, string customerName, string serviceName,
        string salonName, string reviewUrl)
        => _meta.SendAppointmentCompletedAsync(phone, customerName, serviceName, salonName, reviewUrl);

    public Task SendAppointmentUpdatedAsync(string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string bookingUrl = "")
        => _meta.SendAppointmentUpdatedAsync(phone, customerName, serviceName, staffName, startTime, salonName, bookingUrl);

    public Task SendMonthlyLimitWarningAsync(string phone, string salonName, int currentCount, int limit, bool isFull)
        => _meta.SendMonthlyLimitWarningAsync(phone, salonName, currentCount, limit, isFull);

    public Task SendSubscriptionExpiryWarningAsync(string phone, string salonName, int daysLeft)
        => _meta.SendSubscriptionExpiryWarningAsync(phone, salonName, daysLeft);

    public async Task SendCustomMessageAsync(string phone, string message, string? imageUrl = null)
    {
        try
        {
            await _wpp.SendCustomMessageAsync(phone, message, imageUrl);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[WhatsApp] Merkezi WPPConnect hattından gönderilemedi, Meta'ya düşülüyor: {Phone}", phone);
            await _meta.SendCustomMessageAsync(phone, message, imageUrl);
        }
    }
}
