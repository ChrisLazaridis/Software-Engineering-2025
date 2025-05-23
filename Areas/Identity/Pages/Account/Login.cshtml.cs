using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SoftEng2025.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username or Email")]
            public string Username { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (!ModelState.IsValid)
                return Page();

            // lookup...
            var user = await _userManager.FindByNameAsync(Input.Username)
                       ?? await _userManager.FindByEmailAsync(Input.Username);

            if (user == null)
            {
                _logger.LogWarning("No user found for {Identifier}", Input.Username);
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            _logger.LogInformation("Stored PasswordHash: {Hash}", user.PasswordHash);
            // direct verify
            var verify = _userManager.PasswordHasher
                .VerifyHashedPassword(user, user.PasswordHash, Input.Password);
            _logger.LogInformation("VerifyHashedPassword returned {VerifyResult}", verify);

            var result = await _signInManager.CheckPasswordSignInAsync(
                user, Input.Password, lockoutOnFailure: false);

            _logger.LogInformation(
                "PasswordSignInAsync: Succeeded={Succeeded}, LockedOut={LockedOut}, NotAllowed={NotAllowed}, Requires2FA={2FA}",
                result.Succeeded, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, Input.RememberMe);
                _logger.LogInformation("User {UserId} logged in", user.Id);
                return LocalRedirect(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}
