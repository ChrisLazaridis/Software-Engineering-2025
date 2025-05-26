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
            var entity = await _db.Restaurants.FindAsync(id);
            if (entity == null) return NotFound();

            _db.Restaurants.Remove(entity);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public IActionResult OnPostFlag(int id)
        {
            // placeholder – you can log or e-mail a warning here
            return RedirectToPage();
        }
    }
}