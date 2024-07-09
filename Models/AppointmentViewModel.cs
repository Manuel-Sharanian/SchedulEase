using BeautySalon.Data;
using BeautySalon.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BeautySalon.Models
{
    public class AppointmentViewModel
    {
        public Appointment? Appointment { get; set; }
        public Service? Service { get; set; }
        public string? UserId { get; set; }
    }
}