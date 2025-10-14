using Microsoft.EntityFrameworkCore;
using OrderManagementSystem.Data;
using OrderManagementSystem.Models.ViewModels;
using OrderManagementSystem.Services.Interfaces;

namespace OrderManagementSystem.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardData(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            var completedOrders = orders.Where(o => o.Status == "completed").ToList();

            return new DashboardViewModel
            {
                TotalRevenue = completedOrders.Sum(o => o.ProductPrice * o.ProductQuantity),
                TotalOrders = completedOrders.Count,
                AverageOrderValue = completedOrders.Any()
                    ? completedOrders.Average(o => o.ProductPrice * o.ProductQuantity)
                    : 0,
                PendingOrders = orders.Count(o => o.Status == "pending")
            };
        }

        public async Task<List<SalesChartData>> GetSalesChartData(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Where(o => o.Status == "completed" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            return orders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new SalesChartData
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Revenue = g.Sum(o => o.ProductPrice * o.ProductQuantity)
                })
                .OrderBy(x => x.Date)
                .ToList();
        }

        public async Task<List<OrderStatusData>> GetOrderStatusData()
        {
            return await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusData
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
        }
    }
}