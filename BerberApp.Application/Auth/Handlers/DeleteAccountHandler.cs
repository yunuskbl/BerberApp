using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BerberApp.Application.Auth.Handlers;

/// <summary>
/// KVKK Madde 7 / GDPR Madde 17 — "Unutulma Hakkı" (Right to Erasure).
/// Kullanıcının kişisel verileri anonimleştirilir; hesap soft-delete edilir.
/// Randevu/işletme verileri yasal saklama zorunluluğu nedeniyle tutulur.
/// </summary>
public class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand, bool>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<DeleteAccountHandler> _logger;

    public DeleteAccountHandler(IAppDbContext context, ILogger<DeleteAccountHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, ct);

        if (user is null)
            throw new NotFoundException("Kullanıcı bulunamadı.");

        // Şifre doğrulaması — işlemin kullanıcı tarafından başlatıldığını teyit eder
        if (!BCrypt.Net.BCrypt.Verify(request.ConfirmPassword, user.PasswordHash))
            throw new UnauthorizedException("Şifre doğrulaması başarısız. Hesap silinmedi.");

        var tenantId       = user.TenantId;
        var anonymizedToken = Guid.NewGuid().ToString("N")[..10];

        // ── Kişisel verileri anonimleştir ────────────────────────────────
        user.Email          = $"deleted_{anonymizedToken}@deleted.invalid";
        user.FirstName      = "Silinmiş";
        user.LastName       = "Kullanıcı";
        // Kimlik doğrulamayı engelle: rastgele çözülemez hash
        user.PasswordHash   = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));
        // Tüm aktif oturumları geçersiz kıl
        user.RefreshToken        = null;
        user.RefreshTokenExpiry  = null;
        // Şifre sıfırlama OTP'sini temizle
        user.PasswordResetOtp       = null;
        user.PasswordResetOtpExpiry = null;
        // Hesabı soft-delete ile kapat
        user.IsDeleted  = true;
        user.UpdatedAt  = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        // Güvenlik denetim kaydı
        _logger.LogWarning(
            "[SECURITY:ACCOUNT_DELETED] UserId={UserId} TenantId={TenantId} " +
            "AnonymizedAs={AnonToken} Timestamp={Timestamp}",
            request.UserId, tenantId, anonymizedToken, DateTime.UtcNow);

        return true;
    }
}
