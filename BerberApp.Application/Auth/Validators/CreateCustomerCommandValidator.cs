using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Customer.Commands;
using FluentValidation;

namespace BerberApp.Application.Customer.Validators;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Müşteri adı zorunludur.")
            .MaximumLength(100).WithMessage("Müşteri adı 100 karakterden uzun olamaz.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon zorunludur.")
            .Matches(@"^[0-9\+\-\s]+$").WithMessage("Geçerli bir telefon numarası giriniz.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
