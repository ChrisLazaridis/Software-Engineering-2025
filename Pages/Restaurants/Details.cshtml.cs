using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;
using SoftEng2025.Services;    // <-- geocoding service namespace

namespace SoftEng2025.Pages.Restaurants
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IGeocodingService _geocoder;

        public DetailsModel(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            IGeocodingService geocoder)
        {
            _db = db;
            _userManager = userManager;
            _geocoder = geocoder;
        }

        public Restaurant Restaurant { get; set; }

        // new: human‐readable address
        public string Address { get; set; }

        public bool CanAddReview { get; set; }
        public int? MyReviewId { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Restaurant = await _db.Restaurants
                .Include(r => r.Entrepreneur)
                .Include(r => r.Images)
                .Include(r => r.Reviews)
                  .ThenInclude(rv => rv.Critic)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RestaurantId == id);

            if (Restaurant == null)
                return NotFound();

            // reverse‐geocode
            Address = await _geocoder.GetAddressAsync(
                Restaurant.Location.Y,
                Restaurant.Location.X
            );

            if (User.IsInRole("Critic"))
            {
                var userId = _userManager.GetUserId(User);
                var critic = await _db.Critics
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (critic != null)
                {
                    var existing = Restaurant.Reviews
                        .FirstOrDefault(rv => rv.CriticId == critic.CriticId);
                    if (existing == null)
                        CanAddReview = true;
                    else
                        MyReviewId = existing.ReviewId;
                }
            }

            return Page();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var review = await _db.Reviews.FindAsync(id);
            if (review == null)
                return NotFound();

            var restaurantId = review.RestaurantId;
            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();
            return RedirectToPage(new { id = restaurantId });
        }
    }
}
