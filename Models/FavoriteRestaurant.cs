using SoftEng2025.Models.Common;

namespace SoftEng2025.Models
{
    public class FavoriteRestaurant : FavoriteBase
    {
        public int CriticId { get; set; }
        public Critic Critic { get; set; }

        public int RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }
    }
}