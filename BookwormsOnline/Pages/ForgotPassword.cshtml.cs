using BookwormsOnline.Models;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BookwormsOnline.Pages;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordResetEmailSender _emailSender;

    public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IPasswordResetEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var codeEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            
            // Simple URL with email and token
            var callbackUrl = Url.Page(
                "/ResetPassword",
                pageHandler: null,
                values: new { email = Input.Email, code = codeEncoded },
                protocol: Request.Scheme);

            await _emailSender.SendResetLinkAsync(Input.Email, callbackUrl!);

            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        return Page();
    }
}


