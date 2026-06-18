using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class ReceiptItem : BaseEntity
{
    public Guid ReceiptId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }

    // Navigation
    public Receipt Receipt { get; set; } = null!;
}
