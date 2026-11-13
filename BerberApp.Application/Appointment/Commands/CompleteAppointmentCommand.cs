using MediatR;

namespace BerberApp.Application.Appointment.Commands;

public class CompleteAppointmentCommand : IRequest<CompleteAppointmentResult>
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string? ReviewUrl { get; set; }

    // Gerçekte yapılan hizmetler
    public List<Guid> ActualServiceIds { get; set; } = new();
    public decimal? ActualTotalPrice { get; set; }
    // ActualTotalPrice'ın para birimi. Belirtilmezse orijinal hizmetin para birimi kullanılır.
    public string? ActualCurrency { get; set; }
    public string? CompletionNotes { get; set; }
}

public class CompleteAppointmentResult
{
    /// <summary>Paket seansında fiş kesilmez (ücret pakete ödeniyor); boş kalır.</summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    // Paketten alınan randevu: pakete işlenen seans (panel müşteri onay QR'ını gösterebilsin).
    public Guid? PackageId { get; set; }
    public Guid? PackageSessionId { get; set; }
    public int? PackageSessionNumber { get; set; }
    public int? PackageTotalSessions { get; set; }
}
