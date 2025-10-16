using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagementSystem.Models.Entities
{
    [Table("products")]
    public class Product
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Column("variants")]
        public string? Variants { get; set; }

        [Column("stock_quantity")]
        public int StockQuantity { get; set; } = 0;

        [Column("buying_price", TypeName = "decimal(10,2)")]
        public decimal? BuyingPrice { get; set; }

        [Column("selling_price", TypeName = "decimal(10,2)")]
        public decimal SellingPrice { get; set; }

        [Column("image_url")]
        public string? ImageUrl { get; set; } // Keep for backward compatibility

        [Column("image_urls")]
        public string? ImageUrls { get; set; } // JSON array of multiple images

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        [NotMapped] // This property won't be stored in database
        public List<string> Images
        {
            get
            {
                if (!string.IsNullOrEmpty(ImageUrls))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(ImageUrls) ?? new List<string>();
                }

                // Fallback to single image for backward compatibility
                if (!string.IsNullOrEmpty(ImageUrl))
                {
                    return new List<string> { ImageUrl };
                }

                return new List<string>();
            }
            set
            {
                ImageUrls = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }
    }
}