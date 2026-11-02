using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BerberApp.Api.Controllers;

/// <summary>
/// Hizmet sağlayıcının yasal kimliği — sitenin altbilgisi ve gizlilik
/// politikası bunu gösteriyor.
///
/// Değerler e-posta altbilgisiyle aynı yapılandırmadan (<c>Company:*</c>,
/// .env'de <c>COMPANY_*</c>) okunuyor; böylece unvan tek yerde yazılıyor.
/// Site eskiden yalnızca "ayarlıyo" markasını gösteriyordu. Marka bir tüzel
/// kişi değil: Meta işletme doğrulaması belgedeki adı sitede arıyor, KVKK veri
/// sorumlusunun, e-ticaret mevzuatı da hizmet sağlayıcının kim olduğunun
/// açıkça yazılmasını istiyor.
///
/// Doldurulmamış alan döndürülmüyor; ön yüz yalnızca dolu olanları basıyor.
/// </summary>
[ApiController]
[Route("api/company")]
[AllowAnonymous]
public class CompanyController : ControllerBase
{
    private readonly IConfiguration _config;

    public CompanyController(IConfiguration config) => _config = config;

    [HttpGet]
    public IActionResult Get()
    {
        string? Read(string key)
        {
            var value = _config[$"Company:{key}"];
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                legalName = Read("LegalName"),
                address   = Read("Address"),
                email     = Read("Email") ?? "info@ayarliyo.com",
                phone     = Read("Phone"),
                taxInfo   = Read("TaxInfo"),
            }
        });
    }
}
