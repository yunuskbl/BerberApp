using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BerberApp.Infrastructure.Services;

/// <summary>
/// Sırayla birden fazla SMS sağlayıcısı dener; ilki başarılı olduğunda durur.
/// Sıra <c>Sms:Providers</c> ayarından gelir (ör. "Netgsm,Twilio").
///
/// Sağlayıcılar isteğe bağlı olarak yapılandırılmış olabileceği için (ör. Twilio
/// kimlik bilgileri girilmemişse yapıcı hata verebilir) her sağlayıcı DI'dan
/// tembel şekilde çözülür ve hem çözümleme hem gönderim hataları yakalanıp
/// bir sonrakine geçilir. Hiçbiri başaramazsa toplu hata fırlatılır — çağıran
/// (ör. OtpController) bunu yakalayıp kullanıcıya anlamlı mesaj döndürür.
/// </summary>
public class ChainedSmsService : ISmsService
{
    private readonly IServiceProvider _sp;
    private readonly IReadOnlyList<Type> _order;
    private readonly ILogger<ChainedSmsService> _log;

    public ChainedSmsService(IServiceProvider sp, IReadOnlyList<Type> order, ILogger<ChainedSmsService> log)
    {
        _sp    = sp;
        _order = order;
        _log   = log;
    }

    private async Task RunAsync(string operation, Func<ISmsService, Task> send)
    {
        var errors = new List<string>();

        foreach (var type in _order)
        {
            ISmsService provider;
            try
            {
                provider = (ISmsService)_sp.GetRequiredService(type);
            }
            catch (Exception ex)
            {
                errors.Add($"{type.Name}: yapılandırılamadı ({ex.Message})");
                _log.LogWarning("[SMS] {Provider} örneklenemedi ({Op}): {Message}", type.Name, operation, ex.Message);
                continue;
            }

            try
            {
                await send(provider);
                _log.LogInformation("[SMS] {Provider} ile gönderildi ({Op})", type.Name, operation);
                return;
            }
            catch (Exception ex)
            {
                errors.Add($"{type.Name}: {ex.Message}");
                _log.LogWarning("[SMS] {Provider} başarısız ({Op}): {Message}", type.Name, operation, ex.Message);
            }
        }

        throw new InvalidOperationException(
            $"Tüm SMS sağlayıcıları başarısız ({operation}). " + string.Join(" | ", errors));
    }

    public Task SendOtpAsync(string phone, string otp, string salonName = "")
        => RunAsync(nameof(SendOtpAsync), s => s.SendOtpAsync(phone, otp, salonName));

    public Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName, string staffName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => RunAsync(nameof(SendAppointmentConfirmedAsync),
            s => s.SendAppointmentConfirmedAsync(phone, customerName, serviceName, staffName, startTime, salonName, mapsUrl, bookingUrl));

    public Task SendAppointmentReminderAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => RunAsync(nameof(SendAppointmentReminderAsync),
            s => s.SendAppointmentReminderAsync(phone, customerName, serviceName, startTime, salonName, mapsUrl, bookingUrl));

    public Task SendAppointmentReminder1hAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => RunAsync(nameof(SendAppointmentReminder1hAsync),
            s => s.SendAppointmentReminder1hAsync(phone, customerName, serviceName, startTime, salonName, mapsUrl, bookingUrl));

    public Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime, string salonName = "", string bookingUrl = "")
        => RunAsync(nameof(SendAppointmentCancelledAsync),
            s => s.SendAppointmentCancelledAsync(phone, customerName, startTime, salonName, bookingUrl));

    public Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName, string salonName, string reviewUrl)
        => RunAsync(nameof(SendAppointmentCompletedAsync),
            s => s.SendAppointmentCompletedAsync(phone, customerName, serviceName, salonName, reviewUrl));

    public Task SendAppointmentUpdatedAsync(
        string phone, string customerName, string serviceName, string staffName,
        DateTime startTime, string salonName = "", string bookingUrl = "")
        => RunAsync(nameof(SendAppointmentUpdatedAsync),
            s => s.SendAppointmentUpdatedAsync(phone, customerName, serviceName, staffName, startTime, salonName, bookingUrl));
}
