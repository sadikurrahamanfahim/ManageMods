using Microsoft.AspNetCore.Mvc;

namespace OrderManagementSystem.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
