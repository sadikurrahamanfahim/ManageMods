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
        public async Task<IActionResult> Create(Product model, List<IFormFile>? images)
        {
            if (!ModelState.IsValid)
                return View(model);

            var imageUrlList = new List<string>();

            if (images != null && images.Any())
            {
                var bucket = _configuration["Supabase:StorageBucket:Products"];

                foreach (var image in images)
                {
                    if (image != null && image.Length > 0)
                    {
                        var filePath = await _supabaseStorage.UploadFile(image, bucket, "images");

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            var publicUrl = _supabaseStorage.GetPublicUrl(bucket, filePath);
                            imageUrlList.Add(publicUrl);
                        }
                    }
                }
            }

            if (imageUrlList.Any())
            {
                model.Images = imageUrlList;
                model.ImageUrl = imageUrlList.First(); // Set first image as main image for backward compatibility
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
        public async Task<IActionResult> Edit(Product model, List<IFormFile>? images, string? existingImages)
        {
            if (!ModelState.IsValid)
                return View(model);

            var imageUrlList = new List<string>();

            // Keep existing images if provided
            if (!string.IsNullOrEmpty(existingImages))
            {
                imageUrlList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(existingImages) ?? new List<string>();
            }

            // Add new images
            if (images != null && images.Any())
            {
                var bucket = _configuration["Supabase:StorageBucket:Products"];

                foreach (var image in images)
                {
                    if (image != null && image.Length > 0)
                    {
                        var filePath = await _supabaseStorage.UploadFile(image, bucket, "images");

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            var publicUrl = _supabaseStorage.GetPublicUrl(bucket, filePath);
                            imageUrlList.Add(publicUrl);
                        }
                    }
                }
            }

            if (imageUrlList.Any())
            {
                model.Images = imageUrlList;
                model.ImageUrl = imageUrlList.First();
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
            var product = await _productService.GetProductById(id);

            if (product == null)
                return Json(new { success = false, message = "Product not found" });

            // Delete product image from Supabase if exists
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                try
                {
                    var bucket = _configuration["Supabase:StorageBucket:Products"];
                    var uri = new Uri(product.ImageUrl);
                    var segments = uri.Segments;

                    // Extract the file path (images/filename.jpg)
                    if (segments.Length >= 2)
                    {
                        var folder = segments[segments.Length - 2].TrimEnd('/');
                        var fileName = segments[segments.Length - 1];
                        var filePath = $"{folder}/{fileName}";

                        await _supabaseStorage.DeleteFile(bucket, filePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting image: {ex.Message}");
                    // Continue with product deletion even if image delete fails
                }
            }

            // Permanently delete from database
            var result = await _productService.DeleteProduct(id);

            if (result)
                return Json(new { success = true, message = "Product and images deleted successfully" });

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

        // Add endpoint to delete individual image
        [Authorize("admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteProductImage(Guid productId, string imageUrl)
        {
            var product = await _productService.GetProductById(productId);

            if (product == null)
                return Json(new { success = false, message = "Product not found" });

            var images = product.Images;
            images.Remove(imageUrl);

            // Delete from Supabase
            try
            {
                var bucket = _configuration["Supabase:StorageBucket:Products"];
                var uri = new Uri(imageUrl);
                var segments = uri.Segments;

                if (segments.Length >= 2)
                {
                    var folder = segments[segments.Length - 2].TrimEnd('/');
                    var fileName = segments[segments.Length - 1];
                    var filePath = $"{folder}/{fileName}";

                    await _supabaseStorage.DeleteFile(bucket, filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image: {ex.Message}");
            }

            product.Images = images;
            if (images.Any())
            {
                product.ImageUrl = images.First();
            }
            else
            {
                product.ImageUrl = null;
            }

            await _productService.UpdateProduct(product);

            return Json(new { success = true, message = "Image deleted successfully" });
        }
    }
}