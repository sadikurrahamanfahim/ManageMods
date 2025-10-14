using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Services.Interfaces;
using OrderManagementSystem.Helpers;

namespace OrderManagementSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IOrderService _orderService;

        public DashboardController(IDashboardService dashboardService, IOrderService orderService)
        {
            _dashboardService = dashboardService;
            _orderService = orderService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            // Default to last 30 days
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            var dashboardData = await _dashboardService.GetDashboardData(startDate, endDate);
            var salesChartData = await _dashboardService.GetSalesChartData(startDate.Value, endDate.Value);
            var orderStatusData = await _dashboardService.GetOrderStatusData();

            ViewBag.SalesChartData = salesChartData;
            ViewBag.OrderStatusData = orderStatusData;

            return View(dashboardData);
        }
    }
}