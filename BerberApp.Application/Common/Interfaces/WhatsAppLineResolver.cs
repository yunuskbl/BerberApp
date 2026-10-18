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
        //
        // Ama kontrolün kendisi belirsiz olabilir. WPPConnect'e ulaşılamadığında
        // (timeout, servis yeniden başlıyor) oturumun kopuk OLDUĞUNU bilmiyoruz.
        // O durumda gönderimi engellemek, çalışan bir hattı sırf durum sorgusu
        // yavaşladı diye reddetmek demek. Yalnızca WPPConnect'in "bu oturum
        // bağlı değil" dediği kesin durumda engelliyoruz; ulaşılamadığında
        // deneyip gerçek sonucu alıcı bazında raporluyoruz.
        var status = await management.GetSessionStatusAsync(tenant.WppConnectSession, tenant.WppConnectToken, ct);
        if (status.IsDefinitelyDisconnected)
            throw new BadRequestException(
                $"WhatsApp oturumunuz bağlı değil (durum: {status.Value}). " +
                "Ayarlar → WhatsApp sayfasından QR kodu okutarak yeniden bağlanın.");

        return whatsApp.ForTenant(tenant.WppConnectSession, tenant.WppConnectToken);
    }
}
