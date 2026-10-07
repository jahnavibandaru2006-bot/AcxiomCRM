using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin,Manager,SalesExecutive")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var model = new ReportsViewModel();

            var customers = _context.Customers.AsNoTracking();
            var leads = _context.Leads.AsNoTracking();
            var opportunities = _context.Opportunities.AsNoTracking();
            var followUps = _context.FollowUps.AsNoTracking();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId = _userManager.GetUserId(User);

                customers = customers.Where(x => x.AssignedTo == userId);
                leads = leads.Where(x => x.AssignedTo == userId);
                opportunities = opportunities.Where(x => x.AssignedTo == userId);
                followUps = followUps.Where(x => x.AssignedTo == userId);
            }

            var customerList = await customers.ToListAsync();
            var leadList = await leads.ToListAsync();
            var opportunityList = await opportunities.ToListAsync();
            var followUpList = await followUps.ToListAsync();

            // CUSTOMER REPORT
            model.TotalCustomers = customerList.Count;
            model.ActiveCustomers =
                customerList.Count(x => x.Status == "Active");
            model.InactiveCustomers =
                customerList.Count(x => x.Status == "Inactive");

            model.CustomerStatusReport = customerList
                .GroupBy(x => x.Status)
                .Select(g => new ReportRow
                {
                    Label = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // LEAD REPORT
            model.TotalLeads = leadList.Count;
            model.NewLeads =
                leadList.Count(x => x.Status == "New");
            model.QualifiedLeads =
                leadList.Count(x => x.Status == "Qualified");
            model.ConvertedLeads =
                leadList.Count(x => x.Status == "Converted");
            model.LostLeads =
                leadList.Count(x => x.Status == "Lost");

            model.LeadStatusReport = leadList
                .GroupBy(x => x.Status)
                .Select(g => new ReportRow
                {
                    Label = g.Key,
                    Count = g.Count(),
                    Value = g.Sum(x => x.ExpectedValue ?? 0)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // FOLLOW-UP REPORT
            model.TotalFollowUps = followUpList.Count;
            model.PlannedFollowUps =
                followUpList.Count(x => x.Status == "Planned");
            model.CompletedFollowUps =
                followUpList.Count(x => x.Status == "Completed");
            model.MissedFollowUps =
                followUpList.Count(x => x.Status == "Missed");

            model.OverdueFollowUps = followUpList.Count(x =>
                x.FollowUpDate < DateTime.UtcNow &&
                x.Status == "Planned");

            model.FollowUpStatusReport = followUpList
                .GroupBy(x => x.Status)
                .Select(g => new ReportRow
                {
                    Label = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // OPPORTUNITY REPORT
            model.TotalOpportunities = opportunityList.Count;
            model.OpenOpportunities =
                opportunityList.Count(x => x.Status == "Open");
            model.WonOpportunities =
                opportunityList.Count(x => x.Status == "Won");
            model.LostOpportunities =
                opportunityList.Count(x => x.Status == "Lost");

            model.TotalPipelineValue = opportunityList
                .Where(x => x.Status == "Open")
                .Sum(x => x.Amount);

            model.WeightedPipelineValue = opportunityList
                .Where(x => x.Status == "Open")
                .Sum(x => x.WeightedValue);

            model.WonRevenue = opportunityList
                .Where(x => x.Status == "Won")
                .Sum(x => x.Amount);

            model.OpportunityStageReport = opportunityList
                .GroupBy(x => x.Stage)
                .Select(g => new ReportRow
                {
                    Label = g.Key,
                    Count = g.Count(),
                    Value = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            // CONVERSION REPORT
            model.ConversionRate = model.TotalLeads == 0
                ? 0
                : Math.Round(
                    (decimal)model.ConvertedLeads /
                    model.TotalLeads * 100, 2);

            // USER ACTIVITY + AUDIT REPORT
            var audits = _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedDate);

            if (User.IsInRole("SalesExecutive"))
            {
                var currentUserId = _userManager.GetUserId(User);
                audits = audits
                    .Where(x => x.UserId == currentUserId)
                    .OrderByDescending(x => x.CreatedDate);
            }

            var auditList = await audits
                .Take(500)
                .ToListAsync();

            model.AuditReport = auditList
                .Select(x => new AuditReportRow
                {
                    Date = x.CreatedDate,
                    UserId = x.UserId,
                    Action = x.Action,
                    EntityName = x.EntityName,
                    RecordId = x.RecordId,
                    IpAddress = x.IpAddress
                })
                .ToList();

            model.UserActivityReport = auditList
                .GroupBy(x => new { x.UserId, x.Action })
                .Select(g => new AuditReportRow
                {
                    UserId = g.Key.UserId,
                    Action = g.Key.Action,
                    EntityName = "All",
                    RecordId = g.Count().ToString()
                })
                .OrderByDescending(x => x.RecordId)
                .ToList();

            return View(model);
        }
    }
}
