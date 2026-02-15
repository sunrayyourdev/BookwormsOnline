using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BookwormsOnline.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [PersonalData]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [PersonalData]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [PersonalData]
    public string CreditCardNumber { get; set; } = string.Empty;

    [Required]
    [PersonalData]
    public string Mobile { get; set; } = string.Empty;

    [Required]
    [PersonalData]
    public string BillingAddress { get; set; } = string.Empty;

    [Required]
    [PersonalData]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    public string PhotoPath { get; set; } = string.Empty;

    [Required]
    public DateTime LastPasswordChangedDate { get; set; }
}
