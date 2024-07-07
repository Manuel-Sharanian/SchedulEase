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
                UserId = a.UserId
            }).ToList();

            var completedAppointments = appointments.Where(a => a.IsCompleted);
            var totalCompletedAmount = completedAppointments.Sum(a => a.CustomPrice != 0 ? a.CustomPrice : a.Service.Price);
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
            ViewBag.SelectedDate = date;
            ViewBag.TotalCompletedAmount = totalCompletedAmount;
            ViewBag.CompletedCount = completedCount;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.TargetUserId = targetUserId;

            return View(viewModels);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserAppointments(string userId, DateTime? selectedDate)
        {
            DateTime date = selectedDate ?? DateTime.Today;

            var appointments = await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Service)
                .Where(a => a.UserId == userId && a.AppointmentDate.Date == date.Date)
                .ToListAsync();

            var viewModels = appointments.Select(a => new AppointmentViewModel
            {
                Appointment = a,
                Service = a.Service,
                UserId = a.UserId
            }).ToList();

            var completedAppointments = appointments.Where(a => a.IsCompleted);
            var totalCompletedAmount = completedAppointments.Sum(a => a.CustomPrice != 0 ? a.CustomPrice : a.Service.Price);
            var completedCount = completedAppointments.Count();

            ViewBag.SelectedDate = date;
            ViewBag.TotalCompletedAmount = totalCompletedAmount;
            ViewBag.CompletedCount = completedCount;
            ViewBag.IsAdmin = true;
            ViewBag.UserId = userId;

            return View("Index", viewModels);
        }

        [HttpPost]
        [Route("Appointments/MarkCompleted/{id}")]
        public IActionResult MarkCompleted(int id)
        {
            var appointment = _context.Appointments.Find(id);
            if (appointment != null)
            {
                appointment.IsCompleted = true;
                _context.SaveChanges();
                return Ok();
            }
            return NotFound();
        }

        public List<SelectListItem> GetHourOptions()
        {
            var hourOptions = new List<SelectListItem>();

            // Add options for each hour from 06:00 to 20:00 in the 24-hour format with 30-minute increments
            for (int hour = 8; hour < 20; hour++)
            {
                hourOptions.Add(new SelectListItem { Text = $"{hour:00}:00", Value = $"{hour:00}:00" });
                hourOptions.Add(new SelectListItem { Text = $"{hour:00}:30", Value = $"{hour:00}:30" });
            }
            return hourOptions;
        }

        private async Task<bool> IsTimeSlotAvailableForUser(DateTime date, TimeSpan startTime, int duration, string userId)
        {
            var endTime = startTime.Add(TimeSpan.FromMinutes(duration));
            var conflictingAppointments = await _context.Appointments
                .Where(a => a.AppointmentDate.Date == date.Date && a.UserId == userId)
                .ToListAsync();

            return !conflictingAppointments.Any(a =>
                (a.AppointmentHour >= startTime && a.AppointmentHour < endTime) ||
                (a.AppointmentHour.Add(TimeSpan.FromMinutes(a.Duration)) > startTime && a.AppointmentHour < endTime));
        }

        // GET: Appointments/Create
        public IActionResult Create(DateTime? date, string time, string userId)
        {
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName");
            ViewBag.HourOptions = GetHourOptions();
            ViewBag.Date = date;
            ViewBag.Time = time;
            ViewBag.UserId = userId; // Pass userId to the view
            return View();
        }


        // POST: Appointments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,PhoneNumber,AppointmentDate,AppointmentHour,Duration,ClientId,ServiceId,UserId")] Appointment appointment)
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
                        return View(appointment);
                    }
                    appointment.IsCreatedByAdmin = true;
                }
                else
                {
                    appointment.UserId = currentUser.Id;
                    appointment.IsCreatedByAdmin = false;
                }


                // Check if the time slot is available for the target user
                bool isAvailable = await IsTimeSlotAvailableForUser(appointment.AppointmentDate, appointment.AppointmentHour, appointment.Duration, appointment.UserId);
                if (!isAvailable)
                {
                    ModelState.AddModelError("", "The selected time slot is already occupied. Please choose another time.");
                    ViewBag.HourOptions = GetHourOptions();
                    ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", appointment.ServiceId);
                    return View(appointment);
                }

                // Check if the client already exists
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

                // Ensure the selected service exists
                var service = await _context.Services.FindAsync(appointment.ServiceId);
                if (service == null)
                {
                    ModelState.AddModelError("ServiceId", "The selected service does not exist.");
                    ViewBag.HourOptions = GetHourOptions();
                    ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", appointment.ServiceId);
                    return View(appointment);
                }

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { userId = appointment.UserId, selectedDate = appointment.AppointmentDate.ToString("yyyy-MM-dd") });
            }

            ViewBag.HourOptions = GetHourOptions();
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", appointment.ServiceId);
            return View(appointment);
        }


        // GET: Appointments/Edit/5
        public IActionResult Edit(int id, string returnUrl)
        {
            var appointment = _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefault(a => a.Id == id);
            ViewBag.HourOptions = GetHourOptions();
            ViewBag.Time = appointment.AppointmentHour.ToString(@"hh\:mm");
            ViewBag.ReturnUrl = returnUrl;
            if (appointment == null)
            {
                return NotFound();
            }

            var viewModel = new AppointmentEditViewModel
            {
                Id = appointment.Id,
                FirstName = appointment.FirstName,
                PhoneNumber = appointment.PhoneNumber,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentHour = appointment.AppointmentHour,
                ServiceId = appointment.ServiceId,
                CustomPrice = appointment.CustomPrice,
                Services = _context.Services.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.ServiceName,
                    Selected = s.Id == appointment.ServiceId
                }).ToList()
            };
            return View(viewModel);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(AppointmentEditViewModel viewModel, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var appointment = _context.Appointments
                    .Include(a => a.Service)
                    .FirstOrDefault(a => a.Id == viewModel.Id);

                if (appointment == null)
                {
                    return NotFound();
                }

                appointment.FirstName = viewModel.FirstName;
                appointment.PhoneNumber = viewModel.PhoneNumber;
                appointment.AppointmentDate = viewModel.AppointmentDate;
                appointment.AppointmentHour = viewModel.AppointmentHour;
                appointment.ServiceId = viewModel.ServiceId;
                appointment.CustomPrice = viewModel.CustomPrice;

                // Թարմացնում ենք Service-ը
                var newService = _context.Services.Find(viewModel.ServiceId);
                if (newService != null)
                {
                    appointment.Service = newService;
                }

                _context.Update(appointment);
                _context.SaveChanges();

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Index), new { userId = appointment.UserId, selectedDate = appointment.AppointmentDate.ToString("yyyy-MM-dd") });
            }

            // Եթե ModelState-ը վավեր չէ
            viewModel.Services = _context.Services.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.ServiceName,
                Selected = s.Id == viewModel.ServiceId
            }).ToList();

            // Ավելացնում ենք HourOptions-ը
            ViewBag.HourOptions = GetHourOptions();
            ViewBag.ReturnUrl = returnUrl;
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

    }
}