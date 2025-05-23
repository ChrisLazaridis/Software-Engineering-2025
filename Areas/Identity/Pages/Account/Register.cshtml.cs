using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;
using SoftEng2025.Models;

namespace SoftEng2025.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly ApplicationDbContext _db;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _db = db;
            Input = new InputModel();
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required, EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [Display(Name = "Account Type")]
            public string AccountType { get; set; }

            public List<SelectListItem> AccountTypes { get; } = new()
            {
                new SelectListItem("Critic",      "Critic"),
                new SelectListItem("Entrepreneur","Entrepreneur"),
                new SelectListItem("Admin",       "Admin")
            };

            [Required, StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }

            // ─── Critic-only weights ────────────────────────────────────
            [Range(1, 5)] public int Food { get; set; } = 5;
            [Range(1, 5)] public int Service { get; set; } = 4;
            [Range(1, 5)] public int Price { get; set; } = 3;
            [Range(1, 5)] public int Condition { get; set; } = 2;
            [Range(1, 5)] public int Atmosphere { get; set; } = 1;
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        // AJAX endpoint to verify username uniqueness
        public async Task<JsonResult> OnGetCheckUsernameAsync(string username)
        {
            bool taken =
                await _db.Critics.AnyAsync(c => c.Username == username)
                || await _db.Entrepreneurs.AnyAsync(e => e.Username == username)
                || await _db.Admins.AnyAsync(a => a.Username == username)
                || await _userManager.Users.AnyAsync(u => u.UserName == username);

            return new JsonResult(new { available = !taken });
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (!ModelState.IsValid)
                return Page();

            var user = new IdentityUser
            {
                UserName = Input.Username,
                Email = Input.Email
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("New user created.");
                await _userManager.AddToRoleAsync(user, Input.AccountType);

                switch (Input.AccountType)
                {
                    case "Critic":
                        _db.Critics.Add(new Critic
                        {
                            UserId = user.Id,
                            Username = Input.Username,
                            FirstName = "", // can prompt these later
                            LastName = "",
                            Email = Input.Email,
                            Food = Input.Food,
                            Service = Input.Service,
                            Price = Input.Price,
                            Condition = Input.Condition,
                            Atmosphere = Input.Atmosphere
                        });
                        break;
                    case "Entrepreneur":
                        _db.Entrepreneurs.Add(new Entrepreneur
                        {
                            UserId = user.Id,
                            Username = Input.Username,
                            FirstName = "",
                            LastName = "",
                            Email = Input.Email
                        });
                        break;
                    case "Admin":
                        _db.Admins.Add(new Admin
                        {
                            UserId = user.Id,
                            Username = Input.Username,
                            FirstName = "",
                            LastName = "",
                            Email = Input.Email
                        });
                        break;
                }

                await _db.SaveChangesAsync();
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);

            return Page();
        }
    }
}
