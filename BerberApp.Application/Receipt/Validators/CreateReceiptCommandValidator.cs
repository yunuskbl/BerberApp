using BerberApp.Application.Receipt.Commands;
using FluentValidation;

namespace BerberApp.Application.Receipt.Validators;

/// <summary>
/// Fiş oluşturma kuralları.
///
/// Bu validator'dan önce sunucu hiçbir şeyi kontrol etmiyordu: kalemsiz fiş,
/// eksi adet ya da eksi fiyat (yani eksi toplamlı fiş) kabul ediliyordu.
/// 200 karakteri aşan hizmet adı ya da 500'ü aşan not ise veritabanı
/// sütun sınırına çarpıp kullanıcıya anlamsız bir 500 hatası olarak
/// dönüyordu. Sınırlar <c>ReceiptConfiguration</c> ve
/// <c>ReceiptItemConfiguration</c> ile aynı.
/// </summary>
public class CreateReceiptCommandValidator : AbstractValidator<CreateReceiptCommand>
{
    public const int MaxItems       = 50;
    public const int MaxNameLength  = 200;
    public const int MaxNoteLength  = 500;
    public const int MaxQuantity    = 999;
    public const decimal MaxUnitPrice = 1_000_000m;

    public CreateReceiptCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Fişe en az bir hizmet ekleyin.")
            .Must(items => items.Count <= MaxItems)
                .WithMessage($"Bir fişte en fazla {MaxItems} kalem olabilir.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ServiceName)
                .Must(name => !string.IsNullOrWhiteSpace(name))
                    .WithMessage("Hizmet adı boş olamaz.")
                .MaximumLength(MaxNameLength)
                    .WithMessage($"Hizmet adı {MaxNameLength} karakterden uzun olamaz.");

            item.RuleFor(i => i.Quantity)
                .InclusiveBetween(1, MaxQuantity)
                    .WithMessage($"Adet 1 ile {MaxQuantity} arasında olmalı.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("Birim fiyat sıfırdan büyük olmalı.")
                .LessThanOrEqualTo(MaxUnitPrice)
                    .WithMessage("Birim fiyat çok yüksek.");
        });

        RuleFor(x => x.Note)
            .MaximumLength(MaxNoteLength)
                .WithMessage($"Not {MaxNoteLength} karakterden uzun olamaz.");
    }
}
