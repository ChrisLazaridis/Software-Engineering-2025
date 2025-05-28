using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SoftEng2025.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public IList<Restaurant> Recent { get; set; }

        public async Task OnGetAsync()
        {
            Recent = await _db.Restaurants
                .Include(r => r.Entrepreneur)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // Load restaurant with its children
            var restaurant = await _db.Restaurants
                .Include(r => r.Images)   // navigation property ICollection<RestaurantImage> :contentReference[oaicite:0]{index=0}
                .Include(r => r.Reviews)  // navigation property ICollection<Review> :contentReference[oaicite:1]{index=1}
                .FirstOrDefaultAsync(r => r.RestaurantId == id);

            if (restaurant == null)
                return NotFound();

            // Delete all associated images
            if (restaurant.Images.Any())
            {
                _db.RestaurantImages.RemoveRange(restaurant.Images);
            }

            // Delete all associated reviews
            if (restaurant.Reviews.Any())
            {
                _db.Reviews.RemoveRange(restaurant.Reviews);
            }

            // Finally, delete the restaurant
            _db.Restaurants.Remove(restaurant);

            await _db.SaveChangesAsync();  // will cascade to FK tables safely :contentReference[oaicite:2]{index=2}

            return RedirectToPage();
        }

        public IActionResult OnPostFlag(int id)
        {
            // placeholder – you can log or e-mail a warning here
            return RedirectToPage();
        }
    }
}
