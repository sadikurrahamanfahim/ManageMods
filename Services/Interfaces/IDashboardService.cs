using OrderManagementSystem.Models.ViewModels;

namespace OrderManagementSystem.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardData(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<SalesChartData>> GetSalesChartData(DateTime startDate, DateTime endDate);
        Task<List<OrderStatusData>> GetOrderStatusData();
    }
}