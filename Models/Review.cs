// --- Models/Review.cs ---
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SoftEng2025.Models.Common;

namespace SoftEng2025.Models
{
    [Table("Review")]
    public class Review : ITrackable
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int RestaurantId { get; set; }
        [ForeignKey(nameof(RestaurantId))]
        public Restaurant Restaurant { get; set; }

        [Required]
        public int CriticId { get; set; }
        [ForeignKey(nameof(CriticId))]
        public Critic Critic { get; set; }

        [Required, Column(TypeName = "numeric(3,2)")]
        public decimal Food { get; set; }
        [Required, Column(TypeName = "numeric(3,2)")]
        public decimal Service { get; set; }
        [Required, Column(TypeName = "numeric(3,2)")]
        public decimal Price { get; set; }
        [Required, Column(TypeName = "numeric(3,2)")]
        public decimal Condition { get; set; }
        [Required, Column(TypeName = "numeric(3,2)")]
        public decimal Atmosphere { get; set; }

        [Column(TypeName = "numeric(3,2)")]
        public decimal? Average { get; set; }

        [Required]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}