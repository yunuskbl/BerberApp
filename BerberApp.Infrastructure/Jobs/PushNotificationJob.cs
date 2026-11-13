using BerberApp.Application.Common.Interfaces;
using Hangfire;

namespace BerberApp.Infrastructure.Jobs;

/// <summary>
/// Anlık bildirimi arka planda gönderir: müşterinin randevu alması ya da
/// onay vermesi push sunucusunun yanıtını beklemesin; geçici hatada yeniden denenir.
/// </summary>
public class PushNotificationJob
{
    private readonly IPushNotificationService _push;

    public PushNotificationJob(IPushNotificationService push) => _push = push;

    [AutomaticRetry(Attempts = 2)]
    public Task SendToTenantAsync(Guid tenantId, Guid? staffId, string title, string body, string url, string? tag)
        => _push.SendToTenantAsync(tenantId, new PushMessage(title, body, url, tag), staffId);
}
