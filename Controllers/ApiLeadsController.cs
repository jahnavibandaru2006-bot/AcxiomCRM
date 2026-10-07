using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [ApiController]
    [Route("api/leads")]
    [Authorize(Roles = "Admin,Manager,SalesExecutive")]
    public class ApiLeadsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ApiLeadsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var query = _context.Leads.AsNoTracking();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId = _userManager.GetUserId(User);
                query = query.Where(x => x.AssignedTo == userId);
            }

            var leads = await query
                .OrderByDescending(x => x.Id)
                .Select(x => new ApiLeadDto
                {
                    Id = x.Id,
                    LeadCode = x.LeadCode,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    Company = x.Company,
                    Source = x.Source,
                    Priority = x.Priority,
                    ExpectedValue = x.ExpectedValue,
                    Status = x.Status,
                    AssignedTo = x.AssignedTo,
                    CustomerId = x.CustomerId,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync();

            return Ok(leads);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var lead = await _context.Leads
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (lead == null)
                return NotFound(new { message = "Lead not found." });

            if (User.IsInRole("SalesExecutive") &&
                lead.AssignedTo != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return Ok(ToDto(lead));
        }

        [HttpPost]
        public async Task<IActionResult> Create(ApiCreateLeadRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var validPriorities = new[] { "Low", "Medium", "High" };
            var validStatuses = new[]
            {
                "New", "Contacted", "Qualified",
                "Unqualified", "Converted", "Lost"
            };

            if (!validPriorities.Contains(request.Priority))
                return BadRequest(new { message = "Invalid lead priority." });

            var assignedTo = request.AssignedTo;

            if (User.IsInRole("SalesExecutive"))
            {
                assignedTo = _userManager.GetUserId(User);
            }
            else if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                var user = await _userManager.FindByIdAsync(assignedTo);

                if (user == null ||
                    !await _userManager.IsInRoleAsync(user, "SalesExecutive"))
                {
                    return BadRequest(new { message = "Assigned user must be a SalesExecutive." });
                }
            }

            var lead = new Lead
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone,
                Company = request.Company,
                Source = request.Source,
                Priority = request.Priority,
                ExpectedValue = request.ExpectedValue,
                Status = "New",
                AssignedTo = assignedTo,
                Notes = request.Notes,
                CreatedDate = DateTime.UtcNow
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            lead.LeadCode = $"LEAD-{lead.Id:D4}";
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(Get),
                new { id = lead.Id },
                ToDto(lead));
        }

        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] string status)
        {
            var validStatuses = new[]
            {
                "New", "Contacted", "Qualified",
                "Unqualified", "Converted", "Lost"
            };

            if (!validStatuses.Contains(status))
                return BadRequest(new { message = "Invalid lead status." });

            var lead = await _context.Leads.FindAsync(id);

            if (lead == null)
                return NotFound(new { message = "Lead not found." });

            if (User.IsInRole("SalesExecutive") &&
                lead.AssignedTo != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            if (!CanTransition(lead.Status, status))
            {
                return BadRequest(new
                {
                    message = $"Invalid status transition from {lead.Status} to {status}."
                });
            }

            lead.Status = status;
            await _context.SaveChangesAsync();

            return Ok(ToDto(lead));
        }

        private static bool CanTransition(string current, string next)
        {
            if (current == next)
                return true;

            return current switch
            {
                "New" => next is "Contacted" or "Qualified" or "Unqualified" or "Lost",
                "Contacted" => next is "Qualified" or "Unqualified" or "Lost",
                "Qualified" => next is "Converted" or "Lost",
                "Unqualified" => next is "Contacted" or "Qualified",
                "Lost" => next == "Contacted",
                "Converted" => false,
                _ => false
            };
        }

        private static ApiLeadDto ToDto(Lead x)
        {
            return new ApiLeadDto
            {
                Id = x.Id,
                LeadCode = x.LeadCode,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Company = x.Company,
                Source = x.Source,
                Priority = x.Priority,
                ExpectedValue = x.ExpectedValue,
                Status = x.Status,
                AssignedTo = x.AssignedTo,
                CustomerId = x.CustomerId,
                CreatedDate = x.CreatedDate
            };
        }
    }
}
