using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Auth.Commands;
using FluentValidation;

namespace BerberApp.Application.Auth.Validators;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.TenantName)
            .NotEmpty().WithMessage("İşletme adı zorunludur.")
            .MaximumLength(100).WithMessage("İşletme adı 100 karakterden uzun olamaz.");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain zorunludur.")
            .MaximumLength(50).WithMessage("Subdomain 50 karakterden uzun olamaz.")
            .Matches("^[a-z0-9-]+$").WithMessage("Subdomain sadece küçük harf, rakam ve tire içerebilir.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(50).WithMessage("Ad 50 karakterden uzun olamaz.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad zorunludur.")
            .MaximumLength(50).WithMessage("Soyad 50 karakterden uzun olamaz.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.")
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon zorunludur.")
            .Matches(@"^[0-9\+\-\s]+$").WithMessage("Geçerli bir telefon numarası giriniz.");
    }
}