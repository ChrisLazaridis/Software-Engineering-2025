using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;

namespace SoftEng2025.Pages.Entrepreneur
{
    [Authorize]  // only logged-in users
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public string Username { get; set; }

        public class RestaurantCard
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string ImageBase64 { get; set; }
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
        }

        public List<RestaurantCard> Cards { get; set; }

        public IndexModel(ApplicationDbContext db)
            => _db = db;

        public async Task<IActionResult> OnGetAsync()
        {
            // 1) Find the current user's Entrepreneur record
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var entrepreneur = await _db.Entrepreneurs
                .Include(e => e.Restaurants)
                  .ThenInclude(r => r.Images)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (entrepreneur == null)
                return Forbid();    // not an entrepreneur, no access

            Username = entrepreneur.FirstName;

            // 2) Map restaurants to card view-models
            Cards = entrepreneur.Restaurants
                .Select(r => new RestaurantCard
                {
                    Id = r.RestaurantId,
                    Name = r.Name,
                    AverageRating = System.Math.Round(System.Math.Min(r.AverageRating, 5.0), 2),
                    ReviewCount = r.ReviewCount,
                    ImageBase64 = r.Images.FirstOrDefault() != null
                        ? $"data:image/jpeg;base64,{System.Convert.ToBase64String(r.Images.First().ImageData)}"
                        : null
                })
                .ToList();

            return Page();
        }
    }
}
