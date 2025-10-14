using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Services.Interfaces;
using OrderManagementSystem.Models.ViewModels;
using OrderManagementSystem.Helpers;

namespace OrderManagementSystem.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly IFileService _fileService;

        public OrdersController(
            IOrderService orderService,
            IProductService productService,
            IFileService fileService)
        {
            _orderService = orderService;
            _productService = productService;
            _fileService = fileService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var orders = await _orderService.GetAllOrders(status, search);

            ViewBag.CurrentStatus = status;
            ViewBag.SearchTerm = search;

            return View(orders);
        }

        [Authorize("admin", "order_creator")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var products = await _productService.GetAllProducts();
            ViewBag.Products = products;
            return View();
        }

        [Authorize("admin", "order_creator")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderViewModel model, IFormFile? screenshot)
        {
            if (!ModelState.IsValid)
            {
                var products = await _productService.GetAllProducts();
                ViewBag.Products = products;
                return View(model);
            }

            // Upload screenshot if provided
            if (screenshot != null && screenshot.Length > 0)
            {
                model.ScreenshotUrl = await _fileService.UploadFile(screenshot, "orders");
            }

            var userId = Guid.Parse(HttpContext.Session.GetString("UserId")!);
            var result = await _orderService.CreateOrder(model, userId);

            if (result)
            {
                TempData["Success"] = "Order created successfully!";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Failed to create order. Please try again.";
            var productsList = await _productService.GetAllProducts();
            ViewBag.Products = productsList;
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var order = await _orderService.GetOrderById(id);

            if (order == null)
            {
                TempData["Error"] = "Order not found";
                return RedirectToAction("Index");
            }

            return View(order);
        }

        [Authorize("admin", "delivery_handler")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(Guid id, string status, string? trackingNumber, string? comment)
        {
            var userId = Guid.Parse(HttpContext.Session.GetString("UserId")!);

            var order = await _orderService.GetOrderById(id);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            // Update tracking number if provided
            if (!string.IsNullOrEmpty(trackingNumber))
            {
                order.DeliveryTrackingNumber = trackingNumber;
                await _orderService.UpdateOrder(order);
            }

            var result = await _orderService.UpdateOrderStatus(id, status, userId, comment);

            if (result)
            {
                return Json(new { success = true, message = "Order status updated successfully" });
            }

            return Json(new { success = false, message = "Failed to update status" });
        }
    }
}