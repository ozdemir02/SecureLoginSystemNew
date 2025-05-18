using OtpNet;
using QRCoder;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SecureLoginSystemNew.Services
{
    public class TwoFactorService
    {
        private readonly ILogger<TwoFactorService> _logger;

        public TwoFactorService(ILogger<TwoFactorService> logger)
        {
            _logger = logger;
        }

        public string GenerateSecretKey() // secret nøgle
        {
            var key = KeyGeneration.GenerateRandomKey(20); // 20 bytes nøgle
            _logger.LogInformation("Generated new 2FA secret key");
            return Base32Encoding.ToString(key);
        }

        public string GenerateQrCode(string email, string secretKey, string appName = "SecureLoginSystem") // generering af 2fa
        {
            var issuer = Uri.EscapeDataString(appName);
            var label = Uri.EscapeDataString(email);
            var uri = $"otpauth://totp/{issuer}:{label}?secret={secretKey}&issuer={issuer}";

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(5);

            _logger.LogInformation("Generated QR code for 2FA setup");
            return $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
        }

        public bool VerifyCode(string secretKey, string userInputCode) // verificering af 2fa
        {
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogWarning("Secret key is null or empty during verification");
                return false;
            }

            if (string.IsNullOrWhiteSpace(userInputCode) || userInputCode.Length != 6)
            {
                _logger.LogWarning("Invalid user input code format");
                return false;
            }

            try
            {
                var totp = new Totp(Base32Encoding.ToBytes(secretKey));
                var result = totp.VerifyTotp(userInputCode, out _, new VerificationWindow(1, 1));
                _logger.LogInformation($"2FA verification attempt - Code: {userInputCode}, Result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during 2FA code verification");
                return false;
            }
        }
    }
}