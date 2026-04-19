using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Infrastructure.Jobs;

public class ExpireAppointmentsJob
{
    private readonly IAppDbContext _context;

    public ExpireAppointmentsJob(IAppDbContext context)
    {
        _context = context;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExpireOldAppointmentsAsync()
    {
        var threshold = DateTime.UtcNow.AddMinutes(-30);

        var expiredAppointments = await _context.Appointments
            .Where(x => x.Status == AppointmentStatus.Pending &&
                        x.CreatedAt <= threshold)
            .ToListAsync();

        foreach (var apt in expiredAppointments)
        {
            apt.Status = AppointmentStatus.Cancelled;
        }

        if (expiredAppointments.Any())
            await _context.SaveChangesAsync();
    }
}
