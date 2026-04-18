using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BerberApp.Application.Appointment.Commands;
using FluentValidation;

namespace BerberApp.Application.Appointment.Validators;

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Müşteri seçimi zorunludur.");

        RuleFor(x => x.StaffId)
            .NotEmpty().WithMessage("Personel seçimi zorunludur.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Hizmet seçimi zorunludur.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Randevu saati zorunludur.")
            .GreaterThan(DateTime.UtcNow).WithMessage("Randevu saati geçmiş bir tarih olamaz.");
    }
}