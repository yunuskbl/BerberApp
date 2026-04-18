using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Appointment.Commands;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Appointment.Handlers;

public class CompleteAppointmentHandler : IRequestHandler<CompleteAppointmentCommand, bool>
{
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;

    public CompleteAppointmentHandler(IGenericRepository<AppointmentEntity> appointmentRepo)
    {
        _appointmentRepo = appointmentRepo;
    }

    public async Task<bool> Handle(CompleteAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await _appointmentRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (appointment is null)
            throw new NotFoundException("Randevu", request.Id);

        if (appointment.Status == AppointmentStatus.Cancelled)
            throw new BadRequestException("İptal edilmiş randevu tamamlanamaz.");

        if (appointment.Status == AppointmentStatus.Completed)
            throw new BadRequestException("Randevu zaten tamamlanmış.");

        appointment.Status = AppointmentStatus.Completed;
        await _appointmentRepo.UpdateAsync(appointment, ct);

        return true;
    }
}