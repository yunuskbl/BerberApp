using BerberApp.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Common.Interfaces;

/// <summary>
/// İşletmenin toplu mesaj göndereceği WhatsApp hattını çözer.
/// </summary>
public static class WhatsAppLineResolver
{
    /// <summary>
    /// Toplu mesaj yalnızca işletmenin kendi bağladığı numaradan gönderilir.
    ///
    /// Merkezi ayarlıyo hattına düşmek bilinçli olarak engellendi: müşteri
    /// tanımadığı bir numaradan pazarlama mesajı alınca hem salonu tanımaz hem
    /// de bunu spam olarak işaretler. Tek bir numaradan yüzlerce işletmenin
    /// müşterisine mesaj gitmesi o hattın WhatsApp tarafından engellenmesine de
    /// yol açar. Bağlantı yoksa gönderim yapmak yerine kullanıcıya ne yapması
    /// gerektiği söylenir.
    /// </summary>
    public static async Task<IWhatsAppService> RequireTenantLineAsync(
        IAppDbContext context,
        IWhatsAppService whatsApp,
        IWppConnectManagementService management,
        Guid tenantId,
        CancellationToken ct)
    {
        var tenant = await context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.WppConnectSession, t.WppConnectToken })
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(tenant?.WppConnectSession) || string.IsNullOrWhiteSpace(tenant?.WppConnectToken))
            throw new BadRequestException(
                "Toplu mesaj gönderebilmek için önce kendi WhatsApp numaranızı bağlamalısınız. " +
                "Ayarlar → WhatsApp Hesabınızı Bağlayın adımını tamamlayın.");

        // Kayıt var diye bağlantı da ayakta demek değil: telefon uzun süre
        // çevrimdışı kalırsa WhatsApp Web oturumu düşer. Önden kontrol
        // edilmezse her alıcı tek tek hata verir ve kullanıcı sebebi
        // anlaşılmayan bir "hepsi başarısız" sonucu görür.
        var status = await management.GetStatusAsync(tenant.WppConnectSession, tenant.WppConnectToken, ct);
        if (status is not ("CONNECTED" or "inChat" or "isLogged"))
            throw new BadRequestException(
                $"WhatsApp oturumunuz bağlı değil (durum: {status}). " +
                "Ayarlar → WhatsApp sayfasından QR kodu okutarak yeniden bağlanın.");

        return whatsApp.ForTenant(tenant.WppConnectSession, tenant.WppConnectToken);
    }
}
