using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Services.Interfaces;
using OrderManagementSystem.Models.ViewModels;
using OrderManagementSystem.Helpers;
using System.Text.Json;

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

        [Authorize("admin", "delivery_handler")]
        [HttpGet]
        public async Task<IActionResult> ProcessOrders()
        {
            // Get orders that are ready for delivery (status: delivery_submitted)
            var orders = await _orderService.GetAllOrders("delivery_submitted", null);
            return View(orders);
        }

        [Authorize("admin", "delivery_handler")]
        [HttpPost]
        public async Task<IActionResult> MarkOutForDelivery(Guid id)
        {
            var userId = Guid.Parse(HttpContext.Session.GetString("UserId")!);

            var result = await _orderService.UpdateOrderStatus(
                id,
                "out_for_delivery",
                userId,
                "Order marked for delivery"
            );

            if (result)
            {
                return Json(new { success = true, message = "Order marked for delivery successfully" });
            }

            return Json(new { success = false, message = "Failed to update status" });
        }

        [Authorize("admin", "delivery_handler")]
        [HttpGet]
        public async Task<IActionResult> PendingOrders()
        {
            var orders = await _orderService.GetAllOrders("pending", null);
            return View(orders);
        }



        [Authorize("admin", "delivery_handler")]
        [HttpPost]
        public async Task<IActionResult> SendToSteadfast([FromBody] SendToSteadfastRequest data)
        {
            try
            {
                if (data == null || data.Request == null)
                {
                    return Json(new { success = false, message = "Invalid request data" });
                }

                var order = await _orderService.GetOrderById(data.OrderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                if (order.SentToSteadfast)
                {
                    return Json(new { success = false, message = "Order already sent to Steadfast" });
                }

                // Validate request
                var validation = SteadfastValidator.ValidateOrder(
                    data.Request.RecipientName,
                    data.Request.RecipientPhone,
                    data.Request.RecipientAddress,
                    data.Request.CodAmount
                );

                if (!validation.isValid)
                {
                    return Json(new { success = false, message = validation.errorMessage });
                }

                // Send to Steadfast
                var steadfastService = HttpContext.RequestServices.GetRequiredService<ISteadfastService>();
                var response = await steadfastService.CreateOrder(data.Request);

                if (response.Status == 200 && response.Consignment != null)
                {
                    // Update order with Steadfast details
                    order.SteadfastConsignmentId = response.Consignment.ConsignmentId;
                    order.SteadfastTrackingCode = response.Consignment.TrackingCode;
                    order.SteadfastStatus = response.Consignment.Status;
                    order.SentToSteadfast = true;
                    order.SentToSteadfastAt = DateTime.UtcNow;
                    order.Status = "delivery_submitted";
                    order.UpdatedAt = DateTime.UtcNow;

                    // Update alternative phone and email if provided
                    if (!string.IsNullOrEmpty(data.Request.AlternativePhone))
                        order.AlternativePhone = data.Request.AlternativePhone;
                    if (!string.IsNullOrEmpty(data.Request.RecipientEmail))
                        order.RecipientEmail = data.Request.RecipientEmail;

                    await _orderService.UpdateOrder(order);

                    // Add to history
                    var userId = Guid.Parse(HttpContext.Session.GetString("UserId")!);
                    await _orderService.UpdateOrderStatus(order.Id, "delivery_submitted", userId,
                        $"Sent to Steadfast. Tracking Code: {response.Consignment.TrackingCode}");

                    return Json(new
                    {
                        success = true,
                        message = "Order sent to Steadfast successfully",
                        trackingCode = response.Consignment.TrackingCode,
                        consignmentId = response.Consignment.ConsignmentId
                    });
                }
                else
                {
                    return Json(new { success = false, message = response.Message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendToSteadfast Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Add this helper class at the end of the controller or in Models/ViewModels
        public class SendToSteadfastRequest
        {
            public Guid OrderId { get; set; }
            public SteadfastOrderRequest Request { get; set; }
        }

        //[Authorize("admin", "delivery_handler")]
        //[HttpPost]
        //public async Task<IActionResult> SendToSteadfast(Guid orderId, SteadfastOrderRequest request)
        //{

        //}

        [Authorize("admin", "delivery_handler")]
        [HttpGet]
        public async Task<IActionResult> CheckSteadfastStatus(string trackingCode)
        {
            try
            {
                var steadfastService = HttpContext.RequestServices.GetRequiredService<ISteadfastService>();
                var response = await steadfastService.CheckDeliveryStatus(trackingCode);

                return Json(new
                {
                    success = true,
                    status = response.DeliveryStatus
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [Authorize("admin", "delivery_handler")]
        [HttpPost]
        public async Task<IActionResult> BulkSendToSteadfast([FromBody] List<BulkOrderRequest> requests)
        {
            try
            {
                var steadfastService = HttpContext.RequestServices.GetRequiredService<ISteadfastService>();
                var userId = Guid.Parse(HttpContext.Session.GetString("UserId")!);

                var results = new List<object>();

                foreach (var req in requests.Take(50)) // Max 50 at a time
                {
                    var order = await _orderService.GetOrderById(Guid.Parse(req.OrderId));

                    if (order == null || order.SentToSteadfast)
                    {
                        results.Add(new
                        {
                            orderId = req.OrderId,
                            success = false,
                            message = "Order not found or already sent"
                        });
                        continue;
                    }

                    try
                    {
                        var steadfastRequest = new SteadfastOrderRequest
                        {
                            Invoice = req.Invoice,
                            RecipientName = req.Recipient_Name,
                            RecipientPhone = req.Recipient_Phone,
                            RecipientAddress = req.Recipient_Address,
                            CodAmount = req.Cod_Amount,
                            Note = req.Note,
                            AlternativePhone = req.Alternative_Phone,
                            RecipientEmail = req.Recipient_Email,
                            ItemDescription = req.Item_Description,
                            TotalLot = req.Total_Lot,
                            DeliveryType = req.Delivery_Type
                        };

                        var response = await steadfastService.CreateOrder(steadfastRequest);

                        if (response.Status == 200 && response.Consignment != null)
                        {
                            order.SteadfastConsignmentId = response.Consignment.ConsignmentId;
                            order.SteadfastTrackingCode = response.Consignment.TrackingCode;
                            order.SteadfastStatus = response.Consignment.Status;
                            order.SentToSteadfast = true;
                            order.SentToSteadfastAt = DateTime.UtcNow;
                            order.Status = "delivery_submitted";
                            order.UpdatedAt = DateTime.UtcNow;

                            // Update alternative phone and email if provided
                            if (!string.IsNullOrEmpty(req.Alternative_Phone))
                                order.AlternativePhone = req.Alternative_Phone;
                            if (!string.IsNullOrEmpty(req.Recipient_Email))
                                order.RecipientEmail = req.Recipient_Email;

                            await _orderService.UpdateOrder(order);
                            await _orderService.UpdateOrderStatus(
                                order.Id,
                                "delivery_submitted",
                                userId,
                                $"Sent to Steadfast (Bulk). Tracking: {response.Consignment.TrackingCode}"
                            );

                            results.Add(new
                            {
                                orderId = req.OrderId,
                                success = true,
                                trackingCode = response.Consignment.TrackingCode
                            });
                        }
                        else
                        {
                            results.Add(new
                            {
                                orderId = req.OrderId,
                                success = false,
                                message = response.Message
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            orderId = req.OrderId,
                            success = false,
                            message = ex.Message
                        });
                    }

                    // Add small delay between requests to avoid rate limiting
                    await Task.Delay(500);
                }

                var successCount = results.Count(r => ((dynamic)r).success);
                var failCount = results.Count - successCount;

                return Json(new
                {
                    success = true,
                    message = $"Sent {successCount} orders successfully. {failCount} failed.",
                    results = results
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Add this helper class
        public class BulkOrderRequest
        {
            public string OrderId { get; set; }
            public string Invoice { get; set; }
            public string Recipient_Name { get; set; }
            public string Recipient_Phone { get; set; }
            public string Recipient_Address { get; set; }
            public decimal Cod_Amount { get; set; }
            public string? Note { get; set; }
            public string? Alternative_Phone { get; set; }
            public string? Recipient_Email { get; set; }
            public string? Item_Description { get; set; }
            public int? Total_Lot { get; set; }
            public int Delivery_Type { get; set; }
        }
    }
}