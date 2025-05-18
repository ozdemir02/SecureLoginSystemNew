using SecureLoginSystemNew.Models;

namespace SecureLoginSystemNew.Services
{
    public class SecureLogger
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SecureLogger> _logger;

        public SecureLogger(AppDbContext context, ILogger<SecureLogger> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogLoginAttemptAsync(string username, string ip, string userAgent, bool isSuccessful, string failureReason = null)
        {
            try
            {
                // Anonymous data
                var maskedUsername = username?.Length > 3 ?
                    username[..3] + new string('*', username.Length - 3) : "***";

                var maskedIp = ip?.Length > 7 ?
                    string.Join(".", ip.Split('.').Take(2)) + ".x.x" : "x.x.x.x";

                _context.LoginAttempts.Add(new LoginAttempt
                {
                    Username = maskedUsername,
                    IpAddress = maskedIp,
                    UserAgent = userAgent?.Substring(0, Math.Min(200, userAgent.Length)), // Begræns længden
                    IsSuccessful = isSuccessful,
                    FailureReason = isSuccessful ? null : failureReason
                });

                await _context.SaveChangesAsync();

                // logg
                _logger.LogInformation(isSuccessful ?
                    "Successful login for {MaskedUsername}" :
                    "Failed login for {MaskedUsername}. Reason: {FailureReason}",
                    maskedUsername, failureReason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging login attempt");
            }
        }
    }
}
