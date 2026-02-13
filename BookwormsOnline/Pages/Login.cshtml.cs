using BookwormsOnline.Models;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BookwormsOnline.Pages;

[ValidateAntiForgeryToken]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;
    private readonly ReCaptchaService _reCaptchaService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLogService,
        ReCaptchaService reCaptchaService,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _reCaptchaService = reCaptchaService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string ErrorMessage { get; set; } = string.Empty;

    public string? ReCaptchaSiteKey { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? Token { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await _signInManager.SignOutAsync();

        ReturnUrl = returnUrl;
        ReCaptchaSiteKey = Environment.GetEnvironmentVariable("RECAPTCHA_SITEKEY");
        
        // Log for debugging
        if (string.IsNullOrEmpty(ReCaptchaSiteKey))
        {
            _logger.LogWarning("RECAPTCHA_SITEKEY environment variable not found!");
        }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        // Ensure ReCaptchaSiteKey is set for the view in case we return early
        ReCaptchaSiteKey = Environment.GetEnvironmentVariable("RECAPTCHA_SITEKEY");

        if (ModelState.IsValid)
        {
            // Validate reCAPTCHA FIRST - before any database operations
            if (string.IsNullOrEmpty(Input.Token))
            {
                _logger.LogWarning("reCAPTCHA token is empty on login attempt");
                ModelState.AddModelError(string.Empty, "Captcha verification failed. Please try again.");
                return Page();
            }

            _logger.LogInformation($"Verifying reCAPTCHA token: {Input.Token.Substring(0, Math.Min(10, Input.Token.Length))}...");
            
            if (!await _reCaptchaService.VerifyAsync(Input.Token))
            {
                _logger.LogWarning("reCAPTCHA verification failed for login attempt");
                ModelState.AddModelError(string.Empty, "Captcha verification failed. Please try again.");
                return Page();
            }

            _logger.LogInformation("reCAPTCHA verification successful, proceeding with login");

            var user = await _userManager.FindByEmailAsync(Input.Email);
            // ...existing code...
            if (user != null)
            {
                // Concurrent Session Control: Invalidate other sessions by updating the security stamp BEFORE sign-in
                // This ensures that when the user signs in, they get a fresh cookie with the new stamp.
                await _userManager.UpdateSecurityStampAsync(user);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: true);
            
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (result.Succeeded)
            {
                await _auditLogService.LogAsync(user?.Id, Input.Email, "Login Success", ipAddress);

                if (user != null)
                {
                    // Enforce maximum password age (90 days) on login
                    var age = DateTime.UtcNow - user.LastPasswordChangedDate;
                    if (age > TimeSpan.FromDays(90))
                    {
                        return RedirectToPage("./ChangePassword", new { expired = 1 });
                    }
                }

                return LocalRedirect(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = false });
            }
            if (result.IsLockedOut)
            {
                await _auditLogService.LogAsync(user?.Id, Input.Email, "Account Locked Out", ipAddress);
                return RedirectToPage("./Lockout");
            }
            else
            {
                await _auditLogService.LogAsync(user?.Id, Input.Email, "Login Failed", ipAddress);
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }
}
