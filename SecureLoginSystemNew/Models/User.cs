namespace SecureLoginSystemNew.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public required string Email { get; set; }
        public bool IsEmailVerified { get; set; }
        public string TwoFactorSecret { get; set; } = string.Empty; // generer med tom string
        public bool TwoFactorEnabled { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
