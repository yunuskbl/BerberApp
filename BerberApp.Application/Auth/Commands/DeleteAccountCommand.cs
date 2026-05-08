using MediatR;

namespace BerberApp.Application.Auth.Commands;

/// <summary>
/// KVKK/GDPR — Hesap silme ve kişisel verilerin anonimleştirilmesi talebi.
/// Kullanıcı verileri silinmez; "unutulma hakkı" kapsamında anonimleştirilir.
/// </summary>
public class DeleteAccountCommand : IRequest<bool>
{
    public Guid UserId { get; set; }

    /// <summary>Şifre doğrulaması için kullanıcının mevcut şifresi.</summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
