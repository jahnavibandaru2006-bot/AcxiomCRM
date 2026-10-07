using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin,Manager,SalesExecutive")]
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private static readonly string[] ValidTypes =
        {
            "Call",
            "Meeting",
            "Email",
            "Task"
        };

        private static readonly string[] ValidStatuses =
        {
            "Planned",
            "Completed",
            "Missed",
            "Cancelled"
        };

        public ActivitiesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Activities
        public async Task<IActionResult> Index()
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .AsQueryable();

            // SalesExecutives can only see their activities.
            if (User.IsInRole("SalesExecutive"))
            {
                var userId =
                    _userManager.GetUserId(User);

                query = query.Where(a =>
                    a.AssignedTo == userId);
            }

            var activities = await query
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            return View(activities);
        }

        // GET: Activities/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var activity = await _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
                return NotFound();

            if (!CanAccessActivity(activity))
                return Forbid();

            return View(activity);
        }

        // GET: Activities/Create
        public async Task<IActionResult> Create()
        {
            await LoadCustomersAsync();
            await LoadLeadsAsync();
            await LoadOpportunitiesAsync();
            await LoadSalesExecutivesAsync();

            var activity = new Activity
            {
                Type = "Call",
                Status = "Planned",
                ActivityDate = DateTime.Now.AddDays(1)
            };

            if (User.IsInRole("SalesExecutive"))
            {
                activity.AssignedTo =
                    _userManager.GetUserId(User);
            }

            return View(activity);
        }

        // POST: Activities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Activity activity)
        {
            if (User.IsInRole("SalesExecutive"))
            {
                activity.AssignedTo =
                    _userManager.GetUserId(User);
            }

            await ValidateActivityAsync(activity);

            if (!ModelState.IsValid)
            {
                await LoadCustomersAsync(
                    activity.CustomerId);

                await LoadLeadsAsync(
                    activity.LeadId);

                await LoadOpportunitiesAsync(
                    activity.OpportunityId);

                await LoadSalesExecutivesAsync(
                    activity.AssignedTo);

                return View(activity);
            }

            activity.CreatedDate =
                DateTime.UtcNow;

            _context.Activities.Add(activity);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Activity created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Activities/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var activity =
                await _context.Activities
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
                return NotFound();

            if (!CanAccessActivity(activity))
                return Forbid();

            await LoadCustomersAsync(
                activity.CustomerId);

            await LoadLeadsAsync(
                activity.LeadId);

            await LoadOpportunitiesAsync(
                activity.OpportunityId);

            await LoadSalesExecutivesAsync(
                activity.AssignedTo);

            return View(activity);
        }

        // POST: Activities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Activity activity)
        {
            if (id != activity.Id)
                return NotFound();

            var existingActivity =
                await _context.Activities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (existingActivity == null)
                return NotFound();

            if (!CanAccessActivity(existingActivity))
                return Forbid();

            // SalesExecutive cannot change ownership.
            if (User.IsInRole("SalesExecutive"))
            {
                activity.AssignedTo =
                    existingActivity.AssignedTo;
            }

            await ValidateActivityAsync(activity);

            if (!ModelState.IsValid)
            {
                await LoadCustomersAsync(
                    activity.CustomerId);

                await LoadLeadsAsync(
                    activity.LeadId);

                await LoadOpportunitiesAsync(
                    activity.OpportunityId);

                await LoadSalesExecutivesAsync(
                    activity.AssignedTo);

                return View(activity);
            }

            activity.CreatedDate =
                existingActivity.CreatedDate;

            _context.Update(activity);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Activity updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Activities/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var activity =
                await _context.Activities
                    .Include(a => a.Customer)
                    .Include(a => a.Lead)
                    .Include(a => a.Opportunity)
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
                return NotFound();

            if (!CanAccessActivity(activity))
                return Forbid();

            return View(activity);
        }

        // POST: Activities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var activity =
                await _context.Activities
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
                return NotFound();

            if (!CanAccessActivity(activity))
                return Forbid();

            _context.Activities.Remove(activity);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Activity deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool CanAccessActivity(
            Activity activity)
        {
            // Admin and Manager have team-level access.
            if (User.IsInRole("Admin") ||
                User.IsInRole("Manager"))
            {
                return true;
            }

            // SalesExecutive only sees assigned activities.
            if (User.IsInRole("SalesExecutive"))
            {
                var userId =
                    _userManager.GetUserId(User);

                return activity.AssignedTo ==
                       userId;
            }

            return false;
        }

        private async Task ValidateActivityAsync(
            Activity activity)
        {
            if (string.IsNullOrWhiteSpace(
                    activity.Subject))
            {
                ModelState.AddModelError(
                    "Subject",
                    "Activity subject is required.");
            }

            if (!ValidTypes.Contains(activity.Type))
            {
                ModelState.AddModelError(
                    "Type",
                    "Invalid activity type.");
            }

            if (!ValidStatuses.Contains(
                    activity.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Invalid activity status.");
            }

            // Planned activities cannot be scheduled
            // before today.
            if (activity.Status == "Planned" &&
                activity.ActivityDate.Date <
                DateTime.Today)
            {
                ModelState.AddModelError(
                    "ActivityDate",
                    "A planned activity cannot be scheduled " +
                    "before today.");
            }

            if (User.IsInRole("SalesExecutive"))
            {
                activity.AssignedTo =
                    _userManager.GetUserId(User);
            }

            // At least one CRM entity must be associated
            // with the activity.
            if (!activity.CustomerId.HasValue &&
                !activity.LeadId.HasValue &&
                !activity.OpportunityId.HasValue)
            {
                ModelState.AddModelError(
                    "",
                    "Activity must be linked to a customer, " +
                    "lead, or opportunity.");
            }

            // Customer validation.
            if (activity.CustomerId.HasValue)
            {
                var customerExists =
                    await _context.Customers
                        .AnyAsync(c =>
                            c.Id ==
                            activity.CustomerId.Value);

                if (!customerExists)
                {
                    ModelState.AddModelError(
                        "CustomerId",
                        "Selected customer does not exist.");
                }
                else if (
                    User.IsInRole("SalesExecutive"))
                {
                    var userId =
                        _userManager.GetUserId(User);

                    var accessible =
                        await _context.Customers
                            .AnyAsync(c =>
                                c.Id ==
                                activity.CustomerId.Value &&
                                c.AssignedTo ==
                                userId);

                    if (!accessible)
                    {
                        ModelState.AddModelError(
                            "CustomerId",
                            "You can only select a customer assigned to you.");
                    }
                }
            }

            // Lead validation.
            if (activity.LeadId.HasValue)
            {
                var leadExists =
                    await _context.Leads
                        .AnyAsync(l =>
                            l.Id ==
                            activity.LeadId.Value);

                if (!leadExists)
                {
                    ModelState.AddModelError(
                        "LeadId",
                        "Selected lead does not exist.");
                }
                else if (
                    User.IsInRole("SalesExecutive"))
                {
                    var userId =
                        _userManager.GetUserId(User);

                    var accessible =
                        await _context.Leads
                            .AnyAsync(l =>
                                l.Id ==
                                activity.LeadId.Value &&
                                l.AssignedTo ==
                                userId);

                    if (!accessible)
                    {
                        ModelState.AddModelError(
                            "LeadId",
                            "You can only select a lead assigned to you.");
                    }
                }
            }

            // Opportunity validation.
            if (activity.OpportunityId.HasValue)
            {
                var opportunityExists =
                    await _context.Opportunities
                        .AnyAsync(o =>
                            o.Id ==
                            activity.OpportunityId.Value);

                if (!opportunityExists)
                {
                    ModelState.AddModelError(
                        "OpportunityId",
                        "Selected opportunity does not exist.");
                }
                else if (
                    User.IsInRole("SalesExecutive"))
                {
                    var userId =
                        _userManager.GetUserId(User);

                    var accessible =
                        await _context.Opportunities
                            .AnyAsync(o =>
                                o.Id ==
                                activity.OpportunityId.Value &&
                                o.AssignedTo ==
                                userId);

                    if (!accessible)
                    {
                        ModelState.AddModelError(
                            "OpportunityId",
                            "You can only select an opportunity assigned to you.");
                    }
                }
            }

            // Only SalesExecutives can own CRM activities.
            if (!string.IsNullOrWhiteSpace(
                    activity.AssignedTo))
            {
                var assignedUser =
                    await _userManager.FindByIdAsync(
                        activity.AssignedTo);

                if (assignedUser == null)
                {
                    ModelState.AddModelError(
                        "AssignedTo",
                        "Selected user does not exist.");
                }
                else
                {
                    var isSalesExecutive =
                        await _userManager
                            .IsInRoleAsync(
                                assignedUser,
                                "SalesExecutive");

                    if (!isSalesExecutive)
                    {
                        ModelState.AddModelError(
                            "AssignedTo",
                            "Activities can only be assigned " +
                            "to Sales Executives.");
                    }
                }
            }
        }

        private async Task LoadCustomersAsync(
            int? selectedCustomerId = null)
        {
            var query = _context.Customers
                .AsQueryable();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId =
                    _userManager.GetUserId(User);

                query = query.Where(c =>
                    c.AssignedTo == userId);
            }

            var customers = await query
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewData["CustomerId"] =
                new SelectList(
                    customers,
                    "Id",
                    "Name",
                    selectedCustomerId);
        }

        private async Task LoadLeadsAsync(
            int? selectedLeadId = null)
        {
            var query = _context.Leads
                .AsQueryable();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId =
                    _userManager.GetUserId(User);

                query = query.Where(l =>
                    l.AssignedTo == userId);
            }

            var leads = await query
                .OrderBy(l => l.Name)
                .ToListAsync();

            ViewData["LeadId"] =
                new SelectList(
                    leads,
                    "Id",
                    "Name",
                    selectedLeadId);
        }

        private async Task LoadOpportunitiesAsync(
            int? selectedOpportunityId = null)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .AsQueryable();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId =
                    _userManager.GetUserId(User);

                query = query.Where(o =>
                    o.AssignedTo == userId);
            }

            var opportunities = await query
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            var items = opportunities
                .Select(o => new
                {
                    Id = o.Id,
                    DisplayName =
                        o.Name +
                        " - " +
                        (o.Customer != null
                            ? o.Customer.Name
                            : "No Customer")
                })
                .ToList();

            ViewData["OpportunityId"] =
                new SelectList(
                    items,
                    "Id",
                    "DisplayName",
                    selectedOpportunityId);
        }

        private async Task LoadSalesExecutivesAsync(
            string? selectedUserId = null)
        {
            if (User.IsInRole("SalesExecutive"))
            {
                var currentUserId =
                    _userManager.GetUserId(User);

                var currentUser =
                    await _userManager.FindByIdAsync(
                        currentUserId!);

                var users = new[]
                {
                    new
                    {
                        Id = currentUserId!,
                        DisplayName =
                            currentUser?.Email ??
                            currentUser?.UserName ??
                            "Current User"
                    }
                };

                ViewData["AssignedTo"] =
                    new SelectList(
                        users,
                        "Id",
                        "DisplayName",
                        currentUserId);

                return;
            }

            var salesExecutives =
                await _userManager
                    .GetUsersInRoleAsync(
                        "SalesExecutive");

            var usersList = salesExecutives
                .OrderBy(u => u.Email)
                .Select(u => new
                {
                    Id = u.Id,
                    DisplayName =
                        u.Email ??
                        u.UserName ??
                        u.Id
                })
                .ToList();

            ViewData["AssignedTo"] =
                new SelectList(
                    usersList,
                    "Id",
                    "DisplayName",
                    selectedUserId);
        }
    }
}
