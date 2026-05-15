using BerberApp.Application.Common.Interfaces;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Configuration;

namespace BerberApp.Infrastructure.Services;

public class IyzicoService : IIyzicoService
{
    private readonly Options _options;
    private readonly string _callbackUrl;
    private readonly string _frontendSuccessUrl;
    private readonly string _frontendFailUrl;

    private static readonly Dictionary<string, decimal> PlanPrices = new()
    {
        { "baslangic",   899m  },
        { "profesyonel", 1799m },
        { "premium",     2399m },
    };

    public IyzicoService(IConfiguration config)
    {
        var sec = config.GetSection("Iyzico");
        _options = new Options
        {
            ApiKey    = sec["ApiKey"]!,
            SecretKey = sec["SecretKey"]!,
            BaseUrl   = sec["BaseUrl"]!
        };
        _callbackUrl        = sec["CallbackUrl"]!;
        _frontendSuccessUrl = sec["FrontendSuccessUrl"]!;
        _frontendFailUrl    = sec["FrontendFailUrl"]!;
    }

    public async Task<IyzicoCheckoutResult> InitiateCheckout(IyzicoCheckoutRequest req)
    {
        try
        {
            var conversationId = $"{req.TenantId}_{req.Plan}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            var priceStr = req.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            var buyer = new Buyer
            {
                Id              = req.TenantId.ToString(),
                Name            = req.BuyerName,
                Surname         = req.BuyerSurname,
                Email           = req.BuyerEmail,
                GsmNumber       = req.BuyerPhone,
                RegistrationDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LastLoginDate   = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                RegistrationAddress = "Türkiye",
                Ip              = "85.34.78.112",
                City            = "Istanbul",
                Country         = "Turkey",
                ZipCode         = "34000"
            };

            var address = new Address
            {
                ContactName = $"{req.BuyerName} {req.BuyerSurname}",
                City        = "Istanbul",
                Country     = "Turkey",
                Description = "Türkiye",
                ZipCode     = "34000"
            };

            var basketItem = new BasketItem
            {
                Id        = req.Plan,
                Name      = $"ayarlıyo {req.Plan} Abonelik (1 Yıl)",
                Category1 = "Yazılım",
                Category2 = "SaaS Abonelik",
                ItemType  = "VIRTUAL",
                Price     = priceStr
            };

            var initRequest = new CreateCheckoutFormInitializeRequest
            {
                Locale             = "tr",
                ConversationId     = conversationId,
                Price              = priceStr,
                PaidPrice          = priceStr,
                Currency           = "TRY",
                BasketId           = req.TenantId.ToString(),
                PaymentGroup       = "SUBSCRIPTION",
                CallbackUrl        = _callbackUrl,
                EnabledInstallments = new List<int> { 1, 2, 3 },
                Buyer              = buyer,
                ShippingAddress    = address,
                BillingAddress     = address,
                BasketItems        = new List<BasketItem> { basketItem }
            };

            var form = await Task.Run(() =>
                CheckoutFormInitialize.Create(initRequest, _options));

            if (form.Status != "success")
                return new IyzicoCheckoutResult(false, null, null, form.ErrorMessage);

            return new IyzicoCheckoutResult(true, form.CheckoutFormContent, conversationId, null);
        }
        catch (Exception ex)
        {
            return new IyzicoCheckoutResult(false, null, null, ex.Message);
        }
    }

    public async Task<IyzicoPaymentResult> ValidateCallback(string token)
    {
        try
        {
            var retrieveRequest = new RetrieveCheckoutFormRequest
            {
                Locale = "tr",
                Token  = token
            };

            var form = await Task.Run(() =>
                CheckoutForm.Retrieve(retrieveRequest, _options));

            if (form.PaymentStatus != "SUCCESS")
                return new IyzicoPaymentResult(false, null, null, Guid.Empty, form.ErrorMessage ?? "Ödeme başarısız.");

            // conversationId format: {tenantId}_{plan}_{timestamp}
            var parts = form.ConversationId?.Split('_');
            if (parts == null || parts.Length < 2)
                return new IyzicoPaymentResult(false, null, null, Guid.Empty, "Geçersiz conversationId.");

            var tenantId = Guid.Parse(parts[0]);
            var plan     = parts[1];

            return new IyzicoPaymentResult(true, form.ConversationId, plan, tenantId, null);
        }
        catch (Exception ex)
        {
            return new IyzicoPaymentResult(false, null, null, Guid.Empty, ex.Message);
        }
    }

    public static decimal GetPrice(string plan) =>
        PlanPrices.TryGetValue(plan.ToLower(), out var p) ? p : 0;

    public string FrontendSuccessUrl => _frontendSuccessUrl;
    public string FrontendFailUrl    => _frontendFailUrl;
}
