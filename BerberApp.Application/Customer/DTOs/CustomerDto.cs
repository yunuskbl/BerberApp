using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace BerberApp.Application.Customer.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Notes { get; set; }
    /// <summary>Tamamlanmış randevu sayısı — randevulardan sayılıyor.</summary>
    public int TotalVisits { get; set; }
    /// <summary>Son tamamlanmış randevunun tarihi; hiç gelmemişse null.</summary>
    public DateTime? LastVisitAt { get; set; }
}