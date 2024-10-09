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

        public IncomeReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            DateTime start = startDate ?? DateTime.Today;
            DateTime end = endDate ?? start;
            var reportData = await GetReportDataAsync(start, end);
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            return View(reportData);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetReportData(DateTime startDate, DateTime endDate)
        {
            var reportData = await GetReportDataAsync(startDate, endDate);
            return Json(reportData);
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