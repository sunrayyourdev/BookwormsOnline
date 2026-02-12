using BookwormsOnline.Models;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace BookwormsOnline.Pages
{
    [ValidateAntiForgeryToken]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEncryptionService _encryptionService;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuditLogService _auditLogService;
        private readonly IPhotoUploadService _photoUploadService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEncryptionService encryptionService,
            IWebHostEnvironment environment,
            IAuditLogService auditLogService,
            IPhotoUploadService photoUploadService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _encryptionService = encryptionService;
            _environment = environment;
            _auditLogService = auditLogService;
            _photoUploadService = photoUploadService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        // Properties for HTML-encoded address display
        public string EncodedBillingAddress { get; set; } = string.Empty;
        public string EncodedShippingAddress { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            [Display(Name = "First Name")]
            [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First Name can only contain letters, spaces, hyphens, and apostrophes.")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Last Name")]
            [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last Name can only contain letters, spaces, hyphens, and apostrophes.")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Credit Card Number")]
            [RegularExpression(@"^\d{16}$", ErrorMessage = "Credit Card Number must be 16 digits.")]
            public string CreditCardNumber { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Mobile Number")]
            [RegularExpression(@"^[89]\d{7}$", ErrorMessage = "Invalid Mobile Number.")]
            public string Mobile { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Billing Address")]
            [StandardAddress]
            [StringLength(500, ErrorMessage = "Billing Address cannot exceed 500 characters.")]
            public string BillingAddress { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Shipping Address")]
            [StringLength(500, ErrorMessage = "Shipping Address cannot exceed 500 characters.")]
            public string ShippingAddress { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Photo (.JPG only)")]
            public IFormFile? Photo { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                // Validate and save photo using PhotoUploadService
                string? photoPath = null;
                if (Input.Photo != null)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    var uploadResult = await _photoUploadService.ValidateAndSavePhotoAsync(Input.Photo, uploadsFolder);
                    
                    if (!uploadResult.IsSuccess)
                    {
                        ModelState.AddModelError("Input.Photo", uploadResult.Message);
                        return Page();
                    }
                    
                    photoPath = uploadResult.FilePath;
                }

                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    CreditCardNumber = _encryptionService.Encrypt(Input.CreditCardNumber),
                    Mobile = Input.Mobile,
                    // HTML encode addresses to prevent XSS attacks
                    BillingAddress = HttpUtility.HtmlEncode(Input.BillingAddress),
                    ShippingAddress = HttpUtility.HtmlEncode(Input.ShippingAddress),
                    PhotoPath = photoPath ?? string.Empty,
                    LastPasswordChangedDate = DateTime.UtcNow
                };


                var result = await _userManager.CreateAsync(user, Input.Password);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (result.Succeeded)
                {
                    await _auditLogService.LogAsync(user.Id, Input.Email, "Account Created", ipAddress);
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
                
                await _auditLogService.LogAsync(null, Input.Email, "Registration Failed", ipAddress);
                foreach (var error in result.Errors)
                {
                    // Customize error messages for better user experience
                    string customErrorMessage = error.Description;
                    
                    // Replace generic "Username" error with email-specific message
                    if (error.Code == "DuplicateUserName" || error.Description.Contains("already taken"))
                    {
                        customErrorMessage = $"The email address '{Input.Email}' is already registered. Please use a different email or try logging in.";
                    }
                    else if (error.Code == "DuplicateEmail")
                    {
                        customErrorMessage = $"The email address '{Input.Email}' is already registered. Please use a different email or try logging in.";
                    }
                    else if (error.Code == "InvalidEmail")
                    {
                        customErrorMessage = "The email address provided is invalid.";
                    }
                    else if (error.Code == "PasswordTooShort")
                    {
                        customErrorMessage = "Password must be at least 12 characters long.";
                    }
                    else if (error.Code == "PasswordRequiresNonAlphanumeric")
                    {
                        customErrorMessage = "Password must contain at least one special character.";
                    }
                    
                    ModelState.AddModelError(string.Empty, customErrorMessage);
                }
            }

            return Page();
        }
    }
}
