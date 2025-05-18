namespace SecureLoginSystemNew.Models
{
    public class LoginAttempt
    {
        public int Id { get; set; }
        public string Username { get; set; } // anonymiseret navn
        public string IpAddress { get; set; } // maskeret ip
        public string UserAgent { get; set; }
        public DateTime AttemptTime { get; set; } = DateTime.UtcNow;
        public bool IsSuccessful { get; set; }
        public string FailureReason { get; set; } // fejltype

    }
}