using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BerberApp.Application.Service.Commands;
using FluentValidation;

namespace BerberApp.Application.Service.Validators;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hizmet adı zorunludur.")
            .MaximumLength(100).WithMessage("Hizmet adı 100 karakterden uzun olamaz.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Hizmet süresi 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(480).WithMessage("Hizmet süresi 480 dakikadan fazla olamaz.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Fiyat 0'dan küçük olamaz.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Para birimi zorunludur.")
            .MaximumLength(10).WithMessage("Para birimi 10 karakterden uzun olamaz.");
    }
}
