using System.ComponentModel.DataAnnotations;

namespace CleanSolution.Web.Models
{
    public class VerifyEmailModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the 6-digit code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must contain digits only.")]
        public string Code { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
