using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace SecureLoginSystemNew.Models
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    _configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // model conf
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.PasswordHash).HasMaxLength(255);
            });

            modelBuilder.Entity<LoginAttempt>(entity =>
            {
                entity.Property(a => a.IpAddress).HasMaxLength(45);
                entity.Property(a => a.UserAgent).HasMaxLength(500);
                entity.Property(a => a.FailureReason).HasMaxLength(500);
            });
        }
    }
}