using CleanSolution.Domain.Utilities;
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
        private readonly IEmailUtility _emailUtility;

        private const string GenericErrorMessage = "Something went wrong. Please try again later.";

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            IEmailUtility emailUtility)
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            _logger = logger;
            _emailUtility = emailUtility;
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

                    await SendVerificationCodeAsync(user);
                    return RedirectToAction("VerifyEmail", new { email = model.Email, returnUrl = model.ReturnUrl });
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
        public async Task<IActionResult> LoginAsync(string? returnUrl = null, string? error = null)
        {
            if (error == "external")
                TempData["Error"] = "External login failed. Please try again.";

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

                if (result.IsNotAllowed)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null
                        && !await _userManager.IsEmailConfirmedAsync(user)
                        && await _userManager.CheckPasswordAsync(user, model.Password))
                    {
                        await SendVerificationCodeAsync(user);
                        return RedirectToAction("VerifyEmail", new { email = model.Email, returnUrl = model.ReturnUrl });
                    }
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
                return RedirectToAction("Login", new { returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login", new { returnUrl });
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
                return RedirectToAction("Login", new { returnUrl });
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
                        return RedirectToAction("Login", new { returnUrl });
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

            return RedirectToAction("Login", new { returnUrl });
        }

        [HttpGet]
        public IActionResult VerifyEmail(string email, string? returnUrl = null)
        {
            return View(new VerifyEmailModel { Email = email, ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(nameof(model.Code), "Invalid or expired code.");
                return View(model);
            }

            // আগেই ভেরিফাই করা থাকলে কোড চেক ছাড়া সাইন-ইন করা যাবে না, লগইন পেজে পাঠাও
            if (user.EmailConfirmed)
                return RedirectToAction("Login", new { returnUrl = model.ReturnUrl });

            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError(nameof(model.Code), "Too many attempts. Please try again later.");
                return View(model);
            }

            var valid = await _userManager.VerifyUserTokenAsync(
                user, TokenOptions.DefaultEmailProvider, VerifyPurpose, model.Code);

            if (!valid)
            {
                await _userManager.AccessFailedAsync(user);
                ModelState.AddModelError(nameof(model.Code), "Invalid or expired code.");
                return View(model);
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            await _userManager.ResetAccessFailedCountAsync(user);

            // SecurityStamp আপডেট করলে পুরোনো কোড বাতিল হয়
            await _userManager.UpdateSecurityStampAsync(user);

            // সরাসরি লগইন (stamp আপডেটের পরে করতে হবে, নইলে কুকি ইনভ্যালিড হয়ে যাবে)
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User verified email and signed in.");

            return RedirectToLocal(model.ReturnUrl);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationCode(string email, string? returnUrl = null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && !user.EmailConfirmed)
            {
                await SendVerificationCodeAsync(user);
            }

            // ইউজার আছে কি না সেটা বোঝা যাবে না, তাই সবসময় একই বার্তা
            TempData["Info"] = "If the account exists, a new code has been sent.";
            return RedirectToAction("VerifyEmail", new { email, returnUrl });
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

        private const string VerifyPurpose = "EmailVerification";
        private async Task<bool> SendVerificationCodeAsync(ApplicationUser user)
        {
            try
            {
                var code = await _userManager.GenerateUserTokenAsync(
                    user, TokenOptions.DefaultEmailProvider, VerifyPurpose);

                var body = $@"
            <div style='font-family:Arial,sans-serif;max-width:480px;margin:auto'>
                <h2>Verify your email</h2>
                <p>Your Kaarigor verification code is:</p>
                <p style='font-size:32px;letter-spacing:8px;font-weight:bold'>{code}</p>
                <p style='color:#777'>This code expires in a few minutes. If you didn't request it, ignore this email.</p>
            </div>";

                await _emailUtility.SendEmailAsync(user.Email!, user.Email!, "Your verification code", body);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email.");
                return false;
            }
        }
    }
}