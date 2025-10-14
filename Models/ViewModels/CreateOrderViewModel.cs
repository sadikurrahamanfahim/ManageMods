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

        public Guid? ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string? ProductColor { get; set; }

        public int ProductQuantity { get; set; } = 1;

        [Required]
        public decimal ProductPrice { get; set; }

        public string? ScreenshotUrl { get; set; }

        public string? OrderNotes { get; set; }
    }
}