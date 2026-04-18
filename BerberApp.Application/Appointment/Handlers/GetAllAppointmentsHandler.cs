using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Appointment.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Appointment.Handlers;

public class GetAllAppointmentsHandler : IRequestHandler<GetAllAppointmentsQuery, List<AppointmentDto>>
{
    private readonly IAppDbContext _context;

    public GetAllAppointmentsHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppointmentDto>> Handle(GetAllAppointmentsQuery request, CancellationToken ct)
    {
        var query = _context.Appointments
            .Include(x => x.Customer)
            .Include(x => x.Staff)
            .Include(x => x.Service)
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted)
            .AsQueryable();

        if (request.StaffId.HasValue)
            query = query.Where(x => x.StaffId == request.StaffId.Value);

        if (request.Date.HasValue)
        {
            // UTC tarih aralığı — Türkiye saati için +3 saat farkı
            var utcDate = request.Date.Value.Date;
            var startOfDay = utcDate;
            var endOfDay = utcDate.AddDays(1);

            query = query.Where(x =>
                x.StartTime >= startOfDay &&
                x.StartTime < endOfDay);
        }

        var appointments = await query
            .OrderBy(x => x.StartTime)
            .ToListAsync(ct);

        return appointments.Select(x => new AppointmentDto
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
        }).ToList();
    }
}
