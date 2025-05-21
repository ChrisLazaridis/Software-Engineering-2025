using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace SoftEng2025.Models
{
    [Table("Restaurant")]
    public class Restaurant
    {
        [Key]
        public int RestaurantId { get; set; }

        [Required]
        public int EntrepreneurId { get; set; }

        [ForeignKey(nameof(EntrepreneurId))]
        public Entrepreneur Entrepreneur { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; }

        public string? Website { get; set; }

        [Required, Column(TypeName = "geography")]
        public Point Location { get; set; }

        [Required]
        public int Seats { get; set; }

        [Required, MaxLength(255)]
        public string RestaurantType { get; set; }

        public double AverageRating { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RestaurantImage> Images { get; set; } = new List<RestaurantImage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}