namespace BerberApp.Application.Receipt.Dtos;

public class ReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? AppointmentId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Note { get; set; }
    public bool IsVoid { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = new();
}

public class ReceiptItemDto
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}
