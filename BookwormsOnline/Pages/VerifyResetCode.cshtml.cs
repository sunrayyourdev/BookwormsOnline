using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace BookwormsOnline.Pages;

[AllowAnonymous]
public class VerifyResetCodeModel : PageModel
{
    public string? Code { get; set; }

    public IActionResult OnGet(string? code = null)
    {
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest("A code must be supplied for password reset.");
        }

        // Store the code to be POSTed to the reset page
        // This prevents the code from appearing in server access logs
        Code = code;
        
        return Page();
    }
}

