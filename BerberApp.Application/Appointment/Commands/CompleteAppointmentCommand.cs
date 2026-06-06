using MediatR;

namespace BerberApp.Application.Appointment.Commands;

public class CompleteAppointmentCommand : IRequest<bool>
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string? ReviewUrl { get; set; }

    // Gerçekte yapılan hizmetler
    public List<Guid> ActualServiceIds { get; set; } = new();
    public decimal? ActualTotalPrice { get; set; }
    public string? CompletionNotes { get; set; }
}
