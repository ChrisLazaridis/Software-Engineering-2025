using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftEng2025.Models
{
    [Table("RestaurantImage")]
    public class RestaurantImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public Restaurant Restaurant { get; set; }

        [Required]
        public byte[] ImageData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}