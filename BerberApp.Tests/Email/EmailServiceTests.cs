using BerberApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace BerberApp.Tests.Email;

/// <summary>
/// SMTP göndermeden sadece HTML içerik üretimini test eder.
/// Gerçek e-posta gönderimi entegrasyon testine bırakılır.
/// </summary>
public class EmailServiceTests
{
    private static SmtpEmailService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:SmtpHost"]     = "localhost",
                ["Email:SmtpPort"]     = "25",
                ["Email:SmtpUsername"] = "test@ayarliyo.com",
                ["Email:SmtpPassword"] = "testpass",
                ["Email:FromAddress"]  = "noreply@ayarliyo.com",
                ["Email:FromName"]     = "ayarlıyo"
            })
            .Build();
        return new SmtpEmailService(config);
    }

    [Fact]
    public void EmailVerification_HtmlContainsVerificationUrl()
    {
        var service = CreateService();
        var html    = GetEmailHtml(service, s =>
            s.SendEmailVerificationAsync("x@x.com", "Ali Veli", "https://ayarliyo.com/email-dogrula?token=abc123"));

        html.Should().Contain("abc123");
        html.Should().Contain("E-posta Adresimi Doğrula");
        html.Should().Contain("Ali Veli");
    }

    [Fact]
    public void WelcomeEmail_HtmlContainsSalonName()
    {
        var service = CreateService();
        var html    = GetEmailHtml(service, s =>
            s.SendWelcomeEmailAsync("x@x.com", "Ali Veli", "Elite Berber"));

        html.Should().Contain("Elite Berber");
        html.Should().Contain("Ali Veli");
        html.Should().Contain("dashboard");
    }

    [Fact]
    public void AppointmentConfirmed_HtmlContainsDateAndService()
    {
        var service         = CreateService();
        var appointmentDate = new DateTime(2026, 7, 15, 14, 30, 0);
        var html            = GetEmailHtml(service, s =>
            s.SendAppointmentConfirmedAsync("x@x.com", "Mehmet Demir", "Elite Berber", appointmentDate, "Saç Kesimi", "Usta Ali"));

        html.Should().Contain("Mehmet Demir");
        html.Should().Contain("Elite Berber");
        html.Should().Contain("Saç Kesimi");
        html.Should().Contain("Usta Ali");
        html.Should().Contain("2026");
    }

    [Fact]
    public void AppointmentCancelled_HtmlContainsCustomerName()
    {
        var service         = CreateService();
        var appointmentDate = new DateTime(2026, 7, 15, 14, 30, 0);
        var html            = GetEmailHtml(service, s =>
            s.SendAppointmentCancelledAsync("x@x.com", "Zeynep Kaya", "Güzellik Salonu", appointmentDate));

        html.Should().Contain("Zeynep Kaya");
        html.Should().Contain("Güzellik Salonu");
        html.Should().Contain("İptal Edildi");
    }

    [Fact]
    public void AppointmentReminder_HtmlContainsReminderText()
    {
        var service         = CreateService();
        var appointmentDate = new DateTime(2026, 7, 15, 10, 0, 0);
        var html            = GetEmailHtml(service, s =>
            s.SendAppointmentReminderAsync("x@x.com", "Ayşe Yıldız", "Top Salon", appointmentDate, "Manikür"));

        html.Should().Contain("Ayşe Yıldız");
        html.Should().Contain("Top Salon");
        html.Should().Contain("Manikür");
    }

    [Fact]
    public void SubscriptionActivated_HtmlContainsPlanName()
    {
        var service    = CreateService();
        var expiryDate = new DateTime(2026, 8, 1);
        var html       = GetEmailHtml(service, s =>
            s.SendSubscriptionActivatedAsync("x@x.com", "Can Öztürk", "Profesyonel", expiryDate));

        html.Should().Contain("Can Öztürk");
        html.Should().Contain("Profesyonel");
        html.Should().Contain("Panele Git");
    }

    [Fact]
    public void SubscriptionExpiryWarning_HtmlContainsDaysRemaining()
    {
        var service    = CreateService();
        var expiryDate = new DateTime(2026, 7, 25);
        var html       = GetEmailHtml(service, s =>
            s.SendSubscriptionExpiryWarningAsync("x@x.com", "Berk Şahin", "Berk Kuaför", 7, expiryDate));

        html.Should().Contain("7");
        html.Should().Contain("Berk Şahin");
        html.Should().Contain("Aboneliği Yenile");
        html.Should().Contain("pricing");
    }

    // ── Helper: SMTP çağrısı yapmadan HTML'i yakalar ─────────────────────────

    private static string GetEmailHtml(SmtpEmailService service, Func<SmtpEmailService, Task> sendFunc)
    {
        string captured = string.Empty;

        // SmtpClient localhost:25'e bağlanamayacağı için exception gelir.
        // Biz exception'dan önce body üretildiğini test ediyoruz.
        // GetCapturedBodyAsync helper kullanılır.
        try
        {
            sendFunc(service).GetAwaiter().GetResult();
        }
        catch
        {
            // SMTP bağlantısı yok, expected. HTML üretimini ayrı test ediyoruz.
        }

        // HTML içerik testi için refleksiyon yerine bir wrapper kullanırız.
        return InspectHtml(service, sendFunc);
    }

    private static string InspectHtml(SmtpEmailService service, Func<SmtpEmailService, Task> sendFunc)
    {
        // Private SendAsync metodunu geçerek HTML'i capture etmek için
        // TestableSmtpEmailService alt sınıfını kullanırız.
        var testable = new TestableSmtpEmailService();
        sendFunc(testable).GetAwaiter().GetResult();
        return testable.LastHtmlBody;
    }
}

/// <summary>SMTP göndermeden HTML body'yi saklayan test alt sınıfı.</summary>
internal class TestableSmtpEmailService : SmtpEmailService
{
    public string LastHtmlBody { get; private set; } = string.Empty;

    public TestableSmtpEmailService() : base(
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:SmtpHost"]     = "localhost",
                ["Email:SmtpPort"]     = "25",
                ["Email:SmtpUsername"] = "x",
                ["Email:SmtpPassword"] = "x",
                ["Email:FromAddress"]  = "noreply@test.com",
                ["Email:FromName"]     = "Test"
            }).Build())
    { }

    protected override Task SendAsync(string to, string subject, string htmlBody)
    {
        LastHtmlBody = htmlBody;
        return Task.CompletedTask;
    }
}
