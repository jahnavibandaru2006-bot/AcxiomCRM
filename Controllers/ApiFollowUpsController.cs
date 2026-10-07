using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [ApiController]
    [Route("api/followups")]
    [Authorize(Roles = "Admin,Manager,SalesExecutive")]
    public class ApiFollowUpsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ApiFollowUpsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var query = _context.FollowUps.AsNoTracking();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId = _userManager.GetUserId(User);
                query = query.Where(x => x.AssignedTo == userId);
            }

            var followUps = await query
                .OrderBy(x => x.FollowUpDate)
                .Select(x => new ApiFollowUpDto
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    LeadId = x.LeadId,
                    OpportunityId = x.OpportunityId,
                    FollowUpDate = x.FollowUpDate,
                    Status = x.Status,
                    Type = x.Type,
                    AssignedTo = x.AssignedTo,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync();

            return Ok(followUps);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var followUp = await _context.FollowUps
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (followUp == null)
                return NotFound(new { message = "Follow-up not found." });

            if (User.IsInRole("SalesExecutive") &&
                followUp.AssignedTo != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return Ok(ToDto(followUp));
        }

        [HttpPost]
        public async Task<IActionResult> Create(ApiCreateFollowUpRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var validStatuses = new[]
            {
                "Planned", "Completed", "Missed", "Cancelled"
            };

            var validTypes = new[]
            {
                "Call", "Meeting", "Email", "Task"
            };

            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { message = "Invalid follow-up status." });

            if (!validTypes.Contains(request.Type))
                return BadRequest(new { message = "Invalid follow-up type." });

            if ((request.Status == "Planned" ||
                 request.Status == "New") &&
                request.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(new
                {
                    message = "New or planned follow-up cannot be scheduled in the past."
                });
            }

            var customer = await _context.Customers.FindAsync(request.CustomerId);

            if (customer == null)
                return BadRequest(new { message = "Customer not found." });

            if (User.IsInRole("SalesExecutive") &&
                customer.AssignedTo != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            if (request.LeadId.HasValue)
            {
                var lead = await _context.Leads.FindAsync(request.LeadId.Value);

                if (lead == null)
                    return BadRequest(new { message = "Lead not found." });

                if (User.IsInRole("SalesExecutive") &&
                    lead.AssignedTo != _userManager.GetUserId(User))
                {
                    return Forbid();
                }
            }

            if (request.OpportunityId.HasValue)
            {
                var opportunity =
                    await _context.Opportunities.FindAsync(request.OpportunityId.Value);

                if (opportunity == null)
                    return BadRequest(new { message = "Opportunity not found." });

                if (User.IsInRole("SalesExecutive") &&
                    opportunity.AssignedTo != _userManager.GetUserId(User))
                {
                    return Forbid();
                }
            }

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

            var followUp = new FollowUp
            {
                CustomerId = request.CustomerId,
                LeadId = request.LeadId,
                OpportunityId = request.OpportunityId,
                FollowUpDate = request.FollowUpDate,
                Status = request.Status,
                Type = request.Type,
                AssignedTo = assignedTo,
                Notes = request.Notes,
                CreatedDate = DateTime.UtcNow
            };

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(Get),
                new { id = followUp.Id },
                ToDto(followUp));
        }

        private static ApiFollowUpDto ToDto(FollowUp x)
        {
            return new ApiFollowUpDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                LeadId = x.LeadId,
                OpportunityId = x.OpportunityId,
                FollowUpDate = x.FollowUpDate,
                Status = x.Status,
                Type = x.Type,
                AssignedTo = x.AssignedTo,
                CreatedDate = x.CreatedDate
            };
        }
    }
}
