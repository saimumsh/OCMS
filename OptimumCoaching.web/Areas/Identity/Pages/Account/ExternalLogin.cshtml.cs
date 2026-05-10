using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OptimumCoaching.core;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace OptimumCoaching.web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ProviderDisplayName { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        // Step 1 — kick off the external challenge (called from Login / Register).
        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        // Step 2 — handle the provider callback. Either signs in (existing link)
        // or shows the confirmation form (new user).
        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Existing external login → sign in directly.
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("{Email} signed in with {Provider}.",
                    info.Principal.FindFirstValue(ClaimTypes.Email), info.LoginProvider);
                return LocalRedirectOrDashboard(returnUrl);
            }

            if (result.IsLockedOut)
            {
                ErrorMessage = "Account locked. Try again later.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // First-time external sign-in for this provider key. If a user with
            // the same email already exists, link the external login to that
            // account (no confirmation needed, the email proves identity).
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(email))
            {
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null)
                {
                    var addResult = await _userManager.AddLoginAsync(existing, info);
                    if (addResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(existing, isPersistent: false);
                        _logger.LogInformation("Linked {Provider} to existing account {Email}.",
                            info.LoginProvider, email);
                        return LocalRedirectOrDashboard(returnUrl);
                    }
                    ErrorMessage = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }
            }

            // Brand new user — show the confirmation page pre-filled with the
            // info Google provided so they can review/edit before account creation.
            ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
            ReturnUrl = returnUrl;
            Input = new InputModel
            {
                Email = email ?? string.Empty,
                FullName = info.Principal.FindFirstValue(ClaimTypes.Name)
                           ?? info.Principal.FindFirstValue(ClaimTypes.GivenName)
                           ?? string.Empty,
            };
            return Page();
        }

        // Step 3 — user confirmed the form; create the account and link the login.
        public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return Page();

            // Refuse to silently overwrite an existing account.
            if (await _userManager.FindByEmailAsync(Input.Email) != null)
            {
                ModelState.AddModelError(string.Empty, "An account with that email already exists. Please sign in with your password instead.");
                return Page();
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = Input.FullName,
                Email = Input.Email,
                UserName = Input.Email,
                EmailConfirmed = true, // verified by the OAuth provider
                Created = DateTime.UtcNow,
                Status = ApplicationUserStatus.Active,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

            const string defaultRole = "User";
            if (await _roleManager.RoleExistsAsync(defaultRole))
                await _userManager.AddToRoleAsync(user, defaultRole);

            var linkResult = await _userManager.AddLoginAsync(user, info);
            if (!linkResult.Succeeded)
            {
                foreach (var e in linkResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("Created account {Email} via {Provider}.", Input.Email, info.LoginProvider);
            return LocalRedirectOrDashboard(returnUrl);
        }

        private IActionResult LocalRedirectOrDashboard(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Dashboard", "Home", new { area = "" });
        }
    }
}
