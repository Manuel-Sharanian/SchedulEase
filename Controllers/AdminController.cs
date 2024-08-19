using BeautySalon.Data;
using BeautySalon.Models;
using BeautySalon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailService _emailService;

    public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IEmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ApproveUser(string email, string fullName, string token)
    {
        var pendingRegistration = await _context.PendingRegistrations
            .FirstOrDefaultAsync(pr => pr.Email == email && pr.FullName == fullName);

        if (pendingRegistration == null)
        {
            return NotFound("Սպասվող գրանցում չի գտնվել:");
        }

        var user = new IdentityUser
        {
            UserName = pendingRegistration.Email,
            Email = pendingRegistration.Email,
            PasswordHash = pendingRegistration.PasswordHash
        };

        var result = await _userManager.CreateAsync(user);

        if (result.Succeeded)
        {
            // Ստուգում ենք արդյոք սա առաջին օգտատերն է
            var isFirstUser = !await _userManager.Users.AnyAsync(u => u.Id != user.Id);

            if (isFirstUser)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "User");
            }

            // Հեռացնում ենք pending registration-ը
            _context.PendingRegistrations.Remove(pendingRegistration);
            await _context.SaveChangesAsync();

            // Ուղարկում ենք էլ. փոստ օգտատիրոջը
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: HttpContext.Request.Scheme);

            await _emailService.SendEmailConfirmationAsync(pendingRegistration.Email, callbackUrl);

            return Content("Օգտատերը հաստատվել է և հաստատման էլ. նամակն ուղարկվել է:");
        }

        return Content($"Սխալ օգտատիրոջը հաստատելիս: {string.Join(", ", result.Errors.Select(e => e.Description))}");
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

        // Ստուգում ենք արդյոք սա վերջին Admin-ն է
        if (currentUserRoles.Contains("Admin") && newRole != "Admin")
        {
            var adminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
            if (adminCount <= 1)
            {
                TempData["Error"] = "Չի կարելի հեռացնել վերջին Admin-ին";
                return RedirectToAction(nameof(Index));
            }
        }

        await _userManager.RemoveFromRolesAsync(user, currentUserRoles);
        await _userManager.AddToRoleAsync(user, newRole);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            return RedirectToAction("Index");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return RedirectToAction("Index");
        }
    }
}