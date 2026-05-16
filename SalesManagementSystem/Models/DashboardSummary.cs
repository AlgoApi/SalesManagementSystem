namespace SalesManagementSystem.Models;

public sealed class DashboardSummary
{
    public int ProductCount { get; set; }
    public int CustomerCount { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public int LowStockCount { get; set; }
}
