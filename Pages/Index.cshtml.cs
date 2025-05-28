using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;
using SoftEng2025.Services;

namespace SoftEng2025.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IGeocodingService _geocoder;
        private const int PageSize = 10;

        public IndexModel(ApplicationDbContext db, IGeocodingService geocoder)
        {
            _db = db;
            _geocoder = geocoder;
        }

        public class RestaurantCard
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string OwnerName { get; set; }
            public string Address { get; set; }
            public string Type { get; set; }
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
            public string ImageBase64 { get; set; }
        }

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool OnlyFavorites { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public List<RestaurantCard> Cards { get; set; }
        public int TotalPages { get; set; }
        public List<string> AllTypes { get; set; }

        public bool IsCritic { get; set; }
        public List<int> FavoriteRestaurantIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(uid))
            {
                if (await _db.Entrepreneurs.AnyAsync(e => e.UserId == uid))
                    return RedirectToPage("/Entrepreneur/Index");
                if (await _db.Admins.AnyAsync(a => a.UserId == uid))
                    return RedirectToPage("/Admin/Index");

                // load critic favorites early for filtering & ordering
                var critic = await _db.Critics
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == uid);
                if (critic != null)
                {
                    IsCritic = true;
                    FavoriteRestaurantIds = await _db.FavoriteRestaurants
                        .Where(fr => fr.CriticId == critic.CriticId)
                        .Select(fr => fr.RestaurantId)
                        .ToListAsync();
                }
            }

            AllTypes = await _db.Restaurants
                               .Select(r => r.RestaurantType)
                               .Distinct()
                               .OrderBy(t => t)
                               .ToListAsync();

            var qry = _db.Restaurants
                         .Include(r => r.Entrepreneur)
                         .Include(r => r.Images)
                         .AsNoTracking()
                         .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
                qry = qry.Where(r => r.Name.Contains(Search));

            if (!string.IsNullOrWhiteSpace(FilterType))
                qry = qry.Where(r => r.RestaurantType == FilterType);

            if (OnlyFavorites && IsCritic)
                qry = qry.Where(r => FavoriteRestaurantIds.Contains(r.RestaurantId));

            // favorites-first ordering for critics
            if (IsCritic)
                qry = qry
                    .OrderByDescending(r => FavoriteRestaurantIds.Contains(r.RestaurantId))
                    .ThenByDescending(r => r.AverageRating);
            else
                qry = qry.OrderByDescending(r => r.AverageRating);

            var total = await qry.CountAsync();
            TotalPages = (int)Math.Ceiling(total / (double)PageSize);
            CurrentPage = Math.Clamp(CurrentPage, 1, Math.Max(TotalPages, 1));

            var pageData = await qry
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var coords = pageData.Select(r => (Lat: r.Location.Y, Lon: r.Location.X)).ToList();
            var addresses = await _geocoder.GetAddressesAsync(coords);

            Cards = pageData.Select((r, i) => new RestaurantCard
            {
                Id = r.RestaurantId,
                Name = r.Name,
                OwnerName = $"{r.Entrepreneur.FirstName} {r.Entrepreneur.LastName}",
                Address = addresses[i],
                Type = r.RestaurantType,
                AverageRating = Math.Round(Math.Min(r.AverageRating, 5.0), 2),
                ReviewCount = r.ReviewCount,
                ImageBase64 = r.Images.FirstOrDefault() != null
                    ? $"data:image/jpeg;base64,{Convert.ToBase64String(r.Images.First().ImageData)}"
                    : null
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(int restaurantId)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid)) return Forbid();

            var critic = await _db.Critics.SingleOrDefaultAsync(c => c.UserId == uid);
            if (critic == null) return Forbid();

            var existing = await _db.FavoriteRestaurants.SingleOrDefaultAsync(fr =>
                fr.CriticId == critic.CriticId && fr.RestaurantId == restaurantId);

            if (existing != null)
                _db.FavoriteRestaurants.Remove(existing);
            else
                _db.FavoriteRestaurants.Add(new FavoriteRestaurant
                {
                    CriticId = critic.CriticId,
                    RestaurantId = restaurantId
                });

            await _db.SaveChangesAsync();

            return RedirectToPage(new
            {
                search = Search,
                filterType = FilterType,
                onlyFavorites = OnlyFavorites,
                currentPage = CurrentPage
            });
        }
    }
}
