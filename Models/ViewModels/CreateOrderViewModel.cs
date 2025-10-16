using System.ComponentModel.DataAnnotations;

namespace OrderManagementSystem.Models.ViewModels
{
    public class CreateOrderViewModel
    {
        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required]
        public string CustomerAddress { get; set; } = string.Empty;

        // For multiple products - JSON string
        public string? OrderItemsJson { get; set; }

        // For backward compatibility - single product (deprecated)
        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductColor { get; set; }
        public int ProductQuantity { get; set; } = 1;
        public decimal ProductPrice { get; set; }

        public string? OrderNotes { get; set; }

        // Multiple screenshots
        public string? ScreenshotUrls { get; set; }
    }

    public class OrderItemViewModel
    {
        public Guid? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductColor { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal Price { get; set; }
    }
}