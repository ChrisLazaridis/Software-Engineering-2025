using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;

namespace SoftEng2025.Pages.Restaurants
{
    [Authorize(Roles = "Critic")]
    public class AddReviewModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public AddReviewModel(ApplicationDbContext db,
                              UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Bound to route and form
        [BindProperty(SupportsGet = true)]
        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; }

        // Critic info & weights
        public int CriticId { get; set; }
        public int WeightFood { get; set; }
        public int WeightService { get; set; }
        public int WeightPrice { get; set; }
        public int WeightCondition { get; set; }
        public int WeightAtmosphere { get; set; }
        private int SumWeights => WeightFood + WeightService + WeightPrice + WeightCondition + WeightAtmosphere;

        // Step-1 inputs
        [BindProperty]
        public decimal Food { get; set; }
        [BindProperty]
        public decimal Service { get; set; }
        [BindProperty]
        public decimal Price { get; set; }
        [BindProperty]
        public decimal Condition { get; set; }
        [BindProperty]
        public decimal Atmosphere { get; set; }

        // Step-2 inputs
        [BindProperty]
        public decimal Average { get; set; }
        [BindProperty]
        public string Comment { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Restaurant = await _db.Restaurants.FindAsync(RestaurantId);
            if (Restaurant == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var critic = await _db.Critics
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (critic == null) return Forbid();

            // prevent double-review
            var already = await _db.Reviews
              .AnyAsync(r => r.RestaurantId == RestaurantId
                          && r.CriticId == critic.CriticId);
            if (already)
                return RedirectToPage("Details", new { id = RestaurantId });

            CriticId = critic.CriticId;
            WeightFood = critic.Food;
            WeightService = critic.Service;
            WeightPrice = critic.Price;
            WeightCondition = critic.Condition;
            WeightAtmosphere = critic.Atmosphere;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1) Get the logged-in critic (with their weights)
            var userId = _userManager.GetUserId(User);
            var critic = await _db.Critics
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (critic is null)
                return Forbid();

            // 2) Extract weights & compute sum of exponents
            var wF = critic.Food;
            var wS = critic.Service;
            var wP = critic.Price;
            var wC = critic.Condition;
            var wA = critic.Atmosphere;
            var totalW = wF + wS + wP + wC + wA;  // should be 15 by default :contentReference[oaicite:1]{index=1}

            // 3) Recompute weighted sum & root
            var sum = Math.Pow((double)Food, wF)
                      * Math.Pow((double)Service, wS)
                      * Math.Pow((double)Price, wP)
                      * Math.Pow((double)Condition, wC)
                      * Math.Pow((double)Atmosphere, wA);

            var avgDouble = Math.Pow(sum, 1.0 / totalW);

            // 4) Safely convert to decimal & round
            //    (now avgDouble ∈ [1,5], so cast won’t overflow)
            var avgDec = (decimal)avgDouble;
            Average = decimal.Round(avgDec, 2);

            // 5) Persist as before
            var review = new Review
            {
                RestaurantId = RestaurantId,
                CriticId = critic.CriticId,
                Food = Food,
                Service = Service,
                Price = Price,
                Condition = Condition,
                Atmosphere = Atmosphere,
                Average = Average,
                Comment = Comment
            };
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            return RedirectToPage("Details", new { id = RestaurantId });
        }
    }
}
