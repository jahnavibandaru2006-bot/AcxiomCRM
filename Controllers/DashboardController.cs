using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string period = "ThisMonth",
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var (from, to) = GetDateRange(
                period,
                fromDate,
                toDate);

            var customers = _context.Customers
                .AsQueryable();

            var leads = _context.Leads
                .AsQueryable();

            var opportunities = _context.Opportunities
                .AsQueryable();

            var followUps = _context.FollowUps
                .AsQueryable();

            // SalesExecutives only see assigned data.
            if (User.IsInRole("SalesExecutive"))
            {
                var userId =
                    _userManager.GetUserId(User);

                customers = customers.Where(c =>
                    c.AssignedTo == userId);

                leads = leads.Where(l =>
                    l.AssignedTo == userId);

                opportunities = opportunities.Where(o =>
                    o.AssignedTo == userId);

                followUps = followUps.Where(f =>
                    f.AssignedTo == userId);
            }

            // Date-scoped records.
            var filteredCustomers = customers.Where(c =>
                c.CreatedDate >= from &&
                c.CreatedDate <= to);

            var filteredLeads = leads.Where(l =>
                l.CreatedDate >= from &&
                l.CreatedDate <= to);

            var filteredOpportunities =
                opportunities.Where(o =>
                    o.CreatedDate >= from &&
                    o.CreatedDate <= to);

            // Follow-ups are not restricted to the selected
            // creation period. Pending means currently pending.
            var pendingFollowUps = followUps.Where(f =>
                f.Status == "Planned" &&
                f.FollowUpDate.Date >= DateTime.Today);

            var model = new DashboardViewModel
            {
                Period = period,
                FromDate = from,
                ToDate = to,

                TotalCustomers =
                    await filteredCustomers.CountAsync(),

                TotalLeads =
                    await filteredLeads.CountAsync(),

                OpenLeads =
                    await filteredLeads.CountAsync(l =>
                        l.Status != "Lost" &&
                        l.Status != "Converted"),

                TotalOpportunities =
                    await filteredOpportunities.CountAsync(),

                OpenOpportunities =
                    await filteredOpportunities.CountAsync(o =>
                        o.Status == "Open"),

                WonOpportunities =
                    await filteredOpportunities.CountAsync(o =>
                        o.Status == "Won"),

                LostOpportunities =
                    await filteredOpportunities.CountAsync(o =>
                        o.Status == "Lost"),

                PendingFollowUps =
                    await pendingFollowUps.CountAsync(),

                TotalPipelineValue =
                    await filteredOpportunities
                        .Where(o => o.Status == "Open")
                        .SumAsync(o => (decimal?)o.Amount) ?? 0
            };

            await BuildLeadStatusChart(
                filteredLeads,
                model);

            await BuildOpportunityPipelineChart(
                filteredOpportunities,
                model);

            await BuildMonthlySalesChart(
                opportunities,
                model);

            return View(model);
        }

        private async Task BuildLeadStatusChart(
            IQueryable<Lead> leads,
            DashboardViewModel model)
        {
            var statuses = new[]
            {
                "New",
                "Contacted",
                "Qualified",
                "Lost",
                "Converted"
            };

            var grouped = await leads
                .GroupBy(l => l.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            foreach (var status in statuses)
            {
                model.LeadStatusLabels.Add(status);

                model.LeadStatusValues.Add(
                    grouped
                        .FirstOrDefault(x =>
                            x.Status == status)?.Count ?? 0);
            }
        }

        private async Task BuildOpportunityPipelineChart(
            IQueryable<Opportunity> opportunities,
            DashboardViewModel model)
        {
            var labels = new[]
            {
                "Qualification",
                "Proposal",
                "Negotiation",
                "Won",
                "Lost"
            };

            var allOpportunities =
                await opportunities.ToListAsync();

            foreach (var label in labels)
            {
                model.OpportunityStageLabels.Add(label);

                var count = allOpportunities.Count(o =>
                {
                    if (o.Status == "Won")
                        return label == "Won";

                    if (o.Status == "Lost")
                        return label == "Lost";

                    return o.Stage == label;
                });

                model.OpportunityStageValues.Add(count);
            }
        }

        private async Task BuildMonthlySalesChart(
            IQueryable<Opportunity> opportunities,
            DashboardViewModel model)
        {
            var start =
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1).AddMonths(-5);

            var end =
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1).AddMonths(1);

            var wonOpportunities =
                await opportunities
                    .Where(o =>
                        o.Status == "Won" &&
                        o.CreatedDate >= start &&
                        o.CreatedDate < end)
                    .ToListAsync();

            for (var i = 5; i >= 0; i--)
            {
                var month =
                    new DateTime(
                        DateTime.Today.Year,
                        DateTime.Today.Month,
                        1).AddMonths(-i);

                var nextMonth = month.AddMonths(1);

                var amount =
                    wonOpportunities
                        .Where(o =>
                            o.CreatedDate >= month &&
                            o.CreatedDate < nextMonth)
                        .Sum(o => o.Amount);

                model.MonthlySalesLabels.Add(
                    month.ToString("MMM yyyy"));

                model.MonthlySalesValues.Add(amount);
            }
        }

        private (
            DateTime from,
            DateTime to)
            GetDateRange(
                string period,
                DateTime? customFrom,
                DateTime? customTo)
        {
            var today = DateTime.Today;

            switch (period)
            {
                case "Today":
                    return (
                        today,
                        today.AddDays(1).AddTicks(-1));

                case "ThisWeek":
                    var diff =
                        (7 + (today.DayOfWeek -
                              DayOfWeek.Monday)) % 7;

                    var weekStart =
                        today.AddDays(-diff);

                    return (
                        weekStart,
                        weekStart
                            .AddDays(7)
                            .AddTicks(-1));

                case "Custom":
                    var from =
                        customFrom?.Date ??
                        today.AddMonths(-1);

                    var to =
                        customTo?.Date ??
                        today;

                    return (
                        from,
                        to.AddDays(1).AddTicks(-1));

                case "ThisMonth":
                default:
                    var monthStart =
                        new DateTime(
                            today.Year,
                            today.Month,
                            1);

                    return (
                        monthStart,
                        monthStart
                            .AddMonths(1)
                            .AddTicks(-1));
            }
        }
    }
}
