using BookwormsOnline.Data;
using BookwormsOnline.Models;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BookwormsOnline.Pages;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public ResetPasswordModel(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _context = context;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? code = null, string? email = null)
    {
        if (code == null)
        {
            return BadRequest("A code must be supplied for password reset.");
        }
        else
        {
            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            Input = new InputModel
            {
                Code = decodedCode,
                Email = email ?? string.Empty
            };
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        // Check against password history
        var history = await _context.PasswordHistories
            .Where(ph => ph.UserId == user.Id)
            .OrderByDescending(ph => ph.CreatedAt)
            .ToListAsync();

        var passwordHasher = _userManager.PasswordHasher;
        
        // Also check current password
        var isCurrentPassword = passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, Input.Password);
        if (isCurrentPassword == PasswordVerificationResult.Success)
        {
            ModelState.AddModelError(string.Empty, "Your new password cannot be the same as your current password.");
            return Page();
        }

        foreach (var pastPassword in history)
        {
            var result = passwordHasher.VerifyHashedPassword(user, pastPassword.PasswordHash, Input.Password);
            if (result == PasswordVerificationResult.Success)
            {
                ModelState.AddModelError(string.Empty, "You cannot reuse any of your last 2 passwords.");
                return Page();
            }
        }

        var oldHash = user.PasswordHash;
        var resultReset = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
        if (resultReset.Succeeded)
        {
            // Update last password change timestamp
            user.LastPasswordChangedDate = DateTime.UtcNow;
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
            await _auditLogService.LogAsync(user.Id, user.Email!, "Password Reset", ipAddress);

            return RedirectToPage("./ResetPasswordConfirmation");
        }

        foreach (var error in resultReset.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return Page();
    }
}
