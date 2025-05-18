using System.ComponentModel.DataAnnotations;

namespace SecureLoginSystemNew.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Only letters and numbers are allowed")]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public required string ConfirmPassword { get; set; }
    }

}
