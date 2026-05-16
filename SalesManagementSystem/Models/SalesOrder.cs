namespace SalesManagementSystem.Models;

public sealed class SalesOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Новый";
    public decimal TotalAmount { get; set; }
    public string Comment { get; set; } = string.Empty;
}
