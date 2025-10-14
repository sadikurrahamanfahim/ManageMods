using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Services.Interfaces;
using OrderManagementSystem.Helpers;

namespace OrderManagementSystem.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [Authorize("admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetAllCustomers();
            return View(customers);
        }

        [Authorize("admin")]
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var customer = await _customerService.GetCustomerById(id);

            if (customer == null)
            {
                TempData["Error"] = "Customer not found";
                return RedirectToAction("Index");
            }

            var orders = await _customerService.GetCustomerOrders(id);
            ViewBag.Orders = orders;

            return View(customer);
        }
    }
}