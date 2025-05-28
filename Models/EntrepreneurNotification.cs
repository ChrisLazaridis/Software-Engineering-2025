using Microsoft.AspNetCore.Identity;
using SoftEng2025.Models.Common;

namespace SoftEng2025.Models
{
    public class EntrepreneurNotification : NotificationBase
    {
        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public int ReviewId { get; set; }
        public Review Review { get; set; }
    }
}