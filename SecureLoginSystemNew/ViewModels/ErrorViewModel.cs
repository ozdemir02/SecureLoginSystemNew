using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SecureLoginSystemNew.ViewModels
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public string ErrorMessage { get; set; }
        public int? StatusCode { get; set; }
        public string ExceptionMessage { get; set; } // Kun til admins
        public bool IsAdmin => HttpContextHelper.Current?.User.IsInRole("Admin") ?? false;
    }

    // Hjælpeklasse til håndtering af HttpContext
    public static class HttpContextHelper
    {
        private static IHttpContextAccessor _accessor;
        public static HttpContext Current => _accessor?.HttpContext;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _accessor = httpContextAccessor;
        }
    }
}