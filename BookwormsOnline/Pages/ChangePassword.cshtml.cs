using BookwormsOnline.Data;
using BookwormsOnline.Models;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

    public ChangePasswordModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

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
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        // If redirected due to expiry, show a message
        if (Request.Query.ContainsKey("expired"))
        {
            StatusMessage = "Your password has expired. Please set a new password.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        // Check if the new password is the same as the current password
        var isCurrentPassword = await _userManager.CheckPasswordAsync(user, Input.NewPassword);
        if (isCurrentPassword)
        {
            ModelState.AddModelError(string.Empty, "Your new password cannot be the same as your current password.");
            return Page();
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
                return Page();
            }
        }

        // Enforce minimum password age (1 day)
        var now = DateTime.UtcNow;
        var timeSinceLastChange = now - user.LastPasswordChangedDate;
        if (timeSinceLastChange < TimeSpan.FromDays(1))
        {
            var remaining = TimeSpan.FromDays(1) - timeSinceLastChange;
            var message = remaining.TotalHours >= 1 
                ? $"{(int)remaining.TotalHours} hours" 
                : $"{(int)remaining.TotalMinutes} minutes";
            ModelState.AddModelError(string.Empty, $"You changed your password too recently. Please wait {message} before trying again.");
            return Page();
        }

        var oldHash = user.PasswordHash;
        var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
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
        StatusMessage = "Your password has been changed.";

        return RedirectToPage();
    }
}
