using OrderManagementSystem.Models.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagementSystem.Models.Entities
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [Column("order_number")]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Column("customer_id")]
        public Guid CustomerId { get; set; }

        [Required]
        [Column("customer_name")]
        [MaxLength(255)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [Column("customer_phone")]
        [MaxLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required]
        [Column("customer_address")]
        public string CustomerAddress { get; set; } = string.Empty;

        [Column("product_id")]
        public Guid? ProductId { get; set; }

        [Required]
        [Column("product_name")]
        [MaxLength(255)]
        public string ProductName { get; set; } = string.Empty;

        [Column("product_color")]
        [MaxLength(100)]
        public string? ProductColor { get; set; }

        [Column("product_quantity")]
        public int ProductQuantity { get; set; } = 1;

        [Required]
        [Column("product_price", TypeName = "decimal(10,2)")]
        public decimal ProductPrice { get; set; }

        [Column("screenshot_url")]
        public string? ScreenshotUrl { get; set; }

        [Column("order_notes")]
        public string? OrderNotes { get; set; }

        [Required]
        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = "pending";

        [Column("delivery_tracking_number")]
        [MaxLength(100)]
        public string? DeliveryTrackingNumber { get; set; }

        [Column("delivery_receipt_url")]
        public string? DeliveryReceiptUrl { get; set; }

        [Column("cancellation_reason")]
        public string? CancellationReason { get; set; }

        [Column("created_by")]
        public Guid CreatedBy { get; set; }

        [Column("processed_by")]
        public Guid? ProcessedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; } = null!;

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [ForeignKey("CreatedBy")]
        public User Creator { get; set; } = null!;

        [ForeignKey("ProcessedBy")]
        public User? Processor { get; set; }

        public ICollection<OrderHistory> OrderHistories { get; set; } = new List<OrderHistory>();

        [Column("order_items")]
        public string? OrderItems { get; set; } // JSON array of order items

        [NotMapped]
        public List<OrderItemViewModel> Items
        {
            get
            {
                if (!string.IsNullOrEmpty(OrderItems))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<OrderItemViewModel>>(OrderItems)
                        ?? new List<OrderItemViewModel>();
                }
                return new List<OrderItemViewModel>();
            }
            set
            {
                OrderItems = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }

        [Column("screenshot_urls")]
        public string? ScreenshotUrls { get; set; } // JSON array of multiple screenshots

        [NotMapped]
        public List<string> Screenshots
        {
            get
            {
                if (!string.IsNullOrEmpty(ScreenshotUrls))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(ScreenshotUrls)
                        ?? new List<string>();
                }

                // Fallback to single screenshot for backward compatibility
                if (!string.IsNullOrEmpty(ScreenshotUrl))
                {
                    return new List<string> { ScreenshotUrl };
                }

                return new List<string>();
            }
            set
            {
                ScreenshotUrls = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }

        // Add computed property for total
        [NotMapped]
        public decimal TotalAmount
        {
            get
            {
                if (Items != null && Items.Any())
                {
                    return Items.Sum(i => i.Price * i.Quantity);
                }
                return ProductPrice * ProductQuantity;
            }
        }
    }
}