using System.ComponentModel.DataAnnotations;

namespace SecureLoginSystemNew.ViewModels
{
    public class EnableTwoFactorViewModel
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        public string Code { get; set; }

        public string SecretKey { get; set; }
        public string QrCodeImage { get; set; }
    }
}
