using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
        public DetailsModel(ApplicationDbContext db) => _db = db;

        public Restaurant Restaurant { get; set; }

        // Load restaurant + reviews
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

            return Page();
        }

        // Only Admins can delete
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // Find the review
            var review = await _db.Reviews.FindAsync(id);
            if (review == null)
                return NotFound();

            // Capture restaurantId to redirect back
            var restaurantId = review.RestaurantId;

            // Remove and save
            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();

            // Redirect to this same details page
            return RedirectToPage(new { id = restaurantId });
        }
    }
}