// Areas/Identity/Pages/Account/Register.cshtml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using SoftEng2025.Data;
using SoftEng2025.Models;

namespace SoftEng2025.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
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

            // Ensure Input is never null in the view
            Input = new InputModel();
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
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

            [Required]
            [Display(Name = "First name")]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = "Last name")]
            public string LastName { get; set; }

            [Required, StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            // Re-initialize Input so AccountTypes is set
            Input = new InputModel();

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager
                                   .GetExternalAuthenticationSchemesAsync())
                            .ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager
                                   .GetExternalAuthenticationSchemesAsync())
                            .ToList();

            if (!ModelState.IsValid)
                return Page();

            var user = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("New user created.");

                // 1) Add to selected Identity role
                await _userManager.AddToRoleAsync(user, Input.AccountType);

                // 2) Create the profile row in your domain tables
                switch (Input.AccountType)
                {
                    case "Critic":
                        _db.Critics.Add(new Critic
                        {
                            UserId = user.Id,
                            Username = user.UserName,
                            FirstName = Input.FirstName,
                            LastName = Input.LastName,
                            Email = Input.Email
                        });
                        break;

                    case "Entrepreneur":
                        _db.Entrepreneurs.Add(new Entrepreneur
                        {
                            UserId = user.Id,
                            Username = user.UserName,
                            FirstName = Input.FirstName,
                            LastName = Input.LastName,
                            Email = Input.Email
                        });
                        break;

                    case "Admin":
                        _db.Admins.Add(new Admin
                        {
                            UserId = user.Id,
                            Username = user.UserName,
                            FirstName = Input.FirstName,
                            LastName = Input.LastName,
                            Email = Input.Email
                        });
                        break;
                }

                await _db.SaveChangesAsync();

                // 3) Sign in
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            // If creation failed, show errors
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }
    }
}
