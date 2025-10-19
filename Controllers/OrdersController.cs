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



        [HttpPost]
        public async Task<IActionResult> SendToSteadfast(Guid orderId, SteadfastOrderRequest request)
        {
            try
            {
                // Validate request
                var validation = SteadfastValidator.ValidateOrder(
                    request.RecipientName,
                    request.RecipientPhone,
                    request.RecipientAddress,
                    request.CodAmount
                );

                if (!validation.isValid)
                {
                    return Json(new { success = false, message = validation.errorMessage });
                }

                // Rest of the code...
                try
                {
                    var order = await _orderService.GetOrderById(orderId);

                    if (order == null)
                    {
                        return Json(new { success = false, message = "Order not found" });
                    }

                    if (order.SentToSteadfast)
                    {
                        return Json(new { success = false, message = "Order already sent to Steadfast" });
                    }

                    // Send to Steadfast
                    var steadfastService = HttpContext.RequestServices.GetRequiredService<ISteadfastService>();
                    var response = await steadfastService.CreateOrder(request);

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

                        await _orderService.UpdateOrder(order);

                        // Add to history
                        var userId = Guid.Parse(HttpContext.Session.GetString("UserId")!);
                        await _orderService.UpdateOrderStatus(orderId, "delivery_submitted", userId,
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
                    return Json(new { success = false, message = $"Error: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
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

        // Bulk send orders to Steadfast
        [Authorize("admin")]
        [HttpPost]
        public async Task<IActionResult> BulkSendToSteadfast(List<Guid> orderIds)
        {
            try
            {
                var steadfastService = HttpContext.RequestServices.GetRequiredService<ISteadfastService>();
                var userId = Guid.Parse(HttpContext.Session.GetString("UserId")!);

                var results = new List<object>();

                foreach (var orderId in orderIds.Take(50)) // Max 50 at a time
                {
                    var order = await _orderService.GetOrderById(orderId);

                    if (order == null || order.SentToSteadfast)
                    {
                        continue;
                    }

                    try
                    {
                        var request = new SteadfastOrderRequest
                        {
                            Invoice = order.OrderNumber,
                            RecipientName = order.CustomerName,
                            RecipientPhone = order.CustomerPhone,
                            RecipientAddress = order.CustomerAddress,
                            CodAmount = order.TotalAmount,
                            Note = order.OrderNotes,
                            AlternativePhone = order.AlternativePhone,
                            RecipientEmail = order.RecipientEmail,
                            ItemDescription = order.Items != null && order.Items.Any()
                                ? string.Join(", ", order.Items.Select(i => i.ProductName))
                                : order.ProductName,
                            TotalLot = order.Items?.Count ?? 1,
                            DeliveryType = 0
                        };

                        var response = await steadfastService.CreateOrder(request);

                        if (response.Status == 200 && response.Consignment != null)
                        {
                            order.SteadfastConsignmentId = response.Consignment.ConsignmentId;
                            order.SteadfastTrackingCode = response.Consignment.TrackingCode;
                            order.SteadfastStatus = response.Consignment.Status;
                            order.SentToSteadfast = true;
                            order.SentToSteadfastAt = DateTime.UtcNow;
                            order.Status = "delivery_submitted";

                            await _orderService.UpdateOrder(order);

                            results.Add(new
                            {
                                orderId = orderId,
                                success = true,
                                trackingCode = response.Consignment.TrackingCode
                            });
                        }
                        else
                        {
                            results.Add(new
                            {
                                orderId = orderId,
                                success = false,
                                message = response.Message
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            orderId = orderId,
                            success = false,
                            message = ex.Message
                        });
                    }
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
    }
}