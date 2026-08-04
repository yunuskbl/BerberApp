using System.Globalization;

namespace BerberApp.Application.Common.Sms;

/// <summary>
/// Tüm SMS sağlayıcılarının (Netgsm, Twilio, İletiMerkezi) ortak metin kaynağı.
/// Metinler burada tek yerde tutulur; sağlayıcılar yalnızca gönderimden sorumludur.
///
/// Yazım ilkeleri:
/// - İşlemsel bildirim tonu: ünlem, emoji ve pazarlama dili yok.
/// - Müşteriye "Sayın {ad}" ile hitap edilir.
/// - Bilgiler etiketli satırlara ayrılır, virgülle uzayan tek cümleye değil.
/// - Salon adı cümleye gömülmez, kendi satırında verilir. Gömüldüğünde Türkçe ek
///   uyumu ("Elit Kuaför'deki" / "Ada Güzellik'teki") isimden isme değiştiği için
///   otomatik üretilemez; etiketli satır bu sorunu tamamen ortadan kaldırır.
/// - Boş gelen alanlar (salon adı, link, personel) satırıyla birlikte düşer.
/// </summary>
public static class SmsTemplates
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// <summary>Doğrulama kodu. Kod ilk satırda: telefonların otomatik doldurma özelliği bunu yakalar.</summary>
    public static string Otp(string otp, int validMinutes = 5) => Lines(
        $"Doğrulama kodunuz: {otp}",
        $"Kod {validMinutes} dakika geçerlidir. Güvenliğiniz için kimseyle paylaşmayın.");

    public static string AppointmentConfirmed(
        string customerName, string serviceName, string staffName,
        DateTime startTimeUtc, string salonName, string bookingUrl) => Lines(
        Greeting(customerName, "randevunuz onaylandı."),
        Field("Salon",    salonName),
        Field("Tarih",    FormatDateTime(startTimeUtc)),
        Field("Hizmet",   serviceName),
        Field("Personel", staffName),
        Field("Randevu sayfası", bookingUrl));

    public static string AppointmentUpdated(
        string customerName, string serviceName, string staffName,
        DateTime startTimeUtc, string salonName, string bookingUrl) => Lines(
        Greeting(customerName, "randevunuz güncellendi."),
        Field("Salon",      salonName),
        Field("Yeni tarih", FormatDateTime(startTimeUtc)),
        Field("Hizmet",     serviceName),
        Field("Personel",   staffName),
        Field("Randevu sayfası", bookingUrl));

    /// <summary>Bir gün önce gönderilen hatırlatma.</summary>
    public static string AppointmentReminder(
        string customerName, string serviceName,
        DateTime startTimeUtc, string salonName, string bookingUrl) => Lines(
        Greeting(customerName, "yarınki randevunuzu hatırlatırız."),
        Field("Salon",  salonName),
        Field("Tarih",  FormatDateTime(startTimeUtc)),
        Field("Hizmet", serviceName),
        Field("Randevu sayfası", bookingUrl));

    /// <summary>
    /// Bir saat önce gönderilen hatırlatma. Tarih ve randevu bağlantısı bilinçli olarak
    /// yok: müşteri yola çıkmak üzere, ihtiyacı olan tek bilgi saat ve yer.
    /// Mesaj böylece tek SMS parçasında kalır.
    /// </summary>
    public static string AppointmentReminder1h(
        string customerName, string serviceName,
        DateTime startTimeUtc, string salonName) => Lines(
        Greeting(customerName, "randevunuza 1 saat kaldı."),
        Field("Salon",  salonName),
        Field("Saat",   ToTurkeyTime(startTimeUtc).ToString("HH:mm", Tr)),
        Field("Hizmet", serviceName));

    public static string AppointmentCancelled(
        string customerName, DateTime startTimeUtc, string salonName, string bookingUrl) => Lines(
        Greeting(customerName, "randevunuz iptal edilmiştir."),
        Field("Salon", salonName),
        Field("Tarih", FormatDateTime(startTimeUtc)),
        Field("Yeni randevu", bookingUrl));

    public static string AppointmentCompleted(
        string customerName, string serviceName, string salonName, string reviewUrl)
    {
        var service = Clean(serviceName);
        var opening = service.Length == 0
            ? "randevunuz tamamlandı. Bizi tercih ettiğiniz için teşekkür ederiz."
            : $"{service} hizmetiniz tamamlandı. Bizi tercih ettiğiniz için teşekkür ederiz.";

        return Lines(
            Greeting(customerName, opening),
            Field("Salon", salonName),
            Field("Deneyiminizi değerlendirin", reviewUrl));
    }

    // ─── Yardımcılar ────────────────────────────────────────────────────────

    /// <summary>
    /// "Sayın Ayşe Yılmaz, randevunuz onaylandı." — müşteri adı yoksa cümle büyük
    /// harfle başlatılır, böylece küçük harfli bir açılış oluşmaz.
    /// </summary>
    private static string Greeting(string customerName, string sentence)
    {
        var name = Clean(customerName);
        return name.Length == 0
            ? Capitalize(sentence)
            : $"Sayın {name}, {sentence}";
    }

    private static string? Field(string label, string value)
    {
        var v = Clean(value);
        return v.Length == 0 ? null : $"{label}: {v}";
    }

    private static string Lines(params string?[] parts)
        => string.Join("\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

    private static string Capitalize(string value)
        => value.Length == 0 ? value : char.ToUpper(value[0], Tr) + value[1..];

    /// <summary>"12 Mart 2026 Perşembe, 14:30"</summary>
    public static string FormatDateTime(DateTime utcTime)
    {
        var t = ToTurkeyTime(utcTime);
        return $"{t.ToString("d MMMM yyyy dddd", Tr)}, {t.ToString("HH:mm", Tr)}";
    }

    public static DateTime ToTurkeyTime(DateTime utcTime)
    {
        if (utcTime.Kind != DateTimeKind.Utc)
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

        try   { return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")); }
        catch { return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")); }
    }
}
