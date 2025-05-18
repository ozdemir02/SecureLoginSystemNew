using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureLoginSystemNew.Models;
using SecureLoginSystemNew.ViewModels;
using SecureLoginSystemNew.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using OtpNet;
using QRCoder;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using BCrypt.Net;

namespace SecureLoginSystemNew.Controllers
{
    [AllowAnonymous]
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TwoFactorService _twoFactorService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            AppDbContext context,
            TwoFactorService twoFactorService,
            IMemoryCache cache,
            ILogger<AccountController> logger)
        {
            _context = context;
            _twoFactorService = twoFactorService;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet("Register")]
        public IActionResult Register() => View();

        [HttpPost("Register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username is already taken");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 12),
                    TwoFactorSecret = _twoFactorService.GenerateSecretKey(),
                    CreatedAt = DateTime.UtcNow,
                    IsEmailVerified = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New user registered: {user.Username}");
                return RedirectToAction("Login");
            }
            return View(model);
        }

        [HttpGet("Login")]
        public IActionResult Login() => View();

        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                await LogLoginAttempt(model.Username, false, "Invalid credentials");
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }

            // session håndtering
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            if (user.TwoFactorEnabled)
            {
                var token = Guid.NewGuid().ToString();
                _cache.Set(token, user.Id, TimeSpan.FromMinutes(5)); // kort levetid
                HttpContext.Session.SetString("2FA_Token", token);

                _logger.LogInformation($"2FA initiated for {user.Username} from IP: {HttpContext.Connection.RemoteIpAddress}");
                return RedirectToAction("VerifyTwoFactor");
            }

            await SignInUser(user);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("VerifyTwoFactor")]
        public IActionResult VerifyTwoFactor()
        {
            var token = HttpContext.Session.GetString("2FA_Token");
            if (string.IsNullOrEmpty(token) || !_cache.TryGetValue(token, out int _))
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost("VerifyTwoFactor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorViewModel model)
        {
            var token = HttpContext.Session.GetString("2FA_Token");
            if (!_cache.TryGetValue(token, out int userId))
            {
                ModelState.AddModelError("", "Session expired");
                return View(model);
            }

            var user = await _context.Users.FindAsync(userId);
            if (!_twoFactorService.VerifyCode(user.TwoFactorSecret, model.Code))
            {
                ModelState.AddModelError("Code", "Invalid verification code");
                return View(model);
            }

            _cache.Remove(token);
            await SignInUser(user);

            _logger.LogInformation($"Successful 2FA login for {user.Username}");
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet("EnableTwoFactor")]
        public async Task<IActionResult> EnableTwoFactor()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new EnableTwoFactorViewModel
            {
                SecretKey = user.TwoFactorSecret,
                QrCodeImage = _twoFactorService.GenerateQrCode(user.Email, user.TwoFactorSecret)
            };

            return View(model);
        }

        [Authorize]
        [HttpPost("EnableTwoFactor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            model.SecretKey = user.TwoFactorSecret;
            model.QrCodeImage = _twoFactorService.GenerateQrCode(user.Email, user.TwoFactorSecret);

            if (string.IsNullOrEmpty(model.Code) || model.Code.Length != 6)
            {
                ModelState.AddModelError("Code", "Please enter a valid 6-digit code");
                return View(model);
            }

            if (!_twoFactorService.VerifyCode(user.TwoFactorSecret, model.Code))
            {
                ModelState.AddModelError("Code", "Invalid verification code");
                return View(model);
            }

            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();

            await SignInUser(user);

            TempData["StatusMessage"] = "Two-factor authentication has been enabled successfully!";
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("TwoFactorEnabled", user.TwoFactorEnabled ? "true" : "false"),
                new Claim("SessionId", Guid.NewGuid().ToString()) // unik id
            };

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                IssuedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(20)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                authProperties);
        }

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            _logger.LogInformation($"User logged out from IP: {HttpContext.Connection.RemoteIpAddress}");
            return RedirectToAction("Index", "Home");
        }

        private async Task LogLoginAttempt(string username, bool isSuccessful, string failureReason = null)
        {
            try
            {
                var loginAttempt = new LoginAttempt
                {
                    Username = username,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IsSuccessful = isSuccessful,
                    FailureReason = failureReason
                };

                _context.LoginAttempts.Add(loginAttempt);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log login attempt");
            }
        }
    }
}