using CleanSolution.Infrastructure.Identity;
using CleanSolution.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        //External Login
        // ---------- External Login ----------
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                _logger.LogWarning("External provider error: {Error}", remoteError);
                TempData["Error"] = "External login failed. Please try again.";
                return RedirectToAction(nameof(LoginAsync), new { returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(LoginAsync), new { returnUrl });
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                _logger.LogInformation("User logged in with {Provider}.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }

            if (signInResult.IsLockedOut)
            {
                return RedirectToAction("Lockout");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Your account did not provide an email address.";
                return RedirectToAction(nameof(LoginAsync), new { returnUrl });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    user = CreateUser();
                    await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
                    await GetEmailStore().SetEmailAsync(user, email, CancellationToken.None);
                    user.EmailConfirmed = true; 

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        TempData["Error"] = string.Join(" ", createResult.Errors.Select(e => e.Description));
                        return RedirectToAction(nameof(LoginAsync), new { returnUrl });
                    }
                }

                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                    _logger.LogInformation("User created/linked account using {Provider}.", info.LoginProvider);
                    return RedirectToLocal(returnUrl);
                }

                TempData["Error"] = GenericErrorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during external login.");
                TempData["Error"] = GenericErrorMessage;
            }

            return RedirectToAction(nameof(LoginAsync), new { returnUrl });
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