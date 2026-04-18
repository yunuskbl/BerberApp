using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Staff.Commands;
using FluentValidation;

namespace BerberApp.Application.Staff.Validators;

public class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Personel adı zorunludur.")
            .MaximumLength(100).WithMessage("Personel adı 100 karakterden uzun olamaz.");

        RuleFor(x => x.Phone)
            .Matches(@"^[0-9\+\-\s]+$").WithMessage("Geçerli bir telefon numarası giriniz.")
            .When(x => !string.IsNullOrEmpty(x.Phone));
    }
}