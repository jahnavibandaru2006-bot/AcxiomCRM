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
    public class LeadsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private static readonly string[] ValidStatuses =
        {
            "New",
            "Contacted",
            "Qualified",
            "Unqualified",
            "Converted",
            "Lost"
        };

        private static readonly string[] ValidPriorities =
        {
            "Low",
            "Medium",
            "High"
        };

        public LeadsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Leads
        public async Task<IActionResult> Index(
            string? search,
            string? status)
        {
            var query = _context.Leads
                .Include(l => l.Customer)
                .AsQueryable();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId = _userManager.GetUserId(User);

                query = query.Where(l =>
                    l.AssignedTo == userId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(l =>
                    l.Name.Contains(search) ||
                    l.Email.Contains(search) ||
                    (l.Phone != null &&
                     l.Phone.Contains(search)) ||
                    (l.Company != null &&
                     l.Company.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l =>
                    l.Status == status);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Statuses = ValidStatuses;

            return View(await query
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync());
        }

        // GET: Leads/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var lead = await _context.Leads
                .Include(l => l.Customer)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lead == null)
                return NotFound();

            if (!CanAccessLead(lead))
                return Forbid();

            return View(lead);
        }

        // GET: Leads/Create
        public async Task<IActionResult> Create()
        {
            await LoadSalesExecutivesAsync();
            return View();
        }

        // POST: Leads/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lead lead)
        {
            if (User.IsInRole("SalesExecutive"))
            {
                lead.AssignedTo =
                    _userManager.GetUserId(User);
            }

            ValidateLead(lead);

            if (!ModelState.IsValid)
            {
                await LoadSalesExecutivesAsync(
                    lead.AssignedTo);

                return View(lead);
            }

            lead.CreatedDate = DateTime.UtcNow;

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            // Generate Lead Code after database ID exists.
            lead.LeadCode = $"LEAD-{lead.Id:D4}";

            _context.Update(lead);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Lead created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Leads/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lead == null)
                return NotFound();

            if (!CanAccessLead(lead))
                return Forbid();

            await LoadSalesExecutivesAsync(
                lead.AssignedTo);

            return View(lead);
        }

        // POST: Leads/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Lead lead)
        {
            if (id != lead.Id)
                return NotFound();

            var existingLead =
                await _context.Leads
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == id);

            if (existingLead == null)
                return NotFound();

            if (!CanAccessLead(existingLead))
                return Forbid();

            if (User.IsInRole("SalesExecutive"))
            {
                lead.AssignedTo =
                    existingLead.AssignedTo;
            }

            ValidateLead(lead);

            if (!IsValidStatusTransition(
                    existingLead.Status,
                    lead.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    $"Invalid status transition from " +
                    $"{existingLead.Status} to {lead.Status}.");
            }

            if (!ModelState.IsValid)
            {
                await LoadSalesExecutivesAsync(
                    lead.AssignedTo);

                return View(lead);
            }

            lead.LeadCode =
                existingLead.LeadCode;

            lead.CreatedDate =
                existingLead.CreatedDate;

            _context.Update(lead);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Lead updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Leads/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var lead = await _context.Leads
                .Include(l => l.Customer)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lead == null)
                return NotFound();

            if (!CanAccessLead(lead))
                return Forbid();

            return View(lead);
        }

        // POST: Leads/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lead == null)
                return NotFound();

            if (!CanAccessLead(lead))
                return Forbid();

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Lead deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Leads/Convert/5
        public async Task<IActionResult> Convert(int? id)
        {
            if (id == null)
                return NotFound();

            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lead == null)
                return NotFound();

            if (!CanAccessLead(lead))
                return Forbid();

            if (lead.Status != "Qualified")
            {
                TempData["Error"] =
                    "Only qualified leads can be converted.";

                return RedirectToAction(nameof(Index));
            }

            return View(lead);
        }

        // POST: Leads/Convert/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(
            int id,
            decimal? opportunityAmount)
        {
            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lead == null)
                return NotFound();

            if (!CanAccessLead(lead))
                return Forbid();

            if (lead.Status != "Qualified")
            {
                TempData["Error"] =
                    "Only qualified leads can be converted.";

                return RedirectToAction(nameof(Index));
            }

            if (opportunityAmount.HasValue &&
                opportunityAmount.Value <= 0)
            {
                ModelState.AddModelError(
                    "opportunityAmount",
                    "Opportunity amount must be greater than 0.");

                return View(lead);
            }

            // Find an existing customer using the lead email.
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.Email.ToLower() ==
                    lead.Email.ToLower());

            if (customer == null)
            {
                customer = new Customer
                {
                    Name = lead.Name,
                    Email = lead.Email,
                    Phone = lead.Phone ?? string.Empty,
                    Company = lead.Company,
                    Status = "Active",
                    AssignedTo = lead.AssignedTo,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Customers.Add(customer);

                await _context.SaveChangesAsync();

                customer.CustomerCode =
                    $"CUS-{customer.Id:D4}";

                await _context.SaveChangesAsync();
            }
            else if (string.IsNullOrWhiteSpace(
                         customer.AssignedTo))
            {
                customer.AssignedTo =
                    lead.AssignedTo;

                await _context.SaveChangesAsync();
            }

            lead.CustomerId = customer.Id;
            lead.Status = "Converted";

            await _context.SaveChangesAsync();

            // Create opportunity when an amount was supplied.
            if (opportunityAmount.HasValue)
            {
                var opportunity = new Opportunity
                {
                    Name =
                        $"{lead.Company ?? lead.Name} Opportunity",
                    CustomerId = customer.Id,
                    LeadId = lead.Id,
                    Amount = opportunityAmount.Value,
                    Stage = "Qualification",
                    Probability = 25,
                    ExpectedCloseDate =
                        DateTime.Today.AddDays(30),
                    Status = "Open",
                    AssignedTo = lead.AssignedTo,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] =
                "Lead converted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool CanAccessLead(Lead lead)
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

                return lead.AssignedTo == userId;
            }

            return false;
        }

        private void ValidateLead(Lead lead)
        {
            if (string.IsNullOrWhiteSpace(lead.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Lead name is required.");
            }

            if (string.IsNullOrWhiteSpace(lead.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Lead email is required.");
            }

            if (!ValidStatuses.Contains(lead.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Invalid lead status.");
            }

            if (!ValidPriorities.Contains(lead.Priority))
            {
                ModelState.AddModelError(
                    "Priority",
                    "Invalid lead priority.");
            }

            if (lead.ExpectedValue.HasValue &&
                lead.ExpectedValue.Value < 0)
            {
                ModelState.AddModelError(
                    "ExpectedValue",
                    "Expected value cannot be negative.");
            }
        }

        private bool IsValidStatusTransition(
            string oldStatus,
            string newStatus)
        {
            if (oldStatus == newStatus)
                return true;

            return oldStatus switch
            {
                "New" =>
                    newStatus == "Contacted" ||
                    newStatus == "Qualified" ||
                    newStatus == "Unqualified" ||
                    newStatus == "Lost",

                "Contacted" =>
                    newStatus == "Qualified" ||
                    newStatus == "Unqualified" ||
                    newStatus == "Lost",

                "Qualified" =>
                    newStatus == "Converted" ||
                    newStatus == "Lost",

                "Unqualified" =>
                    newStatus == "Contacted" ||
                    newStatus == "Qualified",

                "Lost" =>
                    newStatus == "Contacted",

                "Converted" => false,

                _ => false
            };
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
                    .GetUsersInRoleAsync("SalesExecutive");

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
