using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Validators;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Application.Appointment.Validators;

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    private readonly IAppDbContext _context;

    public CreateAppointmentCommandValidator(IAppDbContext context)
    {
        _context = context;

        // Tenant ID
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await TenantExistsAsync(id, ct))
            .WithMessage("Salon bulunamadı");

        // Staff ID
        RuleFor(x => x.StaffId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await StaffExistsAsync(id, ct))
            .WithMessage("Personel bulunamadı");

        // Service ID
        RuleFor(x => x.ServiceId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await ServiceExistsAsync(id, ct))
            .WithMessage("Hizmet bulunamadı");

        // Start time - must be future date, at least 1 hour from now
        RuleFor(x => x.StartTime)
            .IsFutureDate();

        // Customer ID (if provided)
        RuleFor(x => x.CustomerId)
            .MustAsync(async (id, ct) => await CustomerExistsAsync(id, ct))
            .When(x => x.CustomerId != Guid.Empty)
            .WithMessage("Müşteri bulunamadı");

        // Notes - max 500 chars
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notlar 500 karakterden uzun olamaz");
    }
    private async Task<bool> TenantExistsAsync(Guid tenantId, CancellationToken ct)
    {
        return await _context.Tenants
            .AnyAsync(x => x.Id == tenantId && !x.IsDeleted, ct);
    }

    private async Task<bool> StaffExistsAsync(Guid staffId, CancellationToken ct)
    {
        return await _context.Staff
            .AnyAsync(x => x.Id == staffId && x.IsActive, ct);
    }

    private async Task<bool> ServiceExistsAsync(Guid serviceId, CancellationToken ct)
    {
        return await _context.Services
            .AnyAsync(x => x.Id == serviceId && x.IsActive, ct);
    }

    private async Task<bool> CustomerExistsAsync(Guid customerId, CancellationToken ct)
    {
        return await _context.Customers
            .AnyAsync(x => x.Id == customerId, ct);
    }
}

public record CreateAppointmentCommand(
    Guid TenantId,
    Guid StaffId,
    Guid ServiceId,
    Guid CustomerId,
    DateTime StartTime,
    string? Notes = null,
    bool IsFromBookingPage = false) : IRequest<AppointmentDto>;
 