using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Services.Interfaces;
using OrderManagementSystem.Models.Entities;
using OrderManagementSystem.Helpers;

namespace OrderManagementSystem.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ISupabaseStorageService _supabaseStorage;
        private readonly IConfiguration _configuration;

        public ProductsController(
            IProductService productService,
            ISupabaseStorageService supabaseStorage,
            IConfiguration configuration)
        {
            _productService = productService;
            _supabaseStorage = supabaseStorage;
            _configuration = configuration;
        }

        [Authorize("admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProducts(false);
            return View(products);
        }

        [Authorize("admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize("admin")]
        [HttpPost]
        public async Task<IActionResult> Create(Product model, IFormFile? image)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (image != null && image.Length > 0)
            {
                var bucket = _configuration["Supabase:StorageBucket:Products"];
                var filePath = await _supabaseStorage.UploadFile(image, bucket, "images");

                if (!string.IsNullOrEmpty(filePath))
                {
                    model.ImageUrl = _supabaseStorage.GetPublicUrl(bucket, filePath);
                }
            }

            var result = await _productService.CreateProduct(model);

            if (result)
            {
                TempData["Success"] = "Product created successfully!";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Failed to create product";
            return View(model);
        }

        [Authorize("admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var product = await _productService.GetProductById(id);
            if (product == null)
            {
                TempData["Error"] = "Product not found";
                return RedirectToAction("Index");
            }
            return View(product);
        }

        [Authorize("admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(Product model, IFormFile? image)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (image != null && image.Length > 0)
            {
                var bucket = _configuration["Supabase:StorageBucket:Products"];

                // Delete old image if exists
                if (!string.IsNullOrEmpty(model.ImageUrl))
                {
                    // Extract file path from URL
                    var uri = new Uri(model.ImageUrl);
                    var segments = uri.Segments;
                    var filePath = segments[segments.Length - 1];
                    await _supabaseStorage.DeleteFile(bucket, $"images/{filePath}");
                }

                // Upload new image
                var newFilePath = await _supabaseStorage.UploadFile(image, bucket, "images");
                if (!string.IsNullOrEmpty(newFilePath))
                {
                    model.ImageUrl = _supabaseStorage.GetPublicUrl(bucket, newFilePath);
                }
            }

            var result = await _productService.UpdateProduct(model);

            if (result)
            {
                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Failed to update product";
            return View(model);
        }

        [Authorize("admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _productService.DeleteProduct(id);

            if (result)
                return Json(new { success = true, message = "Product deleted successfully" });

            return Json(new { success = false, message = "Failed to delete product" });
        }

        [Authorize("admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateStock(Guid id, int quantity)
        {
            var result = await _productService.UpdateStock(id, quantity);

            if (result)
                return Json(new { success = true, message = "Stock updated successfully" });

            return Json(new { success = false, message = "Failed to update stock" });
        }
    }
}