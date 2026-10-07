using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [ApiController]
    [Route("api/opportunities")]
    [Authorize(Roles = "Admin,Manager,SalesExecutive")]
    public class ApiOpportunitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ApiOpportunitiesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var query = _context.Opportunities.AsNoTracking();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId = _userManager.GetUserId(User);
                query = query.Where(x => x.AssignedTo == userId);
            }

            var opportunities = await query
                .OrderByDescending(x => x.Id)
                .Select(x => new ApiOpportunityDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    CustomerId = x.CustomerId,
                    LeadId = x.LeadId,
                    Amount = x.Amount,
                    Stage = x.Stage,
                    Probability = x.Probability,
                    ExpectedCloseDate = x.ExpectedCloseDate,
                    Status = x.Status,
                    AssignedTo = x.AssignedTo,
                    WeightedValue = x.WeightedValue,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync();

            return Ok(opportunities);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var opportunity = await _context.Opportunities
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (opportunity == null)
                return NotFound(new { message = "Opportunity not found." });

            if (User.IsInRole("SalesExecutive") &&
                opportunity.AssignedTo != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return Ok(ToDto(opportunity));
        }

        [HttpPost]
        public async Task<IActionResult> Create(ApiCreateOpportunityRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than 0." });

            if (request.Probability < 0 || request.Probability > 100)
                return BadRequest(new { message = "Probability must be between 0 and 100." });

            if (request.Status == "Open" &&
                request.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(new
                {
                    message = "Active opportunity Expected Close Date cannot be in the past."
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

            var opportunity = new Opportunity
            {
                Name = request.Name.Trim(),
                CustomerId = request.CustomerId,
                LeadId = request.LeadId,
                Amount = request.Amount,
                Stage = request.Stage,
                Probability = request.Probability,
                ExpectedCloseDate = request.ExpectedCloseDate,
                Status = request.Status,
                AssignedTo = assignedTo,
                Notes = request.Notes,
                CreatedDate = DateTime.UtcNow
            };

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(Get),
                new { id = opportunity.Id },
                ToDto(opportunity));
        }

        private static ApiOpportunityDto ToDto(Opportunity x)
        {
            return new ApiOpportunityDto
            {
                Id = x.Id,
                Name = x.Name,
                CustomerId = x.CustomerId,
                LeadId = x.LeadId,
                Amount = x.Amount,
                Stage = x.Stage,
                Probability = x.Probability,
                ExpectedCloseDate = x.ExpectedCloseDate,
                Status = x.Status,
                AssignedTo = x.AssignedTo,
                WeightedValue = x.WeightedValue,
                CreatedDate = x.CreatedDate
            };
        }
    }
}
