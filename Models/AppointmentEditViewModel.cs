using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BeautySalon.Models
{
    public class AppointmentEditViewModel
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Գրանցման Օրը")]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Display(Name = "Գրանցման Ժամ")]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentHour { get; set; }

        public int ServiceId { get; set; }
        public decimal CustomPrice { get; set; }
        public int Duration { get; set; }
        public List<SelectListItem>? Services { get; set; }
    }
}
