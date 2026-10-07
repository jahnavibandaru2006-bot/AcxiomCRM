using AcxiomCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class ApiAuthController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuthAuditService _auditService;

        public ApiAuthController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            AuthAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Email and password are required."
                });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                await _auditService.LogAsync(
                    "LoginFailed",
                    null,
                    request.Email);

                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                request.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                await _auditService.LogAsync(
                    "LoginFailed",
                    user.Id,
                    user.Email);

                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var roles = await _userManager.GetRolesAsync(user);

            await _auditService.LogAsync(
                "LoginSuccess",
                user.Id,
                user.Email);

            return Ok(new
            {
                message = "Login successful.",
                userId = user.Id,
                email = user.Email,
                roles
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId =
                _userManager.GetUserId(User);

            var email =
                User.Identity?.Name;

            await _auditService.LogAsync(
                "Logout",
                userId,
                email);

            await _signInManager.SignOutAsync();

            return Ok(new
            {
                message = "Logout successful."
            });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
