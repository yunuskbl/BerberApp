namespace BerberApp.Application.Common.Interfaces;

public interface IIyzicoService
{
    Task<IyzicoCheckoutResult> InitiateCheckout(IyzicoCheckoutRequest request);
    Task<IyzicoPaymentResult> ValidateCallback(string token);
}

public record IyzicoCheckoutRequest(
    Guid TenantId,
    string TenantName,
    string BuyerEmail,
    string BuyerName,
    string BuyerSurname,
    string BuyerPhone,
    string Plan,
    decimal Price
);

public record IyzicoCheckoutResult(
    bool Success,
    string? CheckoutFormContent,
    string? ConversationId,
    string? ErrorMessage
);

public record IyzicoPaymentResult(
    bool Success,
    string? ConversationId,
    string? Plan,
    Guid TenantId,
    string? ErrorMessage
);
