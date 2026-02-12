using BookwormsOnline.Models;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Web;

namespace BookwormsOnline.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            IEncryptionService encryptionService,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public ApplicationUser? AppUser { get; set; }
        public string DecryptedCreditCard { get; set; } = string.Empty;
        public string DecodedBillingAddress { get; set; } = string.Empty;
        public string DecodedShippingAddress { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            AppUser = await _userManager.GetUserAsync(User);

            if (AppUser == null)
            {
                return RedirectToPage("/Login");
            }

            DecryptedCreditCard = _encryptionService.Decrypt(AppUser.CreditCardNumber);
            // HTML-decode addresses for safe display (they are stored as HTML-encoded in database)
            DecodedBillingAddress = HttpUtility.HtmlDecode(AppUser.BillingAddress);
            DecodedShippingAddress = HttpUtility.HtmlDecode(AppUser.ShippingAddress);

            return Page();
        }
    }
}
