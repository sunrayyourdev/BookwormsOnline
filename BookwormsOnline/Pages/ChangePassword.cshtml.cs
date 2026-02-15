using BookwormsOnline.Data;
using BookwormsOnline.Models;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace BookwormsOnline.Pages;

[Authorize]
[ValidateAntiForgeryToken]
public class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IConfiguration _configuration;

    public ChangePasswordModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _auditLogService = auditLogService;
        _configuration = configuration;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    [TempData]
    public string? ToastMessage { get; set; }

    [TempData]
    public string? ToastType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            // Do not expose user ID in error message - security risk
            return NotFound("Unable to load user information.");
        }

        // If redirected due to expiry, show a message
        if (Request.Query.ContainsKey("expired"))
        {
            StatusMessage = "Your password has expired. Please set a new password.";
            ReturnUrl = "/Index";
        }
        else if (string.IsNullOrWhiteSpace(ReturnUrl))
        {
            ReturnUrl = "/Index";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return IsAjaxRequest() 
                ? new JsonResult(new { success = false, errors = GetModelErrors() })
                : Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return IsAjaxRequest()
                ? new JsonResult(new { success = false, errors = new[] { "Unable to load user information." } })
                : NotFound("Unable to load user information.");
        }

        // If redirected due to expiry, show a message
        if (Request.Query.ContainsKey("expired"))
        {
            StatusMessage = "Your password has expired. Please set a new password.";
            ReturnUrl = "/Index";
        }

        // Check if the new password is the same as the current password
        var isCurrentPassword = await _userManager.CheckPasswordAsync(user, Input.NewPassword);
        if (isCurrentPassword)
        {
            ModelState.AddModelError(string.Empty, "Your new password cannot be the same as your current password.");
            return IsAjaxRequest()
                ? new JsonResult(new { success = false, errors = GetModelErrors() })
                : Page();
        }

        // Check against password history
        var history = await _context.PasswordHistories
            .Where(ph => ph.UserId == user.Id)
            .OrderByDescending(ph => ph.CreatedAt)
            .ToListAsync();

        var passwordHasher = _userManager.PasswordHasher;
        foreach (var pastPassword in history)
        {
            var result = passwordHasher.VerifyHashedPassword(user, pastPassword.PasswordHash, Input.NewPassword);
            if (result == PasswordVerificationResult.Success)
            {
                ModelState.AddModelError(string.Empty, "You cannot reuse any of your last 2 passwords.");
                return IsAjaxRequest()
                    ? new JsonResult(new { success = false, errors = GetModelErrors() })
                    : Page();
            }
        }

        // Enforce minimum password age from configuration
        var minPasswordAgeMinutes = _configuration.GetValue<int>("SecuritySettings:PasswordSettings:MinimumAgeMinutes", 2);
        var now = DateTime.UtcNow;
        var timeSinceLastChange = now - user.LastPasswordChangedDate;
        if (timeSinceLastChange < TimeSpan.FromMinutes(minPasswordAgeMinutes))
        {
            var remaining = TimeSpan.FromMinutes(minPasswordAgeMinutes) - timeSinceLastChange;
            var seconds = (int)remaining.TotalSeconds;
            var message = seconds >= 60 
                ? $"{seconds / 60} minute(s) and {seconds % 60} second(s)" 
                : $"{seconds} second(s)";
            ModelState.AddModelError(string.Empty, $"You changed your password too recently. Please wait {message} before trying again.");
            return IsAjaxRequest()
                ? new JsonResult(new { success = false, errors = GetModelErrors() })
                : Page();
        }

        var oldHash = user.PasswordHash;
        var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors)
            {
                // Sanitize error messages to prevent sensitive data exposure
                // Use user-friendly generic messages instead of exposing Identity framework details
                string sanitizedMessage = error.Code switch
                {
                    "PasswordMismatch" => "The current password is incorrect.",
                    "PasswordTooShort" => "Password must be at least 12 characters long.",
                    "PasswordRequiresNonAlphanumeric" => "Password must contain at least one special character.",
                    "PasswordRequiresDigit" => "Password must contain at least one digit.",
                    "PasswordRequiresLower" => "Password must contain at least one lowercase letter.",
                    "PasswordRequiresUpper" => "Password must contain at least one uppercase letter.",
                    _ => "Password change failed. Please ensure your password meets all requirements."
                };
                
                ModelState.AddModelError(string.Empty, sanitizedMessage);
            }

            return IsAjaxRequest()
                ? new JsonResult(new { success = false, errors = GetModelErrors() })
                : Page();
        }

        // Update last password change timestamp
        user.LastPasswordChangedDate = now;
        await _userManager.UpdateAsync(user);

        // Save old password to history
        if (oldHash != null)
        {
            _context.PasswordHistories.Add(new PasswordHistory
            {
                UserId = user.Id,
                PasswordHash = oldHash,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Keep only the last 2 history entries
            var userHistory = await _context.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.CreatedAt)
                .ToListAsync();

            if (userHistory.Count > 2)
            {
                _context.PasswordHistories.RemoveRange(userHistory.Skip(2));
                await _context.SaveChangesAsync();
            }
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditLogService.LogAsync(user.Id, user.Email!, "Password Changed", ipAddress);

        await _signInManager.RefreshSignInAsync(user);
        // StatusMessage is reserved for expiry info; success uses toast + redirect.

        if (IsAjaxRequest())
        {
            return new JsonResult(new { success = true, message = "Password changed successfully." });
        }

        TempData["ToastMessage"] = "Password changed successfully.";
        TempData["ToastType"] = "success";

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    private bool IsAjaxRequest()
    {
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }

    private string[] GetModelErrors()
    {
        return ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToArray();
    }
}
