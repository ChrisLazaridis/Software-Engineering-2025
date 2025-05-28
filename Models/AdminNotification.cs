using SoftEng2025.Models.Common;

namespace SoftEng2025.Models
{
    public class AdminNotification : NotificationBase
    {
        public int? RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }

        public int? ReviewId { get; set; }
        public Review Review { get; set; }
    }
}