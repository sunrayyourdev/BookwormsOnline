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
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptionService _encryptionService;

        public ProfileModel(
            UserManager<ApplicationUser> userManager,
            IEncryptionService encryptionService)
        {
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        public ApplicationUser? AppUser { get; set; }
        public string MaskedCreditCard { get; set; } = string.Empty;
        public string DecodedBillingAddress { get; set; } = string.Empty;
        public string DecodedShippingAddress { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            AppUser = await _userManager.GetUserAsync(User);

            if (AppUser == null)
            {
                return RedirectToPage("/Login");
            }

            var decryptedCard = _encryptionService.Decrypt(AppUser.CreditCardNumber);
            MaskedCreditCard = MaskCardNumber(decryptedCard);

            // HTML-decode addresses for safe display
            DecodedBillingAddress = HttpUtility.HtmlDecode(AppUser.BillingAddress);
            DecodedShippingAddress = HttpUtility.HtmlDecode(AppUser.ShippingAddress);

            return Page();
        }

        private static string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
            {
                return "****";
            }

            var last4 = cardNumber[^4..];
            return $"**** **** **** {last4}";
        }
    }
}

