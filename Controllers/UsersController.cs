using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private static readonly string[] ValidRoles =
        {
            "Admin",
            "Manager",
            "SalesExecutive"
        };

        public UsersController(
            UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Users
        public async Task<IActionResult> Index(string? search)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                users = users.Where(u =>
                    u.Email != null &&
                    u.Email.Contains(search));
            }

            var result = new System.Collections.Generic.List<UserManagementViewModel>();

            foreach (var user in users.OrderBy(u => u.Email))
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? "",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    IsActive = IsUserActive(user)
                });
            }

            ViewBag.Search = search;

            return View(result);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            LoadRoles();
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateUserViewModel model)
        {
            if (!ValidRoles.Contains(model.Role))
            {
                ModelState.AddModelError(
                    "Role",
                    "Invalid role selected.");
            }

            if (!ModelState.IsValid)
            {
                LoadRoles(model.Role);
                return View(model);
            }

            var existingUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "A user with this email already exists.");

                LoadRoles(model.Role);
                return View(model);
            }

            var temporaryPassword =
                GenerateTemporaryPassword();

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var result = await _userManager.CreateAsync(
                user,
                temporaryPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                LoadRoles(model.Role);
                return View(model);
            }

            await _userManager.AddToRoleAsync(
                user,
                model.Role);

            TempData["CreatedUser"] = model.Email;
            TempData["TemporaryPassword"] = temporaryPassword;

            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "",
                Role = roles.FirstOrDefault() ?? "SalesExecutive",
                IsActive = IsUserActive(user)
            };

            LoadRoles(model.Role);

            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ValidRoles.Contains(model.Role))
            {
                ModelState.AddModelError(
                    "Role",
                    "Invalid role selected.");
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Prevent the currently logged-in Admin
            // from removing their own Admin role.
            if (user.Id == _userManager.GetUserId(User) &&
                model.Role != "Admin")
            {
                ModelState.AddModelError(
                    "Role",
                    "You cannot remove your own Admin role.");
            }

            if (!ModelState.IsValid)
            {
                LoadRoles(model.Role);
                return View(model);
            }

            var duplicateUser =
                _userManager.Users.FirstOrDefault(u =>
                    u.Email != null &&
                    u.Email.ToLower() ==
                    model.Email.ToLower() &&
                    u.Id != id);

            if (duplicateUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "A user with this email already exists.");

                LoadRoles(model.Role);
                return View(model);
            }

            user.Email = model.Email;
            user.UserName = model.Email;
            user.EmailConfirmed = true;

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                LoadRoles(model.Role);
                return View(model);
            }

            var existingRoles =
                await _userManager.GetRolesAsync(user);

            if (existingRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(
                    user,
                    existingRoles);
            }

            await _userManager.AddToRoleAsync(
                user,
                model.Role);

            if (model.IsActive)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = null;
            }
            else
            {
                user.LockoutEnabled = true;
                user.LockoutEnd =
                    DateTimeOffset.UtcNow.AddYears(100);
            }

            await _userManager.UpdateAsync(user);

            TempData["Success"] =
                "User updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Do not deactivate yourself.
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] =
                    "You cannot deactivate your own account.";

                return RedirectToAction(nameof(Index));
            }

            user.LockoutEnabled = true;

            if (IsUserActive(user))
            {
                user.LockoutEnd =
                    DateTimeOffset.UtcNow.AddYears(100);
            }
            else
            {
                user.LockoutEnd = null;
            }

            await _userManager.UpdateAsync(user);

            TempData["Success"] =
                IsUserActive(user)
                    ? "User activated successfully."
                    : "User deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private void LoadRoles(string? selectedRole = null)
        {
            ViewBag.Roles = new SelectList(
                ValidRoles,
                selectedRole);
        }

        private static bool IsUserActive(
            IdentityUser user)
        {
            return !user.LockoutEnd.HasValue ||
                   user.LockoutEnd <= DateTimeOffset.UtcNow;
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";

            var chars = new char[12];

            chars[0] =
                upper[RandomNumberGenerator.GetInt32(upper.Length)];

            chars[1] =
                lower[RandomNumberGenerator.GetInt32(lower.Length)];

            chars[2] =
                digits[RandomNumberGenerator.GetInt32(digits.Length)];

            const string all =
                upper + lower + digits;

            for (int i = 3; i < chars.Length; i++)
            {
                chars[i] =
                    all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            return new string(chars);
        }
    }
}
