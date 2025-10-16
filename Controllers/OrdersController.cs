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
        private readonly ISupabaseStorageService _supabaseStorage; // Changed from IFileService
        private readonly IConfiguration _configuration;

        public OrdersController(
            IOrderService orderService,
            IProductService productService,
            ISupabaseStorageService supabaseStorage, // Changed
            IConfiguration configuration)
        {
            _orderService = orderService;
            _productService = productService;
            _supabaseStorage = supabaseStorage;
            _configuration = configuration;
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
        public async Task<IActionResult> Create(CreateOrderViewModel model, List<IFormFile>? screenshots)
        {
            if (!ModelState.IsValid)
            {
                var products = await _productService.GetAllProducts();
                ViewBag.Products = products;
                return View(model);
            }

            // Upload multiple screenshots to Supabase if provided
            var screenshotUrlList = new List<string>();
            if (screenshots != null && screenshots.Any())
            {
                var bucket = _configuration["Supabase:StorageBucket:Orders"];

                foreach (var screenshot in screenshots)
                {
                    if (screenshot != null && screenshot.Length > 0)
                    {
                        var filePath = await _supabaseStorage.UploadFile(screenshot, bucket, "screenshots");

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            var publicUrl = _supabaseStorage.GetPublicUrl(bucket, filePath);
                            screenshotUrlList.Add(publicUrl);
                        }
                    }
                }
            }

            if (screenshotUrlList.Any())
            {
                model.ScreenshotUrls = System.Text.Json.JsonSerializer.Serialize(screenshotUrlList);
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term)
        {
            var products = await _productService.GetAllProducts();

            var results = products
                .Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.SellingPrice,
                    stock = p.StockQuantity,
                    variants = p.Variants
                });

            return Json(results);
        }
    }
}