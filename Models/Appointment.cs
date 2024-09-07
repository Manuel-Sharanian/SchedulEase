using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Mvc.Rendering;

namespace BeautySalon.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        [Required]
        [Display(Name = "Գրանցման Օրը")]
        public DateTime AppointmentDate { get; set; } // Ensure this property exists

        [Required]
        [Display(Name = "Գրանցման Ժամ")]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentHour { get; set; }

        [Required(ErrorMessage = "Անունը պարտադիր է")]
        [Display(Name = "Անուն")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Հեռախոսահամարը պարտադիր է")]
        [Phone(ErrorMessage = "Անվավեր հեռախոսահամար")]
        [Display(Name = "Հեռախոսահամար")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Հեռախոսահամարը պետք է լինի 9 նիշ.")]
        public string? PhoneNumber { get; set; }
        public Client? Client { get; set; }

        // One-to-many relationship
        public int? ServiceId { get; set; }
        public Service? Service { get; set; }

        [Column(TypeName = "decimal(18,2)")]

        public decimal? CustomPrice { get; set; }

        public string? CustomServiceName { get; set; }
        public int Duration { get; set; } // Duration in minutes (e.g., 30, 60, 90)

        public bool IsCompleted { get; set; }

        public string? CreatedByUserId { get; set; }
        public string? CreatedByUsername { get; set; }
    }
}

