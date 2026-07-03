using FluentValidation;
using System.Text.RegularExpressions;

namespace BerberApp.Application.Common.Validators;

/// <summary>
/// Reusable validators for common inputs
/// </summary>
public static class CommonValidators
{
    /// <summary>
    /// Turkish phone number format: +905551234567 or 05551234567
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsTurkishPhone<T>(
        this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("Telefon numarası gereklidir")
            .Matches(@"^(\+90|0)[0-9]{10}$")
            .WithMessage("Geçerli bir Türkiye telefon numarası girin (+905551234567 veya 05551234567)");
    }

    /// <summary>
    /// Turkish email format
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsValidEmail<T>(
        this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("E-posta adresi gereklidir")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin")
            .MaximumLength(255).WithMessage("E-posta 255 karakterden uzun olamaz");
    }

    /// <summary>
    /// Strong password: min 8 chars, uppercase, lowercase, digit, special char
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsStrongPassword<T>(
        this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("Şifre gereklidir")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır")
            .Matches("[A-ZÇĞİÖŞÜ]").WithMessage("Şifre en az bir büyük harf içermeli")
            .Matches("[a-zçğışöü]").WithMessage("Şifre en az bir küçük harf içermeli")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermeli")
            .Matches("[^a-zA-Z0-9]").WithMessage("Şifre en az bir özel karakter içermeli");
    }

    /// <summary>
    /// Turkish name format (min 2 chars, no numbers)
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsTurkishName<T>(
        this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("Ad/Soyad gereklidir")
            .MinimumLength(2).WithMessage("Ad/Soyad en az 2 karakter olmalıdır")
            .MaximumLength(255).WithMessage("Ad/Soyad 255 karakterden uzun olamaz")
            .Matches(@"^[a-zA-ZçğıöşüÇĞİÖŞÜ\s]+$")
            .WithMessage("Ad/Soyad sadece harfler ve boşluk içerebilir");
    }

    /// <summary>
    /// Future date validation (min 1 hour from now)
    /// </summary>
    public static IRuleBuilderOptions<T, DateTime> IsFutureDate<T>(
        this IRuleBuilder<T, DateTime> rule)
    {
        return rule
            .NotEmpty().WithMessage("Tarih gereklidir")
            .GreaterThan(DateTime.UtcNow.AddHours(1))
            .WithMessage("Randevu en az 1 saat sonrası için alınabilir");
    }

    /// <summary>
    /// Not past date validation
    /// </summary>
    public static IRuleBuilderOptions<T, DateTime> IsNotPastDate<T>(
        this IRuleBuilder<T, DateTime> rule)
    {
        return rule
            .NotEmpty().WithMessage("Tarih gereklidir")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Geçmiş tarih seçilemez");
    }

    /// <summary>
    /// Price validation (> 0)
    /// </summary>
    public static IRuleBuilderOptions<T, decimal> IsValidPrice<T>(
        this IRuleBuilder<T, decimal> rule)
    {
        return rule
            .GreaterThan(0).WithMessage("Fiyat 0'dan büyük olmalıdır")
            .LessThan(decimal.MaxValue).WithMessage("Fiyat geçersiz");
    }

    /// <summary>
    /// Duration validation (> 0 minutes)
    /// </summary>
    public static IRuleBuilderOptions<T, int> IsValidDuration<T>(
        this IRuleBuilder<T, int> rule)
    {
        return rule
            .GreaterThan(0).WithMessage("Süre 0'dan büyük olmalıdır")
            .LessThanOrEqualTo(480).WithMessage("Süre 8 saatten fazla olamaz");
    }
}

/// <summary>
/// Güvenilir e-posta kontrolü: bilinen sağlayıcı + rastgele karakter tespiti
/// </summary>
public static class TrustedEmailHelper
{
    private static readonly HashSet<string> AllowedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com",
        "outlook.com", "outlook.com.tr",
        "hotmail.com", "hotmail.com.tr",
        "live.com", "msn.com", "windowslive.com",
        "icloud.com", "me.com", "mac.com",
        "yahoo.com", "yahoo.com.tr", "ymail.com",
        "yandex.com", "yandex.com.tr", "yandex.ru",
        "proton.me", "protonmail.com",
        "mynet.com",
    };

    /// <summary>E-posta bilinen bir sağlayıcıya mı ait? (boş değerlere dokunmaz — NotEmpty kuralı yakalar)</summary>
    public static bool HasAllowedDomain(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1) return true; // format hatasını EmailAddress kuralı yakalar
        return AllowedDomains.Contains(email[(at + 1)..].Trim());
    }

    /// <summary>
    /// Kullanıcı adı kısmı gerçekçi mi? Rastgele klavye dizilerini (ör. xkjqwrtpz) reddeder:
    /// en az 3 karakter, en az bir harf, en az bir sesli harf, 5+ ardışık sessiz harf yok.
    /// </summary>
    public static bool HasPlausibleLocalPart(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        var at = email.IndexOf('@');
        if (at <= 0) return true;

        // +etiket kısmını yok say (ornek+tag@gmail.com)
        var local = email[..at].Split('+')[0].ToLowerInvariant();
        if (local.Length < 3) return false;

        var letters = local.Where(char.IsLetter).ToList();
        if (letters.Count == 0) return false;

        const string vowels = "aeiou";
        if (!letters.Any(c => vowels.Contains(c))) return false;

        var consonantRun = 0;
        foreach (var c in local)
        {
            if (char.IsLetter(c) && !vowels.Contains(c))
            {
                if (++consonantRun >= 5) return false;
            }
            else
            {
                consonantRun = 0;
            }
        }
        return true;
    }
}

/// <summary>
/// Phone number utility
/// </summary>
public static class PhoneNumberHelper
{
    /// <summary>
    /// Normalize phone number to +90 format
    /// </summary>
    public static string NormalizePhoneNumber(string phone)
    {
        var cleaned = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        if (cleaned.StartsWith("+90"))
            return cleaned;

        if (cleaned.StartsWith("0"))
            return "+90" + cleaned.Substring(1);

        // 10-digit number starting with 5 (Turkish mobile without prefix)
        if (cleaned.Length == 10 && cleaned.StartsWith("5"))
            return "+90" + cleaned;

        throw new ArgumentException("Geçersiz telefon numarası formatı");
    }

    /// <summary>
    /// Validate Turkish phone
    /// </summary>
    public static bool IsValidTurkishPhone(string phone)
    {
        try
        {
            var normalized = NormalizePhoneNumber(phone);
            return Regex.IsMatch(normalized, @"^\+90[0-9]{10}$");
        }
        catch
        {
            return false;
        }
    }
}