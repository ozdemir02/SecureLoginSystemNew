using System.ComponentModel.DataAnnotations;

namespace SecureLoginSystemNew.ViewModels
{
    public class VerifyTwoFactorViewModel
    {
        [Required]
        public string Code { get; set; }
    }
}
