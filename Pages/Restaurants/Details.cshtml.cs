// Pages/Restaurants/Details.cshtml.cs
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;

namespace SoftEng2025.Pages.Restaurants
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public DetailsModel(ApplicationDbContext db,
                            UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public Restaurant Restaurant { get; set; }

        // New properties to control the Add Review UI
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

            // If the current user is in the Critic role, check if they've already reviewed
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

        // Only Admins can delete any review
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
