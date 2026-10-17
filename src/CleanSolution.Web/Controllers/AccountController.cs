using CleanSolution.Infrastructure.Identity;
using CleanSolution.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CleanSolution.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly ILogger<AccountController> _logger;

        private const string GenericErrorMessage = "Something went wrong. Please try again later.";

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            _logger = logger;
        }

        // Register
        [HttpGet]
        public async Task<IActionResult> RegisterAsync(string? returnUrl = null)
        {
            var model = new RegisterModel { ReturnUrl = returnUrl };
            model.ExternalLogins = await GetExternalLoginsAsync();
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAsync(RegisterModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");
            model.ExternalLogins = await GetExternalLoginsAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = CreateUser();
                var ct = HttpContext.RequestAborted;

                await _userStore.SetUserNameAsync(user, model.Email, ct);
                await GetEmailStore().SetEmailAsync(user, model.Email, ct);
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToLocal(model.ReturnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Register request was cancelled by the client.");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during registration.");
                ModelState.AddModelError(string.Empty, GenericErrorMessage);
            }

            return View(model);
        }

        // Login
        [HttpGet]
        public async Task<IActionResult> LoginAsync(string? returnUrl = null)
        {
            var model = new LoginModel { ReturnUrl = returnUrl ?? Url.Content("~/") };

            try
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sign out external scheme on login page load.");
            }

            model.ExternalLogins = await GetExternalLoginsAsync();
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAsync(LoginModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");
            model.ExternalLogins = await GetExternalLoginsAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(model.ReturnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction("LoginWith2fa", new { returnUrl = model.ReturnUrl, rememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction("Lockout");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Login request was cancelled by the client.");
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during login.");
                ModelState.AddModelError(string.Empty, GenericErrorMessage);
            }

            return View(model);
        }

        // Logout
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutAsync(string? returnUrl = null)
        {
            try
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logged out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during logout.");
            }

            return RedirectToLocal(returnUrl);
        }

        // Helpers
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        private async Task<List<AuthenticationScheme>> GetExternalLoginsAsync()
        {
            try
            {
                return (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load external authentication schemes.");
                return new List<AuthenticationScheme>();
            }
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.",
                    ex);
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}