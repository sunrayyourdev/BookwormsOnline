using BookwormsOnline.Models;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Web;
using System.Linq;

namespace BookwormsOnline.Pages
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEncryptionService _encryptionService;

        public ProfileModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEncryptionService encryptionService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _encryptionService = encryptionService;
        }

        public ApplicationUser? AppUser { get; set; }
        public string MaskedCreditCard { get; set; } = string.Empty;
        public string FullCreditCard { get; set; } = string.Empty;
        public string DecodedBillingAddress { get; set; } = string.Empty;
        public string DecodedShippingAddress { get; set; } = string.Empty;
        public bool IsTwoFactorEnabled { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            AppUser = await _userManager.GetUserAsync(User);

            if (AppUser == null)
            {
                // User is authenticated but data is not in database
                // Sign them out and redirect to login
                await _signInManager.SignOutAsync();
                return RedirectToPage("/Login");
            }

            var decryptedCard = _encryptionService.Decrypt(AppUser.CreditCardNumber);
            FullCreditCard = FormatCardNumber(NormalizeDigits(decryptedCard));
            MaskedCreditCard = MaskCardNumber(decryptedCard);

            // HTML-decode addresses for safe display
            DecodedBillingAddress = HttpUtility.HtmlDecode(AppUser.BillingAddress);
            DecodedShippingAddress = HttpUtility.HtmlDecode(AppUser.ShippingAddress);

            IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(AppUser);

            return Page();
        }

        private static string MaskCardNumber(string cardNumber)
        {
            var digits = NormalizeDigits(cardNumber);
            if (string.IsNullOrWhiteSpace(digits) || digits.Length < 4)
            {
                return "****";
            }

            var last4 = digits[^4..];
            return $"**** **** **** {last4}";
        }

        private static string NormalizeDigits(string input)
        {
            return new string(input.Where(char.IsDigit).ToArray());
        }

        private static string FormatCardNumber(string digits)
        {
            if (string.IsNullOrWhiteSpace(digits))
            {
                return "";
            }

            var groups = Enumerable.Range(0, (digits.Length + 3) / 4)
                .Select(i => digits.Substring(i * 4, Math.Min(4, digits.Length - i * 4)));

            return string.Join(" ", groups);
        }
    }
}
