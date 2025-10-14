using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagementSystem.Models.Entities
{
    [Table("order_history")]
    public class OrderHistory
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("order_id")]
        public Guid OrderId { get; set; }

        [Column("changed_by")]
        public Guid ChangedBy { get; set; }

        [Column("previous_status")]
        [MaxLength(50)]
        public string? PreviousStatus { get; set; }

        [Required]
        [Column("new_status")]
        [MaxLength(50)]
        public string NewStatus { get; set; } = string.Empty;

        [Column("comment")]
        public string? Comment { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;

        [ForeignKey("ChangedBy")]
        public User ChangedByUser { get; set; } = null!;
    }
}