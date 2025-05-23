using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;

namespace SoftEng2025.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private const int PageSize = 10;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public class RestaurantCard
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string OwnerName { get; set; }
            public string Location { get; set; }
            public string Type { get; set; }
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
            public string ImageBase64 { get; set; }
        }

        // Input
        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        // Output
        public List<RestaurantCard> Cards { get; set; }
        public int TotalPages { get; set; }
        public List<string> AllTypes { get; set; }

        public async Task OnGetAsync()
        {
            // 1) Gather all distinct types for filter dropdown
            AllTypes = await _db.Restaurants
                               .Select(r => r.RestaurantType)
                               .Distinct()
                               .OrderBy(t => t)
                               .ToListAsync();

            // 2) Base query: sorted by AvgRating desc
            var query = _db.Restaurants
                .Include(r => r.Entrepreneur)
                .Include(r => r.Images)
                .AsNoTracking()
                .OrderByDescending(r => r.AverageRating)
                .AsQueryable();

            // 3) Apply search
            if (!string.IsNullOrWhiteSpace(Search))
                query = query.Where(r => r.Name.Contains(Search));

            // 4) Apply type filter
            if (!string.IsNullOrWhiteSpace(FilterType))
                query = query.Where(r => r.RestaurantType == FilterType);

            // 5) Pagination
            var totalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            var pageData = await query
                .Skip((Page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // 6) Project to card view model (clamp + round rating + include review count)
            Cards = pageData.Select(r =>
            {
                // clamp at 5.0 then round to one decimal place
                var normalized = Math.Min(r.AverageRating, 5.0);
                var rounded = Math.Round(normalized, 1);

                return new RestaurantCard
                {
                    Id = r.RestaurantId,
                    Name = r.Name,
                    OwnerName = $"{r.Entrepreneur.FirstName} {r.Entrepreneur.LastName}",
                    Location = $"{r.Location.Y:F4}, {r.Location.X:F4}",
                    Type = r.RestaurantType,
                    AverageRating = rounded,
                    ReviewCount = r.ReviewCount,
                    ImageBase64 = r.Images.FirstOrDefault() != null
                        ? $"data:image/jpeg;base64,{Convert.ToBase64String(r.Images.First().ImageData)}"
                        : null
                };
            }).ToList();
        }
    }
}
