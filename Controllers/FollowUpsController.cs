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
    public class FollowUpsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private static readonly string[] ValidStatuses =
        {
            "Planned",
            "Completed",
            "Missed",
            "Cancelled"
        };

        private static readonly string[] ValidTypes =
        {
            "Call",
            "Meeting",
            "Email",
            "Task"
        };

        public FollowUpsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: FollowUps
        public async Task<IActionResult> Index()
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .AsQueryable();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId = _userManager.GetUserId(User);

                query = query.Where(f =>
                    f.AssignedTo == userId);
            }

            var followUps = await query
                .OrderBy(f => f.FollowUpDate)
                .ToListAsync();

            return View(followUps);
        }

        // GET: FollowUps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var followUp = await _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (followUp == null)
                return NotFound();

            if (!CanAccessFollowUp(followUp))
                return Forbid();

            return View(followUp);
        }

        // GET: FollowUps/Create
        public async Task<IActionResult> Create()
        {
            await LoadCustomersAsync();
            await LoadLeadsAsync();
            await LoadOpportunitiesAsync();
            await LoadSalesExecutivesAsync();

            var followUp = new FollowUp
            {
                FollowUpDate =
                    DateTime.Now.AddDays(1),
                Status = "Planned",
                Type = "Call"
            };

            if (User.IsInRole("SalesExecutive"))
            {
                followUp.AssignedTo =
                    _userManager.GetUserId(User);
            }

            return View(followUp);
        }

        // POST: FollowUps/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            FollowUp followUp)
        {
            if (User.IsInRole("SalesExecutive"))
            {
                followUp.AssignedTo =
                    _userManager.GetUserId(User);
            }

            await ValidateFollowUpAsync(followUp);

            if (!ModelState.IsValid)
            {
                await LoadCustomersAsync(
                    followUp.CustomerId);

                await LoadLeadsAsync(
                    followUp.LeadId);

                await LoadOpportunitiesAsync(
                    followUp.OpportunityId);

                await LoadSalesExecutivesAsync(
                    followUp.AssignedTo);

                return View(followUp);
            }

            followUp.CreatedDate =
                DateTime.UtcNow;

            _context.FollowUps.Add(followUp);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Follow-up created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: FollowUps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var followUp =
                await _context.FollowUps
                    .FirstOrDefaultAsync(f => f.Id == id);

            if (followUp == null)
                return NotFound();

            if (!CanAccessFollowUp(followUp))
                return Forbid();

            await LoadCustomersAsync(
                followUp.CustomerId);

            await LoadLeadsAsync(
                followUp.LeadId);

            await LoadOpportunitiesAsync(
                followUp.OpportunityId);

            await LoadSalesExecutivesAsync(
                followUp.AssignedTo);

            return View(followUp);
        }

        // POST: FollowUps/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            FollowUp followUp)
        {
            if (id != followUp.Id)
                return NotFound();

            var existingFollowUp =
                await _context.FollowUps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == id);

            if (existingFollowUp == null)
                return NotFound();

            if (!CanAccessFollowUp(
                    existingFollowUp))
            {
                return Forbid();
            }

            if (User.IsInRole("SalesExecutive"))
            {
                followUp.AssignedTo =
                    existingFollowUp.AssignedTo;
            }

            await ValidateFollowUpAsync(
                followUp);

            if (!ModelState.IsValid)
            {
                await LoadCustomersAsync(
                    followUp.CustomerId);

                await LoadLeadsAsync(
                    followUp.LeadId);

                await LoadOpportunitiesAsync(
                    followUp.OpportunityId);

                await LoadSalesExecutivesAsync(
                    followUp.AssignedTo);

                return View(followUp);
            }

            followUp.CreatedDate =
                existingFollowUp.CreatedDate;

            _context.Update(followUp);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Follow-up updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: FollowUps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var followUp =
                await _context.FollowUps
                    .Include(f => f.Customer)
                    .Include(f => f.Lead)
                    .Include(f => f.Opportunity)
                    .FirstOrDefaultAsync(f => f.Id == id);

            if (followUp == null)
                return NotFound();

            if (!CanAccessFollowUp(followUp))
                return Forbid();

            return View(followUp);
        }

        // POST: FollowUps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var followUp =
                await _context.FollowUps
                    .FirstOrDefaultAsync(f => f.Id == id);

            if (followUp == null)
                return NotFound();

            if (!CanAccessFollowUp(followUp))
                return Forbid();

            _context.FollowUps.Remove(followUp);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Follow-up deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool CanAccessFollowUp(
            FollowUp followUp)
        {
            if (User.IsInRole("Admin") ||
                User.IsInRole("Manager"))
            {
                return true;
            }

            if (User.IsInRole("SalesExecutive"))
            {
                var userId =
                    _userManager.GetUserId(User);

                return followUp.AssignedTo ==
                       userId;
            }

            return false;
        }

        private async Task ValidateFollowUpAsync(
            FollowUp followUp)
        {
            if (followUp.CustomerId <= 0)
            {
                ModelState.AddModelError(
                    "CustomerId",
                    "Customer is required.");
            }

            if (!ValidStatuses.Contains(
                    followUp.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Invalid follow-up status.");
            }

            if (!ValidTypes.Contains(
                    followUp.Type))
            {
                ModelState.AddModelError(
                    "Type",
                    "Invalid follow-up type.");
            }

            // New/planned follow-ups cannot be
            // scheduled before today.
            if ((followUp.Status == "Planned" ||
                 followUp.Status == "New") &&
                followUp.FollowUpDate.Date <
                DateTime.Today)
            {
                ModelState.AddModelError(
                    "FollowUpDate",
                    "A new or planned follow-up " +
                    "cannot be scheduled before today.");
            }

            if (User.IsInRole("SalesExecutive"))
            {
                followUp.AssignedTo =
                    _userManager.GetUserId(User);
            }

            // Customer must exist.
            if (followUp.CustomerId > 0)
            {
                var customerExists =
                    await _context.Customers
                        .AnyAsync(c =>
                            c.Id ==
                            followUp.CustomerId);

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
                                followUp.CustomerId &&
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

            // Lead must be accessible if selected.
            if (followUp.LeadId.HasValue)
            {
                var leadExists =
                    await _context.Leads
                        .AnyAsync(l =>
                            l.Id ==
                            followUp.LeadId.Value);

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
                                followUp.LeadId.Value &&
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

            // Opportunity must be accessible if selected.
            if (followUp.OpportunityId.HasValue)
            {
                var opportunityExists =
                    await _context.Opportunities
                        .AnyAsync(o =>
                            o.Id ==
                            followUp.OpportunityId.Value);

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
                                followUp.OpportunityId.Value &&
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

            // Only SalesExecutives may own CRM records.
            if (!string.IsNullOrWhiteSpace(
                    followUp.AssignedTo))
            {
                var assignedUser =
                    await _userManager.FindByIdAsync(
                        followUp.AssignedTo);

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
                            "Follow-ups can only be assigned " +
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
