using System.Diagnostics;
using System.Security.Claims;

using BeautySalon.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautySalon.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            if (await _userManager.IsInRoleAsync(currentUser, "Admin") || await _userManager.IsInRoleAsync(currentUser, "Manager"))
            {
                var allUsers = await _userManager.Users
                    .Where(u => u.Id != currentUser.Id)
                    .ToListAsync();

                var nonManagerUsers = new List<IdentityUser>();
                foreach (var user in allUsers)
                {
                    if (!await _userManager.IsInRoleAsync(user, "Manager"))
                    {
                        nonManagerUsers.Add(user);
                    }
                }

                return View(nonManagerUsers);
            }

            return View(new List<IdentityUser>());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

