using BookwormsOnline.Models;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

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

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEncryptionService encryptionService,
            IWebHostEnvironment environment,
            IAuditLogService auditLogService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _encryptionService = encryptionService;
            _environment = environment;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Last Name")]
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
            public string BillingAddress { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Shipping Address")]
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
                if (Input.Photo != null)
                {
                    var extension = Path.GetExtension(Input.Photo.FileName).ToLower();
                    if (extension != ".jpg")
                    {
                        ModelState.AddModelError("Input.Photo", "Only .JPG files are allowed.");
                        return Page();
                    }

                    if (Input.Photo.ContentType.ToLower() != "image/jpeg")
                    {
                        ModelState.AddModelError("Input.Photo", "Invalid file type. Only JPG images are allowed.");
                        return Page();
                    }
                }

                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    CreditCardNumber = _encryptionService.Encrypt(Input.CreditCardNumber),
                    Mobile = Input.Mobile,
                    BillingAddress = Input.BillingAddress,
                    ShippingAddress = Input.ShippingAddress
                };

                if (Input.Photo != null)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.Photo.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await Input.Photo.CopyToAsync(fileStream);
                    }
                    user.PhotoPath = "/uploads/" + uniqueFileName;
                }

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
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }
    }
}
