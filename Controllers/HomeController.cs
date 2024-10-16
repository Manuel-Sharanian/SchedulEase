using System.Diagnostics;
using BeautySalon.Data;
using BeautySalon.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class HomeController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _context;

    public HomeController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index(DateTime? selectedDate)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        DateTime date = selectedDate ?? DateTime.Today;

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

            // 1. Ագրեգացիա տվյալների բազայում գրանցումների համար
            var userStats = await _context.Appointments
                .Where(a => a.AppointmentDate.Date == date.Date)
                .GroupBy(a => a.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    CompletedAppointmentsCount = g.Count(a => a.IsCompleted)
                })
                .ToListAsync();

            // 2. Բատչ հարցում եկամուտների հաշվետվությունների համար
            var incomeReports = await _context.IncomeReports
                .Where(r => r.Date.Date == date.Date)
                .SelectMany(r => r.UserReports)
                .Select(ur => new
                {
                    ur.UserId,
                    HasIncomeReport = ur.EmployeeAmount > 0 || ur.EmployerAmount > 0
                })
                .ToListAsync();

            // 3. Արդյունքների միավորում հիշողության մեջ
            var userDashboards = nonManagerUsers
                .Select(user => new UserDashboardViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    CompletedAppointmentsCount = userStats.FirstOrDefault(s => s.UserId == user.Id)?.CompletedAppointmentsCount ?? 0,
                    HasIncomeReport = incomeReports.Any(r => r.UserId == user.Id && r.HasIncomeReport)
                })
                .ToList();

            ViewBag.SelectedDate = date;
            return View(userDashboards);
        }

        return View(new List<UserDashboardViewModel>());
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

