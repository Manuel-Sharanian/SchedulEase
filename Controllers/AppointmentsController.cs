using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BeautySalon.Data;
using BeautySalon.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;


namespace BeautySalon.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AppointmentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Appointments
        public async Task<IActionResult> Index(string userId, DateTime? selectedDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Եթե Admin է և userId տրված է, օգտագործում ենք այդ userId-ն
            // Հակառակ դեպքում օգտագործում ենք ընթացիկ օգտատիրոջ Id-ն
            var targetUserId = isAdmin && !string.IsNullOrEmpty(userId) ? userId : currentUser.Id;

            // Ստուգում ենք՝ արդյոք դիտվում են սեփական գրանցումները
            var isViewingOwnAppointments = targetUserId == currentUser.Id;

            DateTime date = selectedDate ?? DateTime.Today;
            var appointments = await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Service)
                .Where(a => a.AppointmentDate.Date == date.Date && a.UserId == targetUserId)
                .ToListAsync();

            var viewModels = appointments.Select(a => new AppointmentViewModel
            {
                Appointment = a,
                Service = a.Service,
                UserId = a.UserId,
                EffectivePrice = a.CustomPrice.HasValue ? (a.CustomPrice.Value == 0 ? 0 : a.CustomPrice.Value) : a.Service?.Price ?? 0
            }).ToList();

            var completedAppointments = appointments.Where(a => a.IsCompleted);
            var totalCompletedAmount = completedAppointments.Sum(a =>
                a.CustomPrice.HasValue ? (a.CustomPrice.Value == 0 ? 0 : a.CustomPrice.Value) : a.Service?.Price ?? 0);
            var completedCount = completedAppointments.Count();
            // Appointment Index էջում UserName-ի համար
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    ViewBag.UserName = user.UserName;
                }
                else
                {
                    ViewBag.UserName = "Սեփական գրանցումներ";
                }
            }
            // User-ների էջում իրանց եկամտի մասին
            var incomeReport = await _context.IncomeReports
           .Include(r => r.UserReports)
           .FirstOrDefaultAsync(r => r.Date.Date == date.Date);

            if (incomeReport != null)
            {
                var userReport = incomeReport.UserReports.FirstOrDefault(ur => ur.UserId == targetUserId);
                if (userReport != null)
                {
                    ViewBag.EmployeeAmount = userReport.EmployeeAmount;
                    ViewBag.EmployerAmount = userReport.EmployerAmount;
                }
            }


            ViewBag.SelectedDate = date;
            ViewBag.TotalCompletedAmount = totalCompletedAmount;
            ViewBag.CompletedCount = completedCount;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.TargetUserId = targetUserId;
            ViewBag.IsViewingOwnAppointments = isViewingOwnAppointments;

            return View(viewModels);
        }

        [HttpPost]
        [Route("Appointments/MarkCompleted/{id}")]
        public async Task<IActionResult> MarkCompleted(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.IsCompleted = true;
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> CheckZeroValueAppointments(DateTime date, string userId)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Service)
                .Where(a => a.AppointmentDate.Date == date.Date && a.UserId == userId)
                .ToListAsync();

            bool hasZeroValueAppointments = appointments.Any(a =>
                (a.CustomPrice.HasValue && a.CustomPrice.Value == 0) &&
                (a.Service == null || a.Service.Price == 0));

            return Json(new { hasZeroValueAppointments });
        }

        public List<SelectListItem> GetHourOptions()
        {
            var hourOptions = new List<SelectListItem>();

            // Add options for each hour from 06:00 to 20:00 in the 24-hour format with 30-minute increments
            for (int hour = 6; hour < 20; hour++)
            {
                hourOptions.Add(new SelectListItem { Text = $"{hour:00}:00", Value = $"{hour:00}:00" });
                hourOptions.Add(new SelectListItem { Text = $"{hour:00}:30", Value = $"{hour:00}:30" });
            }
            return hourOptions;
        }

        private async Task<bool> IsTimeSlotAvailableForUser(DateTime date, TimeSpan startTime, int duration, string userId, int? currentAppointmentId = null)
        {
            var endTime = startTime.Add(TimeSpan.FromMinutes(duration));
            var startMinutes = (int)startTime.TotalMinutes;
            var endMinutes = (int)endTime.TotalMinutes;

            var conflictingAppointments = await _context.Appointments
                .Where(a => a.UserId == userId &&
                            a.AppointmentDate.Date == date.Date &&
                            ((a.AppointmentHour.Hours * 60 + a.AppointmentHour.Minutes < endMinutes &&
                              a.AppointmentHour.Hours * 60 + a.AppointmentHour.Minutes + a.Duration > startMinutes) ||
                             (a.AppointmentHour.Hours * 60 + a.AppointmentHour.Minutes == startMinutes && a.Duration == duration)) &&
                            (currentAppointmentId == null || a.Id != currentAppointmentId))
                .AnyAsync();

            return !conflictingAppointments;
        }

        // GET: Appointments/Create
        public async Task<IActionResult> Create(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var appointment = new Appointment();

            string targetUserId;
            if (isAdmin && !string.IsNullOrEmpty(userId))
            {
                targetUserId = userId;
                appointment.UserId = userId;
                appointment.IsCreatedByAdmin = true;
            }
            else
            {
                targetUserId = currentUser.Id;
                appointment.UserId = currentUser.Id;
                appointment.IsCreatedByAdmin = false;
            }
            // Ստանում ենք թիրախային օգտատիրոջ անունը
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            ViewBag.UserName = targetUser != null ? targetUser.UserName : "Անհայտ օգտատեր";

            ViewBag.HourOptions = GetHourOptions();
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName");
            ViewBag.UserId = appointment.UserId;
            ViewBag.IsCreatedByAdmin = appointment.IsCreatedByAdmin;
            return View(appointment);
        }


        // POST: Appointments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,PhoneNumber,AppointmentDate,AppointmentHour,Duration,ClientId,ServiceId,UserId,IsCreatedByAdmin,CustomServiceName")] Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Challenge();
                }

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                if (isAdmin && !string.IsNullOrEmpty(appointment.UserId))
                {
                    var targetUser = await _userManager.FindByIdAsync(appointment.UserId);
                    if (targetUser == null)
                    {
                        ModelState.AddModelError("UserId", "Selected user does not exist.");
                        ViewBag.HourOptions = GetHourOptions();
                        ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", appointment.ServiceId);
                        ViewBag.UserId = appointment.UserId;
                        ViewBag.IsCreatedByAdmin = appointment.IsCreatedByAdmin;
                        return View(appointment);
                    }
                    appointment.IsCreatedByAdmin = true;
                }
                else
                {
                    appointment.UserId = currentUser.Id;
                    appointment.IsCreatedByAdmin = false;
                }

                bool isAvailable = await IsTimeSlotAvailableForUser(appointment.AppointmentDate, appointment.AppointmentHour, appointment.Duration, appointment.UserId);
                if (!isAvailable)
                {
                    ModelState.AddModelError("", "Ընտրված ժամանակահատվածը զբաղված է. Խնդրում ենք ընտրել այլ ժամ.");
                    ViewBag.SelectedDate = appointment.AppointmentDate;
                    ViewBag.SelectedTime = appointment.AppointmentHour.ToString(@"hh\:mm");
                    ViewBag.SelectedServiceId = appointment.ServiceId;
                    ViewBag.HourOptions = GetHourOptions();
                    ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", appointment.ServiceId);
                    ViewBag.UserId = appointment.UserId;
                    ViewBag.IsCreatedByAdmin = appointment.IsCreatedByAdmin;
                    var user = await _userManager.FindByIdAsync(appointment.UserId);
                    ViewBag.UserName = user != null ? user.UserName : "Անհայտ օգտատեր";
                    return View(appointment);
                }

                // Check if the client already exists Ստուգում ենք արդյոք հաճախորդը գոյություն ունի բազայում թե ոչ
                //var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.PhoneNumber == appointment.PhoneNumber);
                //if (existingClient != null)
                //{
                //    appointment.Client = existingClient;
                //}

                //        // Check if the client already exists based on the phone number
                //        var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.PhoneNumber == appointment.PhoneNumber);
                //        if (existingClient == null)
                //        {
                //            // Create a new client if it doesn't exist
                //            //var client = new Client
                //            //{
                //            //    FirstName = appointment.FirstName,
                //            //    PhoneNumber = appointment.PhoneNumber
                //            //};
                //            //_context.Clients.Add(client);
                //            await _context.SaveChangesAsync();
                //        }
                //        else
                //        {
                //            appointment.Client = existingClient;
                //        }

                if (appointment.ServiceId.HasValue)
                {
                    var service = await _context.Services.FindAsync(appointment.ServiceId.Value);
                    if (service == null)
                    {
                        ModelState.AddModelError("ServiceId", "The selected service does not exist.");
                        ViewBag.HourOptions = GetHourOptions();
                        ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", appointment.ServiceId);
                        return View(appointment);
                    }
                    appointment.CustomPrice = service.Price;
                }
                else if (!string.IsNullOrEmpty(appointment.CustomServiceName))
                {
                    appointment.CustomPrice = 0;
                }
                else
                {
                    ModelState.AddModelError("", "Պետք է ընտրել ծառայություն կամ մուտքագրել նոր ծառայության անվանում.");
                    ViewBag.HourOptions = GetHourOptions();
                    ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", appointment.ServiceId);
                    ViewBag.UserId = appointment.UserId;
                    ViewBag.IsCreatedByAdmin = appointment.IsCreatedByAdmin;
                    var user = await _userManager.FindByIdAsync(appointment.UserId);
                    ViewBag.UserName = user != null ? user.UserName : "Անհայտ օգտատեր";
                    return View(appointment);
                }

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { userId = appointment.UserId, selectedDate = appointment.AppointmentDate.ToString("yyyy-MM-dd") });
            }

            ViewBag.HourOptions = GetHourOptions();
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", appointment.ServiceId);
            ViewBag.UserId = appointment.UserId;
            ViewBag.IsCreatedByAdmin = appointment.IsCreatedByAdmin;
            return View(appointment);
        }

        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int id, string returnUrl)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            var users = await _userManager.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName
                })
                .ToListAsync();

            var user = await _userManager.FindByIdAsync(appointment.UserId);
            var viewModel = new AppointmentEditViewModel
            {
                Id = appointment.Id,
                UserId = appointment.UserId,
                UserName = user?.UserName ?? "Անհայտ օգտատեր",
                FirstName = appointment.FirstName,
                PhoneNumber = appointment.PhoneNumber,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentHour = appointment.AppointmentHour,
                ServiceId = appointment.ServiceId,
                CustomServiceName = appointment.CustomServiceName,
                CustomPrice = appointment.CustomPrice ?? appointment.Service?.Price ?? 0,
                Duration = appointment.Duration,
                Services = await _context.Services.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.ServiceName,
                    Selected = s.Id == appointment.ServiceId
                }).ToListAsync(),
                Users = users,
                SelectedUserId = appointment.UserId
            };

            ViewBag.HourOptions = GetHourOptions();
            ViewBag.Time = appointment.AppointmentHour.ToString(@"hh\:mm");
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.UserName = (await _userManager.FindByIdAsync(appointment.UserId))?.UserName ?? "Անհայտ օգտատեր";
            ViewBag.IsAdmin = isAdmin;

            return View(viewModel);
        }
        private async Task<List<SelectListItem>> GetUserSelectList()
        {
            return await _userManager.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName
                })
                .ToListAsync();
        }

        [HttpGet]
        public IActionResult GetAllServices()
        {
            var services = _context.Services.Select(s => new { id = s.Id, serviceName = s.ServiceName, price = s.Price }).ToList();
            return Json(services);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAmount(int id, string amount)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(amount) || amount == "0")
            {
                appointment.CustomPrice = 0;
            }
            else if (decimal.TryParse(amount, out decimal parsedAmount))
            {
                appointment.CustomPrice = parsedAmount;
            }
            else
            {
                return BadRequest("Անվավեր գումար");
            }

            await _context.SaveChangesAsync();

            decimal effectivePrice = appointment.CustomPrice ?? 0;
            string formattedAmount = effectivePrice == 0 ?
                "0.00 ֏" :
                string.Format("{0:#,##0.00 ֏}", effectivePrice);

            return Json(new
            {
                formattedAmount = formattedAmount,
                isCustomPrice = appointment.CustomPrice.HasValue && appointment.CustomPrice.Value > 0
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateService(int appointmentId, int? serviceId, string serviceName)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null)
            {
                return Json(new { success = false, errorMessage = "Նշանակումը չի գտնվել" });
            }

            if (serviceId.HasValue)
            {
                var service = await _context.Services.FindAsync(serviceId.Value);
                if (service == null)
                {
                    return Json(new { success = false, errorMessage = "Ծառայությունը չի գտնվել" });
                }
                appointment.ServiceId = service.Id;
                appointment.CustomPrice = service.Price;
            }
            else
            {
                // Եթե serviceId-ն null է, նշանակում է մուտքագրվել է նոր ծառայություն
                appointment.ServiceId = null;
                appointment.CustomServiceName = serviceName;
                // Գինը թողնում ենք նույնը կամ սահմանում ենք 0, կախված ձեր պահանջներից
                appointment.CustomPrice = 0;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                formattedPrice = string.Format("{0:#,##0.00 ֏}", appointment.CustomPrice)
            });
        }



        // Edit GET POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AppointmentEditViewModel viewModel, string returnUrl)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (ModelState.IsValid)
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Service)
                    .FirstOrDefaultAsync(a => a.Id == viewModel.Id);

                if (appointment == null)
                {
                    return NotFound();
                }

                // Եթե ադմինիստրատոր է և փոխել է օգտատիրոջը
                if (isAdmin && viewModel.SelectedUserId != appointment.UserId)
                {
                    // Ստուգում ենք, արդյոք ընտրված ժամը հասանելի է նոր օգտատիրոջ համար
                    bool isAvailable = await IsTimeSlotAvailableForUser(viewModel.AppointmentDate, viewModel.AppointmentHour, viewModel.Duration, viewModel.SelectedUserId);
                    if (!isAvailable)
                    {
                        ModelState.AddModelError("", "Ընտրված ժամանակահատվածն արդեն զբաղված է ընտրված օգտատիրոջ համար: Խնդրում ենք ընտրել այլ ժամ:");
                        viewModel.Users = await GetUserSelectList();
                        ViewBag.HourOptions = GetHourOptions();
                        ViewBag.ReturnUrl = returnUrl;
                        ViewBag.IsAdmin = isAdmin;

                        viewModel.Services = await _context.Services.Select(s => new SelectListItem
                        {
                            Value = s.Id.ToString(),
                            Text = s.ServiceName,
                            Selected = s.Id == viewModel.ServiceId
                        }).ToListAsync();

                        return View(viewModel);
                    }
                    appointment.UserId = viewModel.SelectedUserId;
                }
                else
                {
                    // Ստուգում ենք, արդյոք ընտրված ժամը հասանելի է
                    bool isAvailable = await IsTimeSlotAvailableForUser(viewModel.AppointmentDate, viewModel.AppointmentHour, viewModel.Duration, appointment.UserId, appointment.Id);
                    if (!isAvailable)
                    {
                        ModelState.AddModelError("", "Ընտրված ժամանակահատվածն արդեն զբաղված է: Խնդրում ենք ընտրել այլ ժամ:");
                        viewModel.Users = await GetUserSelectList();
                        ViewBag.HourOptions = GetHourOptions();
                        ViewBag.ReturnUrl = returnUrl;
                        ViewBag.IsAdmin = isAdmin;

                        viewModel.Services = await _context.Services.Select(s => new SelectListItem
                        {
                            Value = s.Id.ToString(),
                            Text = s.ServiceName,
                            Selected = s.Id == viewModel.ServiceId
                        }).ToListAsync();

                        return View(viewModel);
                    }
                }

                // Update other properties
                appointment.FirstName = viewModel.FirstName;
                appointment.PhoneNumber = viewModel.PhoneNumber;
                appointment.AppointmentDate = viewModel.AppointmentDate;
                appointment.AppointmentHour = viewModel.AppointmentHour;

                if (!string.IsNullOrEmpty(viewModel.CustomServiceName))
                {
                    appointment.ServiceId = null;
                    appointment.CustomServiceName = viewModel.CustomServiceName;
                    appointment.CustomPrice = 0;
                }
                else
                {
                    appointment.ServiceId = viewModel.ServiceId;
                    appointment.CustomServiceName = null;
                    appointment.CustomPrice = viewModel.CustomPrice;
                }

                appointment.Duration = viewModel.Duration;

                _context.Update(appointment);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Index), new { userId = appointment.UserId, selectedDate = appointment.AppointmentDate.ToString("yyyy-MM-dd") });
            }

            // Եթե ModelState-ը վավեր չէ
            viewModel.Users = await GetUserSelectList();
            ViewBag.HourOptions = GetHourOptions();
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.IsAdmin = isAdmin;

            viewModel.Services = await _context.Services.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.ServiceName,
                Selected = s.Id == viewModel.ServiceId
            }).ToListAsync();

            return View(viewModel);
        }



        // GET: Appointments/Delete/5
        public async Task<IActionResult> Delete(int? id, string returnUrl)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string returnUrl)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index), new { userId = appointment.UserId, selectedDate = appointment.AppointmentDate.ToString("yyyy-MM-dd") });
        }

        public IActionResult GetServicePrice(int serviceId)
        {
            var service = _context.Services.Find(serviceId);
            if (service == null)
            {
                return NotFound();
            }
            return Json(service.Price);
        }


        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveIncomeReport([FromBody] IncomeReportViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
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
                            UserReports = new List<UserIncomeReport>
                    {
                        new UserIncomeReport
                        {
                            UserId = model.UserId,
                            EmployeeAmount = model.EmployeeAmount,
                            EmployerAmount = model.EmployerAmount
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
                                EmployerAmount = model.EmployerAmount
                            };
                            existingReport.UserReports.Add(userReport);
                        }
                        else
                        {
                            userReport.EmployeeAmount = model.EmployeeAmount;
                            userReport.EmployerAmount = model.EmployerAmount;
                        }
                        existingReport.TotalEmployeeAmount = existingReport.UserReports.Sum(ur => ur.EmployeeAmount);
                        existingReport.TotalEmployerAmount = existingReport.UserReports.Sum(ur => ur.EmployerAmount);
                    }

                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        Success = true,
                        TotalEmployeeAmount = existingReport.TotalEmployeeAmount,
                        TotalEmployerAmount = existingReport.TotalEmployerAmount
                    });
                }
                catch (Exception ex)
                {
                    return Json(new { Success = false, ErrorMessage = ex.Message });
                }
            }
            return Json(new { Success = false, ErrorMessage = "Invalid model state" });
        }


        [HttpGet]
        public async Task<IActionResult> GetIncomeDistribution(DateTime date, string userId)
        {
            var incomeReport = await _context.IncomeReports
                .Include(r => r.UserReports)
                .FirstOrDefaultAsync(r => r.Date.Date == date.Date);

            if (incomeReport != null)
            {
                var userReport = incomeReport.UserReports.FirstOrDefault(ur => ur.UserId == userId);
                if (userReport != null)
                {
                    var totalAmount = userReport.EmployeeAmount + userReport.EmployerAmount;
                    var employeePercentage = totalAmount > 0 ? (userReport.EmployeeAmount / totalAmount) * 100 : 0;
                    var employerPercentage = totalAmount > 0 ? (userReport.EmployerAmount / totalAmount) * 100 : 0;

                    return Json(new
                    {
                        employeeAmount = userReport.EmployeeAmount,
                        employerAmount = userReport.EmployerAmount,
                        employeePercentage = Math.Round(employeePercentage, 2),
                        employerPercentage = Math.Round(employerPercentage, 2)
                    });
                }
            }

            return Json(null);
        }
    }
}