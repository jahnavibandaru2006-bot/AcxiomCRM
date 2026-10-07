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
    public class OpportunitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private static readonly string[] ValidStatuses =
        {
            "Open",
            "Won",
            "Lost"
        };

        public OpportunitiesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Opportunities
        public async Task<IActionResult> Index()
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .AsQueryable();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId = _userManager.GetUserId(User);

                query = query.Where(o =>
                    o.AssignedTo == userId);
            }

            var opportunities = await query
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            return View(opportunities);
        }

        // GET: Opportunities/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var opportunity = await _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (opportunity == null)
                return NotFound();

            if (!CanAccessOpportunity(opportunity))
                return Forbid();

            return View(opportunity);
        }

        // GET: Opportunities/Create
        public async Task<IActionResult> Create(
            int? customerId,
            int? leadId)
        {
            await LoadCustomersAsync(customerId);
            await LoadLeadsAsync(leadId);
            await LoadSalesExecutivesAsync();

            var opportunity = new Opportunity
            {
                CustomerId = customerId ?? 0,
                LeadId = leadId,
                ExpectedCloseDate =
                    DateTime.Today.AddDays(30),
                Status = "Open",
                Stage = "Qualification",
                Probability = 25
            };

            if (User.IsInRole("SalesExecutive"))
            {
                opportunity.AssignedTo =
                    _userManager.GetUserId(User);
            }

            return View(opportunity);
        }

        // POST: Opportunities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Opportunity opportunity)
        {
            if (User.IsInRole("SalesExecutive"))
            {
                opportunity.AssignedTo =
                    _userManager.GetUserId(User);
            }

            await ValidateOpportunityAsync(opportunity);

            if (!ModelState.IsValid)
            {
                await LoadCustomersAsync(
                    opportunity.CustomerId);

                await LoadLeadsAsync(
                    opportunity.LeadId);

                await LoadSalesExecutivesAsync(
                    opportunity.AssignedTo);

                return View(opportunity);
            }

            opportunity.CreatedDate =
                DateTime.UtcNow;

            _context.Opportunities.Add(opportunity);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Opportunity created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Opportunities/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var opportunity =
                await _context.Opportunities
                    .FirstOrDefaultAsync(o => o.Id == id);

            if (opportunity == null)
                return NotFound();

            if (!CanAccessOpportunity(opportunity))
                return Forbid();

            await LoadCustomersAsync(
                opportunity.CustomerId);

            await LoadLeadsAsync(
                opportunity.LeadId);

            await LoadSalesExecutivesAsync(
                opportunity.AssignedTo);

            return View(opportunity);
        }

        // POST: Opportunities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Opportunity opportunity)
        {
            if (id != opportunity.Id)
                return NotFound();

            var existingOpportunity =
                await _context.Opportunities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);

            if (existingOpportunity == null)
                return NotFound();

            if (!CanAccessOpportunity(
                    existingOpportunity))
            {
                return Forbid();
            }

            // SalesExecutive cannot change ownership.
            if (User.IsInRole("SalesExecutive"))
            {
                opportunity.AssignedTo =
                    existingOpportunity.AssignedTo;
            }

            await ValidateOpportunityAsync(
                opportunity);

            if (!ModelState.IsValid)
            {
                await LoadCustomersAsync(
                    opportunity.CustomerId);

                await LoadLeadsAsync(
                    opportunity.LeadId);

                await LoadSalesExecutivesAsync(
                    opportunity.AssignedTo);

                return View(opportunity);
            }

            opportunity.CreatedDate =
                existingOpportunity.CreatedDate;

            _context.Update(opportunity);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Opportunity updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Opportunities/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var opportunity =
                await _context.Opportunities
                    .Include(o => o.Customer)
                    .Include(o => o.Lead)
                    .FirstOrDefaultAsync(o => o.Id == id);

            if (opportunity == null)
                return NotFound();

            if (!CanAccessOpportunity(opportunity))
                return Forbid();

            return View(opportunity);
        }

        // POST: Opportunities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var opportunity =
                await _context.Opportunities
                    .FirstOrDefaultAsync(o => o.Id == id);

            if (opportunity == null)
                return NotFound();

            if (!CanAccessOpportunity(opportunity))
                return Forbid();

            _context.Opportunities.Remove(opportunity);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Opportunity deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool CanAccessOpportunity(
            Opportunity opportunity)
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

                return opportunity.AssignedTo ==
                       userId;
            }

            return false;
        }

        private async Task ValidateOpportunityAsync(
            Opportunity opportunity)
        {
            if (string.IsNullOrWhiteSpace(
                    opportunity.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Opportunity name is required.");
            }

            if (opportunity.CustomerId <= 0)
            {
                ModelState.AddModelError(
                    "CustomerId",
                    "Customer is required.");
            }

            // Required assignment validation.
            if (User.IsInRole("SalesExecutive"))
            {
                var userId =
                    _userManager.GetUserId(User);

                opportunity.AssignedTo = userId;
            }

            // Amount must be greater than zero.
            if (opportunity.Amount <= 0)
            {
                ModelState.AddModelError(
                    "Amount",
                    "Amount must be greater than 0.");
            }

            // Probability must be between 0 and 100.
            if (opportunity.Probability < 0 ||
                opportunity.Probability > 100)
            {
                ModelState.AddModelError(
                    "Probability",
                    "Probability must be between 0 and 100.");
            }

            if (!ValidStatuses.Contains(
                    opportunity.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Invalid opportunity status.");
            }

            // Active/Open opportunities cannot
            // have a past expected close date.
            if (opportunity.Status == "Open" &&
                opportunity.ExpectedCloseDate.Date
                    < DateTime.Today)
            {
                ModelState.AddModelError(
                    "ExpectedCloseDate",
                    "Expected close date cannot be in the past " +
                    "for an open opportunity.");
            }

            // Verify the selected customer is accessible.
            if (opportunity.CustomerId > 0)
            {
                var customerExists =
                    await _context.Customers
                        .AnyAsync(c =>
                            c.Id ==
                            opportunity.CustomerId);

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

                    var customerAccessible =
                        await _context.Customers
                            .AnyAsync(c =>
                                c.Id ==
                                opportunity.CustomerId &&
                                c.AssignedTo ==
                                userId);

                    if (!customerAccessible)
                    {
                        ModelState.AddModelError(
                            "CustomerId",
                            "You can only use customers assigned to you.");
                    }
                }
            }

            // Verify the selected lead is accessible.
            if (opportunity.LeadId.HasValue)
            {
                var leadExists =
                    await _context.Leads
                        .AnyAsync(l =>
                            l.Id ==
                            opportunity.LeadId.Value);

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

                    var leadAccessible =
                        await _context.Leads
                            .AnyAsync(l =>
                                l.Id ==
                                opportunity.LeadId.Value &&
                                l.AssignedTo ==
                                userId);

                    if (!leadAccessible)
                    {
                        ModelState.AddModelError(
                            "LeadId",
                            "You can only use leads assigned to you.");
                    }
                }
            }

            // Only SalesExecutives can be assigned
            // as CRM record owners.
            if (!string.IsNullOrWhiteSpace(
                    opportunity.AssignedTo))
            {
                var assignedUser =
                    await _userManager.FindByIdAsync(
                        opportunity.AssignedTo);

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
                            "Opportunities can only be assigned " +
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
