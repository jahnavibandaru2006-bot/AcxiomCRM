using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Admin,Manager,SalesExecutive")]
    public class ApiReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ApiReportsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("pipeline")]
        public async Task<IActionResult> Pipeline()
        {
            var query = _context.Opportunities.AsNoTracking();

            if (User.IsInRole("SalesExecutive"))
            {
                var userId = _userManager.GetUserId(User);
                query = query.Where(x => x.AssignedTo == userId);
            }

            var opportunities = await query.ToListAsync();

            var result = new
            {
                totalOpportunities = opportunities.Count,
                openOpportunities = opportunities.Count(x => x.Status == "Open"),
                wonOpportunities = opportunities.Count(x => x.Status == "Won"),
                lostOpportunities = opportunities.Count(x => x.Status == "Lost"),
                totalPipelineValue = opportunities
                    .Where(x => x.Status == "Open")
                    .Sum(x => x.Amount),
                weightedPipelineValue = opportunities
                    .Where(x => x.Status == "Open")
                    .Sum(x => x.WeightedValue),
                byStage = opportunities
                    .GroupBy(x => x.Stage)
                    .Select(g => new
                    {
                        stage = g.Key,
                        count = g.Count(),
                        value = g.Sum(x => x.Amount),
                        weightedValue = g.Sum(x => x.WeightedValue)
                    })
                    .OrderByDescending(x => x.value)
                    .ToList()
            };

            return Ok(result);
        }
    }
}
