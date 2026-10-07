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
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private static readonly string[] ValidStatuses =
        {
            "Active",
            "Inactive"
        };

        public CustomersController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Customers
        public async Task<IActionResult> Index(
            string? search,
            string? status)
        {
            var query = _context.Customers.AsQueryable();

            // SalesExecutives can only see their assigned customers.
            if (User.IsInRole("SalesExecutive"))
            {
                var currentUserId =
                    _userManager.GetUserId(User);

                query = query.Where(c =>
                    c.AssignedTo == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(c =>
                    c.Name.Contains(search) ||
                    c.Email.Contains(search) ||
                    c.Phone.Contains(search) ||
                    (c.Company != null &&
                     c.Company.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c =>
                    c.Status == status);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Statuses = ValidStatuses;

            return View(await query
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync());
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            if (!CanAccessCustomer(customer))
            {
                return Forbid();
            }

            return View(customer);
        }

        // GET: Customers/Create
        public async Task<IActionResult> Create()
        {
            await LoadSalesExecutivesAsync();

            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Customer customer)
        {
            // SalesExecutive can only assign the customer
            // to themselves.
            if (User.IsInRole("SalesExecutive"))
            {
                customer.AssignedTo =
                    _userManager.GetUserId(User);
            }

            await ValidateCustomerAsync(customer);

            if (!ModelState.IsValid)
            {
                await LoadSalesExecutivesAsync(
                    customer.AssignedTo);

                return View(customer);
            }

            customer.CustomerCode =
                await GenerateCustomerCodeAsync();

            customer.CreatedDate =
                DateTime.UtcNow;

            _context.Customers.Add(customer);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Customer created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer =
                await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            if (!CanAccessCustomer(customer))
            {
                return Forbid();
            }

            await LoadSalesExecutivesAsync(
                customer.AssignedTo);

            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            var existingCustomer =
                await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);

            if (existingCustomer == null)
            {
                return NotFound();
            }

            if (!CanAccessCustomer(existingCustomer))
            {
                return Forbid();
            }

            // SalesExecutive cannot change ownership.
            if (User.IsInRole("SalesExecutive"))
            {
                customer.AssignedTo =
                    existingCustomer.AssignedTo;
            }

            await ValidateCustomerAsync(
                customer,
                customer.Id);

            if (!ModelState.IsValid)
            {
                await LoadSalesExecutivesAsync(
                    customer.AssignedTo);

                return View(customer);
            }

            try
            {
                customer.CustomerCode =
                    existingCustomer.CustomerCode;

                customer.CreatedDate =
                    existingCustomer.CreatedDate;

                _context.Update(customer);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Customer updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(customer.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer =
                await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            if (!CanAccessCustomer(customer))
            {
                return Forbid();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var customer =
                await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            if (!CanAccessCustomer(customer))
            {
                return Forbid();
            }

            _context.Customers.Remove(customer);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Customer deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool CanAccessCustomer(
            Customer customer)
        {
            // Admin and Manager can access all customers.
            if (User.IsInRole("Admin") ||
                User.IsInRole("Manager"))
            {
                return true;
            }

            // SalesExecutive can only access
            // their own assigned customers.
            if (User.IsInRole("SalesExecutive"))
            {
                var currentUserId =
                    _userManager.GetUserId(User);

                return customer.AssignedTo ==
                       currentUserId;
            }

            return false;
        }

        private async Task ValidateCustomerAsync(
            Customer customer,
            int? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(customer.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Customer name is required.");
            }

            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Customer email is required.");
            }

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                ModelState.AddModelError(
                    "Phone",
                    "Customer phone is required.");
            }

            if (!ValidStatuses.Contains(customer.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Invalid customer status.");
            }

            var normalizedEmail =
                customer.Email.Trim().ToLower();

            var emailExists =
                await _context.Customers.AnyAsync(c =>
                    c.Email.ToLower() == normalizedEmail &&
                    (!currentId.HasValue ||
                     c.Id != currentId.Value));

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "A customer with this email already exists.");
            }

            var normalizedPhone =
                customer.Phone.Trim();

            var phoneExists =
                await _context.Customers.AnyAsync(c =>
                    c.Phone == normalizedPhone &&
                    (!currentId.HasValue ||
                     c.Id != currentId.Value));

            if (phoneExists)
            {
                ModelState.AddModelError(
                    "Phone",
                    "A customer with this phone number already exists.");
            }
        }

        private async Task<string>
            GenerateCustomerCodeAsync()
        {
            var nextId =
                (await _context.Customers
                    .Select(c => (int?)c.Id)
                    .MaxAsync() ?? 0) + 1;

            return $"CUS-{nextId:D4}";
        }

        private async Task LoadSalesExecutivesAsync(
            string? selectedUserId = null)
        {
            // SalesExecutive should only be able
            // to assign records to themselves.
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

            var salesUsers =
                salesExecutives
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
                    salesUsers,
                    "Id",
                    "DisplayName",
                    selectedUserId);
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers
                .Any(c => c.Id == id);
        }
    }
}
