using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BookwormsOnline.Services;

/// <summary>
/// Custom validation attribute for standard address validation.
/// Allows alphanumeric characters, spaces, hyphens, commas, periods, and apostrophes.
/// Disallows special characters like @, #, $, %, &, etc.
/// </summary>
public class StandardAddressAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return true; // Let [Required] handle null/empty checks
        }

        var address = value.ToString()!;
        
        // Allow: letters, digits, spaces, hyphens, commas, periods, apostrophes, and parentheses
        // Pattern: alphanumeric, common address punctuation, and spaces
        var pattern = @"^[a-zA-Z0-9\s\-,'().]+$";
        
        if (!Regex.IsMatch(address, pattern))
        {
            ErrorMessage = "Address can only contain letters, numbers, spaces, hyphens, commas, periods, apostrophes, and parentheses.";
            return false;
        }

        return true;
    }
}

