using BeautySalon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var userViewModels = new List<UserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserViewModel
            {
                Id = user.Id,
                FullName = user.UserName,
                Roles = roles.ToList()
            });
        }

        return View(userViewModels);
    }


    [HttpPost]
    public async Task<IActionResult> ChangeRole(string userId, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var currentUserRoles = await _userManager.GetRolesAsync(user);

        // Check whether is this semi-final Admin
        if (currentUserRoles.Contains("Admin") && newRole != "Admin")
        {
            var adminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
            if (adminCount <= 2)
            {
                TempData["Error"] = "Չի կարելի հեռացնել նախավերջին Admin-ին";
                return RedirectToAction(nameof(Index));
            }
        }

        await _userManager.RemoveFromRolesAsync(user, currentUserRoles);
        await _userManager.AddToRoleAsync(user, newRole);

        return RedirectToAction(nameof(Index));
    }

}