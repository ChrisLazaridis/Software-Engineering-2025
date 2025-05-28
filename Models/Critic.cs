// --- Models/Critic.cs ---
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SoftEng2025.Models.Common;

namespace SoftEng2025.Models
{
    [Table("Critic")]
    public class Critic : Person
    {
        [Key]
        public int CriticId { get; set; }

        // Default scoring biases
        public int Food { get; set; } = 5;
        public int Service { get; set; } = 4;
        public int Price { get; set; } = 3;
        public int Condition { get; set; } = 2;
        public int Atmosphere { get; set; } = 1;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // ← NEW: navigation for favorites
        public ICollection<FavoriteRestaurant> FavoriteRestaurants { get; set; }
            = new List<FavoriteRestaurant>();
    }
}