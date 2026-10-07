using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Services
{
    public class AuthAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthAuditService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string action,
            string? userId = null,
            string? email = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var ipAddress =
                httpContext?.Connection?.RemoteIpAddress?.ToString();

            var audit = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = "Authentication",
                RecordId = email,
                NewValue = email,
                CreatedDate = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
    }
}
