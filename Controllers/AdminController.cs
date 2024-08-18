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
    [AllowAnonymous] // Կամ [Authorize(Roles = "Admin")]
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
            await _userManager.AddToRoleAsync(user, "User");

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: HttpContext.Request.Scheme);

            await _emailService.SendEmailConfirmationAsync(pendingRegistration.Email, callbackUrl);

            // Տպում ենք callbackUrl-ը լոգերում կամ կոնսոլում դեբագի համար
            Console.WriteLine($"Generated callbackUrl: {callbackUrl}");

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