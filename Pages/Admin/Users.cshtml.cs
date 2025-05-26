using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SoftEng2025.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public UsersModel(ApplicationDbContext db) => _db = db;

        // Tuple: (critic entity, total reviews, last 3 reviews)
        public IList<(Critic critic, int reviewCount, IEnumerable<Review> recent)> TopUsers { get; set; }

        public async Task OnGetAsync()
        {
            var critics = await _db.Critics
                .Include(c => c.Reviews)
                    .ThenInclude(r => r.Restaurant)
                .OrderByDescending(c => c.Reviews.Count)
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

            TopUsers = critics
                .Select(c => (
                    critic: c,
                    reviewCount: c.Reviews.Count,
                    recent: c.Reviews
                                     .OrderByDescending(r => r.CreatedAt)
                                     .Take(3)
                ))
                .ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            var critic = await _db.Critics
                                  .Include(c => c.Reviews)
                                  .FirstOrDefaultAsync(c => c.UserId == userId);
            if (critic != null)
            {
                // remove their reviews
                _db.Reviews.RemoveRange(critic.Reviews);
                // remove critic profile
                _db.Critics.Remove(critic);
                // optionally remove IdentityUser
                var user = await _db.Users.FindAsync(userId);
                if (user != null) _db.Users.Remove(user);

                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostFlag(string userId, string message)
        {
            // placeholder – you could log or send an email here
            return RedirectToPage();
        }
    }
}
