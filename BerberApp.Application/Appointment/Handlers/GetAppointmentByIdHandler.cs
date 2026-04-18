using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Appointment.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Appointment.Handlers;

public class GetAppointmentByIdHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto>
{
    private readonly IAppDbContext _context;

    public GetAppointmentByIdHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<AppointmentDto> Handle(GetAppointmentByIdQuery request, CancellationToken ct)
    {
        var appointment = await _context.Appointments
            .Include(x => x.Customer)
            .Include(x => x.Staff)
            .Include(x => x.Service)
            .Where(x => x.Id == request.Id && x.TenantId == request.TenantId)
            .Select(x => new AppointmentDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.FullName,
                StaffId = x.StaffId,
                StaffName = x.Staff.FullName,
                ServiceId = x.ServiceId,
                ServiceName = x.Service.Name,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status,
                Notes = x.Notes,
                DurationMinutes = x.Service.DurationMinutes
            })
            .FirstOrDefaultAsync(ct);

        if (appointment is null)
            throw new NotFoundException("Randevu", request.Id);

        return appointment;
    }
}
