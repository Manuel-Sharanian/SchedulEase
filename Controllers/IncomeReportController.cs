using System.ComponentModel;
using System.Drawing.Printing;
using System.Drawing;
using BeautySalon.Data;
using BeautySalon.Models;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Policy;
using System.Xml.Linq;
using SendGrid.Helpers.Mail;

namespace BeautySalon.Controllers
{
    public class IncomeReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IncomeReportController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            DateTime start = startDate ?? DateTime.Today;
            DateTime end = endDate ?? start;

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var isManager = await _userManager.IsInRoleAsync(currentUser, "Manager");

            var reportData = await GetReportDataAsync(start, end);
            var userReports = new List<UserReportViewModel>();

            if (isAdmin || isManager)
            {
                var users = new List<IdentityUser>();

                if (isAdmin)
                {
                    users = (await _userManager.GetUsersInRoleAsync("User"))
                        .Concat(await _userManager.GetUsersInRoleAsync("Admin"))
                        .ToList();
                }
                else if (isManager)
                {
                    users = (await _userManager.GetUsersInRoleAsync("User"))
                        .Concat(await _userManager.GetUsersInRoleAsync("Admin"))
                        .Where(u => u.Id != currentUser.Id)  // Բացառում ենք ընթացիկ Manager-ին
                        .ToList();
                }

                foreach (var user in users)
                {
                    var userReport = await GetUserReportDataAsync(user.Id, start, end);
                    userReports.Add(userReport);
                }
            }

            var viewModel = new IncomeReportViewModel
            {
                StartDate = start,
                EndDate = end,
                EmployeeAmount = reportData.EmployeeAmount,
                EmployerAmount = reportData.EmployerAmount,
                TotalAmount = reportData.TotalAmount,
                CompletedAppointments = reportData.CompletedAppointments,
                UserReports = userReports,
                IsAdmin = isAdmin,
                IsManager = isManager
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetReportData(DateTime startDate, DateTime endDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var isManager = await _userManager.IsInRoleAsync(currentUser, "Manager");

            var reportData = await GetReportDataAsync(startDate, endDate);
            var userReports = new List<UserReportViewModel>();

            if (isAdmin || isManager)
            {
                var users = new List<IdentityUser>();

                if (isAdmin)
                {
                    users = (await _userManager.GetUsersInRoleAsync("User"))
                        .Concat(await _userManager.GetUsersInRoleAsync("Admin"))
                        .ToList();
                }
                else if (isManager)
                {
                    users = (await _userManager.GetUsersInRoleAsync("User"))
                        .Concat(await _userManager.GetUsersInRoleAsync("Admin"))
                        .Where(u => u.Id != currentUser.Id)  // Բացառում ենք ընթացիկ Manager-ին
                        .ToList();
                }

                foreach (var user in users)
                {
                    var userReport = await GetUserReportDataAsync(user.Id, startDate, endDate);
                    userReports.Add(userReport);
                }
            }

            var viewModel = new IncomeReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                EmployeeAmount = reportData.EmployeeAmount,
                EmployerAmount = reportData.EmployerAmount,
                TotalAmount = reportData.TotalAmount,
                CompletedAppointments = reportData.CompletedAppointments,
                UserReports = userReports,
                IsAdmin = isAdmin,
                IsManager = isManager
            };

            return Json(viewModel);
        }

        private async Task<IncomeReportViewModel> GetReportDataAsync(DateTime startDate, DateTime endDate)
        {
            var reports = await _context.IncomeReports
                .Where(r => r.Date.Date >= startDate.Date && r.Date.Date <= endDate.Date)
                .ToListAsync();

            return new IncomeReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                EmployeeAmount = reports.Sum(r => r.TotalEmployeeAmount),
                EmployerAmount = reports.Sum(r => r.TotalEmployerAmount),
                CompletedAppointments = reports.Sum(r => r.TotalCompletedAppointments),
                TotalAmount = reports.Sum(r => r.TotalEmployeeAmount + r.TotalEmployerAmount)
            };
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetUserReportData(string userId, DateTime startDate, DateTime endDate)
        {
            var userReport = await GetUserReportDataAsync(userId, startDate, endDate);
            return Json(userReport);
        }

        private async Task<UserReportViewModel> GetUserReportDataAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var userReports = await _context.IncomeReports
                .Where(r => r.UserReports.Any(ur => ur.UserId == userId) && r.Date >= startDate && r.Date <= endDate)
                .SelectMany(r => r.UserReports.Where(ur => ur.UserId == userId))
                .ToListAsync();

            var user = await _userManager.FindByIdAsync(userId);

            return new UserReportViewModel
            {
                Id = userId,
                StartDate = startDate,
                EndDate = endDate,
                UserName = user?.UserName,
                EmployeeAmount = userReports.Sum(r => r.EmployeeAmount),
                EmployerAmount = userReports.Sum(r => r.EmployerAmount),
                CompletedAppointments = userReports.Sum(r => r.CompletedAppointments),
                TotalAmount = userReports.Sum(r => r.EmployeeAmount + r.EmployerAmount)
            };
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> SaveIncomeReport([FromBody] IncomeReportViewModel model)
        {
            var existingReport = await _context.IncomeReports
                .Include(r => r.UserReports)
                .FirstOrDefaultAsync(r => r.Date.Date == model.Date.Date);

            if (existingReport == null)
            {
                existingReport = new IncomeReport
                {
                    Date = model.Date,
                    TotalEmployeeAmount = model.EmployeeAmount,
                    TotalEmployerAmount = model.EmployerAmount,
                    TotalCompletedAppointments = model.CompletedAppointments,
                    UserReports = new List<UserIncomeReport>
            {
                new UserIncomeReport
                {
                    UserId = model.UserId,
                    EmployeeAmount = model.EmployeeAmount,
                    EmployerAmount = model.EmployerAmount,
                    CompletedAppointments = model.CompletedAppointments
                }
            }
                };
                _context.IncomeReports.Add(existingReport);
            }
            else
            {
                var userReport = existingReport.UserReports.FirstOrDefault(ur => ur.UserId == model.UserId);
                if (userReport == null)
                {
                    userReport = new UserIncomeReport
                    {
                        UserId = model.UserId,
                        EmployeeAmount = model.EmployeeAmount,
                        EmployerAmount = model.EmployerAmount,
                        CompletedAppointments = model.CompletedAppointments
                    };
                    existingReport.UserReports.Add(userReport);

                    existingReport.TotalEmployeeAmount += model.EmployeeAmount;
                    existingReport.TotalEmployerAmount += model.EmployerAmount;
                    existingReport.TotalCompletedAppointments += model.CompletedAppointments;
                }
                else
                {
                    existingReport.TotalEmployeeAmount -= userReport.EmployeeAmount;
                    existingReport.TotalEmployerAmount -= userReport.EmployerAmount;
                    existingReport.TotalCompletedAppointments -= userReport.CompletedAppointments;

                    userReport.EmployeeAmount = model.EmployeeAmount;
                    userReport.EmployerAmount = model.EmployerAmount;
                    userReport.CompletedAppointments = model.CompletedAppointments;

                    existingReport.TotalEmployeeAmount += model.EmployeeAmount;
                    existingReport.TotalEmployerAmount += model.EmployerAmount;
                    existingReport.TotalCompletedAppointments += model.CompletedAppointments;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                TotalEmployeeAmount = existingReport.TotalEmployeeAmount,
                TotalEmployerAmount = existingReport.TotalEmployerAmount,
                TotalCompletedAppointments = existingReport.TotalCompletedAppointments
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CheckUserSavedReport(string userId, DateTime date)
        {
            var savedReport = await _context.UserSavedReports
                .AnyAsync(r => r.UserId == userId && r.Date.Date == date.Date);

            return Json(new { saved = savedReport });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> MarkUserReportSaved(string userId, DateTime date)
        {
            var userSavedReport = new UserSavedReport
            {
                UserId = userId,
                Date = date
            };
            _context.UserSavedReports.Add(userSavedReport);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}