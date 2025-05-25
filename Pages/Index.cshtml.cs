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
using SoftEng2025.Services;    // <-- geocoding service namespace

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
            public string Address { get; set; }    // now holds the human-readable address
            public string Type { get; set; }
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
            public string ImageBase64 { get; set; }
        }

        public string Search { get; set; }
        public string FilterType { get; set; }
        public int CurrentPage { get; set; }
        public List<RestaurantCard> Cards { get; set; }
        public int TotalPages { get; set; }
        public List<string> AllTypes { get; set; }

        public async Task<IActionResult> OnGetAsync(string search, string filterType, int currentPage = 1)
        {
            // 0) if Entrepreneur, redirect to their dashboard
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) &&
                await _db.Entrepreneurs.AnyAsync(e => e.UserId == userId))
            {
                return RedirectToPage("/Entrepreneur/Index");
            }

            // 1) assign incoming parameters
            Search = search;
            FilterType = filterType;
            CurrentPage = currentPage;

            // 2) build filter dropdown
            AllTypes = await _db.Restaurants
                               .Select(r => r.RestaurantType)
                               .Distinct()
                               .OrderBy(t => t)
                               .ToListAsync();

            // 3) base query
            var query = _db.Restaurants
                           .Include(r => r.Entrepreneur)
                           .Include(r => r.Images)
                           .AsNoTracking()
                           .OrderByDescending(r => r.AverageRating)
                           .AsQueryable();

            // 4) apply search
            if (!string.IsNullOrWhiteSpace(Search))
                query = query.Where(r => r.Name.Contains(Search));

            // 5) apply type filter
            if (!string.IsNullOrWhiteSpace(FilterType))
                query = query.Where(r => r.RestaurantType == FilterType);

            // 6) pagination calculation
            var totalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            CurrentPage = Math.Clamp(CurrentPage, 1, Math.Max(TotalPages, 1));

            // 7) fetch page of data
            var pageData = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // 8) prepare the list of (lat, lon) pairs
            var coords = pageData
                .Select(r => (Lat: r.Location.Y, Lon: r.Location.X))
                .ToList();

            // 9) lookup all addresses in parallel
            var addresses = await _geocoder.GetAddressesAsync(coords);

            // 10) map entities to view models, injecting the resolved address
            Cards = pageData
                .Select((r, i) =>
                {
                    var normalized = Math.Min(r.AverageRating, 5.0);
                    var rounded = Math.Round(normalized, 2);

                    return new RestaurantCard
                    {
                        Id = r.RestaurantId,
                        Name = r.Name,
                        OwnerName = $"{r.Entrepreneur.FirstName} {r.Entrepreneur.LastName}",
                        Address = addresses[i],
                        Type = r.RestaurantType,
                        AverageRating = rounded,
                        ReviewCount = r.ReviewCount,
                        ImageBase64 = r.Images.FirstOrDefault() != null
                            ? $"data:image/jpeg;base64,{Convert.ToBase64String(r.Images.First().ImageData)}"
                            : null
                    };
                })
                .ToList();

            // finally render the page
            return Page();
        }
    }
}
