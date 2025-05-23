using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;

namespace SoftEng2025.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required, Display(Name = "Nickname")]
            public string Nickname { get; set; }

            public bool IsCritic { get; set; }

            // Critic-only weights
            public int Food { get; set; }
            public int Service { get; set; }
            public int Price { get; set; }
            public int Condition { get; set; }
            public int Atmosphere { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Unable to load user.");

            var roles = await _userManager.GetRolesAsync(user);
            Input = new InputModel
            {
                IsCritic = roles.Contains("Critic")
            };

            if (Input.IsCritic)
            {
                var critic = await _db.Critics.SingleAsync(c => c.UserId == user.Id);
                Input.Nickname = critic.Username;
                Input.Food = critic.Food;
                Input.Service = critic.Service;
                Input.Price = critic.Price;
                Input.Condition = critic.Condition;
                Input.Atmosphere = critic.Atmosphere;
            }
            else
            {
                Input.Nickname = "";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Unable to load user.");

            var roles = await _userManager.GetRolesAsync(user);
            Input.IsCritic = roles.Contains("Critic");

            if (!ModelState.IsValid)
                return Page();

            if (Input.IsCritic)
            {
                var critic = await _db.Critics.SingleAsync(c => c.UserId == user.Id);

                // Nickname uniqueness
                if (Input.Nickname != critic.Username)
                {
                    bool taken = await _db.Critics.AnyAsync(c => c.Username == Input.Nickname);
                    if (taken)
                    {
                        ModelState.AddModelError(nameof(Input.Nickname), "Nickname already taken.");
                        return Page();
                    }
                    critic.Username = Input.Nickname;
                }

                // Update weights
                critic.Food = Input.Food;
                critic.Service = Input.Service;
                critic.Price = Input.Price;
                critic.Condition = Input.Condition;
                critic.Atmosphere = Input.Atmosphere;

                await _db.SaveChangesAsync();
            }

            // Refresh sign-in (no other AspNetUsers properties changed)
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
