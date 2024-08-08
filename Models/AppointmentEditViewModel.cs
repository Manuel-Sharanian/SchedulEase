using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BeautySalon.Models
{
    public class AppointmentEditViewModel
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public List<SelectListItem>? Users { get; set; }
        public string? SelectedUserId { get; set; }
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Անունը պարտադիր է")]
        [Display(Name = "Անուն")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Հեռախոսահամարը պարտադիր է")]
        [Phone(ErrorMessage = "Անվավեր հեռախոսահամար")]
        [Display(Name = "Հեռախոսահամար")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Հեռախոսահամարը պետք է լինի 9 նիշ.")]
        public string? PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Գրանցման Օրը")]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Display(Name = "Գրանցման Ժամ")]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentHour { get; set; }
        public int? ServiceId { get; set; }
        public decimal? CustomPrice { get; set; }

        public string? CustomServiceName { get; set; }
        public decimal OriginalServicePrice { get; set; }
        public int Duration { get; set; }
        public List<SelectListItem>? Services { get; set; }
    }
}
