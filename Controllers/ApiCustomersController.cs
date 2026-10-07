using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize(Roles = "Admin,Manager,SalesExecutive")]
    public class ApiCustomersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ApiCustomersController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var query = _context.Customers.AsNoTracking();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId = _userManager.GetUserId(User);
                query = query.Where(x => x.AssignedTo == userId);
            }

            var customers = await query
                .OrderByDescending(x => x.Id)
                .Select(x => new ApiCustomerDto
                {
                    Id = x.Id,
                    CustomerCode = x.CustomerCode,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    Company = x.Company,
                    City = x.City,
                    State = x.State,
                    Address = x.Address,
                    Status = x.Status,
                    AssignedTo = x.AssignedTo,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync();

            return Ok(customers);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
                return NotFound(new { message = "Customer not found." });

            if (User.IsInRole("SalesExecutive") &&
                customer.AssignedTo != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return Ok(ToDto(customer));
        }

        [HttpPost]
        public async Task<IActionResult> Create(ApiCreateCustomerRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var emailExists = await _context.Customers
                .AnyAsync(x => x.Email.ToLower() == request.Email.ToLower());

            if (emailExists)
                return Conflict(new { message = "A customer with this email already exists." });

            var phoneExists = await _context.Customers
                .AnyAsync(x => x.Phone == request.Phone);

            if (phoneExists)
                return Conflict(new { message = "A customer with this phone already exists." });

            var validStatuses = new[] { "Active", "Inactive" };

            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { message = "Invalid customer status." });

            string? assignedTo = request.AssignedTo;

            if (User.IsInRole("SalesExecutive"))
            {
                assignedTo = _userManager.GetUserId(User);
            }
            else if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                var assignedUser = await _userManager.FindByIdAsync(assignedTo);

                if (assignedUser == null ||
                    !await _userManager.IsInRoleAsync(assignedUser, "SalesExecutive"))
                {
                    return BadRequest(new { message = "Assigned user must be a SalesExecutive." });
                }
            }

            var customer = new Customer
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone.Trim(),
                Company = request.Company,
                City = request.City,
                State = request.State,
                Address = request.Address,
                Status = request.Status,
                AssignedTo = assignedTo,
                Notes = request.Notes,
                CreatedDate = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            customer.CustomerCode = $"CUS-{customer.Id:D4}";
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(Get),
                new { id = customer.Id },
                ToDto(customer));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            ApiCreateCustomerRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
                return NotFound(new { message = "Customer not found." });

            if (User.IsInRole("SalesExecutive") &&
                customer.AssignedTo != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            var duplicateEmail = await _context.Customers
                .AnyAsync(x => x.Id != id &&
                               x.Email.ToLower() == request.Email.ToLower());

            if (duplicateEmail)
                return Conflict(new { message = "A customer with this email already exists." });

            var duplicatePhone = await _context.Customers
                .AnyAsync(x => x.Id != id &&
                               x.Phone == request.Phone);

            if (duplicatePhone)
                return Conflict(new { message = "A customer with this phone already exists." });

            if (!new[] { "Active", "Inactive" }.Contains(request.Status))
                return BadRequest(new { message = "Invalid customer status." });

            customer.Name = request.Name.Trim();
            customer.Email = request.Email.Trim();
            customer.Phone = request.Phone.Trim();
            customer.Company = request.Company;
            customer.City = request.City;
            customer.State = request.State;
            customer.Address = request.Address;
            customer.Status = request.Status;
            customer.Notes = request.Notes;

            if (!User.IsInRole("SalesExecutive"))
            {
                if (!string.IsNullOrWhiteSpace(request.AssignedTo))
                {
                    var assignedUser = await _userManager.FindByIdAsync(request.AssignedTo);

                    if (assignedUser == null ||
                        !await _userManager.IsInRoleAsync(assignedUser, "SalesExecutive"))
                    {
                        return BadRequest(new { message = "Assigned user must be a SalesExecutive." });
                    }
                }

                customer.AssignedTo = request.AssignedTo;
            }

            await _context.SaveChangesAsync();

            return Ok(ToDto(customer));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
                return NotFound(new { message = "Customer not found." });

            if (User.IsInRole("SalesExecutive") &&
                customer.AssignedTo != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static ApiCustomerDto ToDto(Customer x)
        {
            return new ApiCustomerDto
            {
                Id = x.Id,
                CustomerCode = x.CustomerCode,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Company = x.Company,
                City = x.City,
                State = x.State,
                Address = x.Address,
                Status = x.Status,
                AssignedTo = x.AssignedTo,
                CreatedDate = x.CreatedDate
            };
        }
    }
}
