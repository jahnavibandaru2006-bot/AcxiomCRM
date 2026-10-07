using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? userId,
            string? module,
            string? action,
            DateTime? fromDate,
            DateTime? toDate)
        {
            // Load audit records
            var allLogs = await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            IEnumerable<AuditLog> filteredLogs = allLogs;

            // User filter
            if (!string.IsNullOrWhiteSpace(userId))
            {
                filteredLogs = filteredLogs.Where(
                    x => x.UserId == userId);
            }

            // Module filter
            if (!string.IsNullOrWhiteSpace(module))
            {
                filteredLogs = filteredLogs.Where(
                    x => x.EntityName == module);
            }

            // Action filter
            if (!string.IsNullOrWhiteSpace(action))
            {
                filteredLogs = filteredLogs.Where(
                    x => x.Action == action);
            }

            // From date
            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                filteredLogs = filteredLogs.Where(
                    x => x.CreatedDate.ToLocalTime().Date >= startDate);
            }

            // To date
            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date;

                filteredLogs = filteredLogs.Where(
                    x => x.CreatedDate.ToLocalTime().Date <= endDate);
            }

            var logs = filteredLogs.ToList();

            // Dropdown values
            ViewBag.Users = allLogs
                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                .Select(x => x.UserId!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ViewBag.Modules = allLogs
                .Where(x => !string.IsNullOrWhiteSpace(x.EntityName))
                .Select(x => x.EntityName!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ViewBag.Actions = allLogs
                .Where(x => !string.IsNullOrWhiteSpace(x.Action))
                .Select(x => x.Action!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // Preserve filters
            ViewBag.SelectedUser = userId;
            ViewBag.SelectedModule = module;
            ViewBag.SelectedAction = action;

            ViewBag.FromDate =
                fromDate?.ToString("yyyy-MM-dd");

            ViewBag.ToDate =
                toDate?.ToString("yyyy-MM-dd");

            ViewBag.AuditCount = logs.Count;

            return View(logs);
        }
    }
}
