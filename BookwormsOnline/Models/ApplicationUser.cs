using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BookwormsOnline.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(50)]
    [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First Name can only contain letters, spaces, hyphens, and apostrophes.")]
    [PersonalData]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last Name can only contain letters, spaces, hyphens, and apostrophes.")]
    [PersonalData]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(16, MinimumLength = 16)]
    [RegularExpression(@"^\d{16}$", ErrorMessage = "Credit Card Number must be exactly 16 digits.")]
    [PersonalData]
    public string CreditCardNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(8, MinimumLength = 8)]
    [RegularExpression(@"^[89]\d{7}$", ErrorMessage = "Mobile Number must be 8 digits starting with 8 or 9.")]
    [PersonalData]
    public string Mobile { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [PersonalData]
    public string BillingAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [PersonalData]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    public string PhotoPath { get; set; } = string.Empty;

    [Required]
    public DateTime LastPasswordChangedDate { get; set; }
}
