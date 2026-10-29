using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class Customer : BaseEntity
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public int TotalVisits { get; set; } = 0;

    /// <summary>
    /// Müşterinin kampanya/bilgilendirme mesajı almaya açık rıza verdiği an.
    /// null = rıza yok.
    ///
    /// Tarih olarak tutuluyor çünkü ticari elektronik ileti mevzuatında
    /// ispat yükü gönderende: "rıza var mı" kadar "ne zaman alındı" da
    /// sorulabiliyor. Boolean bu bilgiyi taşıyamazdı.
    /// </summary>
    public DateTime? MarketingConsentAt { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
