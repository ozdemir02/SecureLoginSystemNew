namespace SecureLoginSystemNew.Services
{
    public class PasswordHasher
    {
        public (string Hash, string Salt) HashPassword(string password)
        {
            string salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            string hash = BCrypt.Net.BCrypt.HashPassword(password, salt);
            return (hash, salt);
        }

        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
