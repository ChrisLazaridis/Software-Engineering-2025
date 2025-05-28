using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SoftEng2025.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ReviewsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private const int PageSize = 5;

        public ReviewsModel(ApplicationDbContext db) => _db = db;

        // Renamed from Page to CurrentPage
        [BindProperty(SupportsGet = true, Name = "CurrentPage")]
        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }
        public IList<Review> Reviews { get; set; }

        // Rename parameter and default
        public async Task OnGetAsync(int CurrentPage = 1)
        {
            this.CurrentPage = CurrentPage;

            var query = _db.Reviews
                .Include(r => r.Restaurant)
                .Include(r => r.Critic)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(total / (double)PageSize);

            Reviews = await query
                .Skip((this.CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var review = await _db.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();

            // Redirect back, preserving CurrentPage
            return RedirectToPage("/Admin/Reviews", new { CurrentPage = this.CurrentPage });
        }
    }
}