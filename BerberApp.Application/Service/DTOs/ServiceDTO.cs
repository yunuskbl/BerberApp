using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace BerberApp.Application.Service.DTOs;

public class ServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? NameRu { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Color { get; set; }
    public bool IsActive { get; set; }
}