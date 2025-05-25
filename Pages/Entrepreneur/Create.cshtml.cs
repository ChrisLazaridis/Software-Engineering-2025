using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoftEng2025.Data;
using SoftEng2025.Models;

namespace SoftEng2025.Pages.Entrepreneur
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext db, ILogger<CreateModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        [BindProperty]
        public Restaurant Restaurant { get; set; }

        [BindProperty]
        public IFormFile ImageFile { get; set; }

        public void OnGet()
        {
            Restaurant = new Restaurant
            {
                Location = new NetTopologySuite.Geometries.Point(0, 0) { SRID = 4326 }
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1) Parse lat/lon
            if (!double.TryParse(Request.Form["Lat"], out var lat) ||
                !double.TryParse(Request.Form["Lon"], out var lon))
            {
                ModelState.AddModelError(string.Empty, "Invalid location selected.");
                return Page();
            }

            // 2) Lookup entrepreneur
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ent = await _db.Entrepreneurs
                               .FirstOrDefaultAsync(e => e.UserId == userId);
            if (ent == null) return Forbid();

            // 3) Assign the ID _and_ navigation
            Restaurant.EntrepreneurId = ent.EntrepreneurId;
            Restaurant.Entrepreneur = ent;
            Restaurant.Location = new NetTopologySuite.Geometries.Point(lon, lat) { SRID = 4326 };
            Restaurant.CreatedAt = DateTime.UtcNow;

            // 4) Strip out the one nested validation
            ModelState.Remove("Restaurant.Entrepreneur.User");

            // 5) Re-validate only Restaurant
            ModelState.Clear();
            if (!TryValidateModel(Restaurant, nameof(Restaurant)))
                return Page();

            // 6) Save
            _db.Restaurants.Add(Restaurant);
            await _db.SaveChangesAsync();

            // 7) Optional image
            if (ImageFile != null && ImageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await ImageFile.CopyToAsync(ms);
                _db.RestaurantImages.Add(new RestaurantImage
                {
                    RestaurantId = Restaurant.RestaurantId,
                    ImageData = ms.ToArray()
                });
                await _db.SaveChangesAsync();
            }

            return RedirectToPage("/Entrepreneur/Index");
        }
    }
}
