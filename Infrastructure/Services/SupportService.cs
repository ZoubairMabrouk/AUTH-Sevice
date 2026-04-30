using AUTH_Sevice.Application.Common.Intefaces;
using System.Security.Claims;

namespace AUTH_Sevice.Infrastructure.Services
{

    public class BcryptPasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12;

        public string Hash(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        public bool Verify(string password, string hash) =>
            BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;
        private readonly HttpContext? _context = httpContextAccessor.HttpContext;

        public Guid? UserId
        {
            get
            {
                var sub = _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? _user?.FindFirst("sub")?.Value;
                return Guid.TryParse(sub, out var id) ? id : null;
            }
        }

        public string? Email => _user?.FindFirst(ClaimTypes.Email)?.Value;
        public string? Role => _user?.FindFirst(ClaimTypes.Role)?.Value;

        public string? IpAddress =>
            _context?.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? _context?.Connection.RemoteIpAddress?.ToString();
    }

}
