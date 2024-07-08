using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautySalon.Models
{
    public class Client
    {
        public int Id { get; set; } // Նշանակման հերթական համարը

        [Required]
        [Display(Name = "Անուն")]
        public string? FirstName { get; set; }


        [Display(Name = "Ազգանուն")]
        public string? LastName { get; set; }


        [Display(Name = "Ծննդյան ամսաթիվ")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [Display(Name = "Հեռախոսահամար")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "The phone number must be 9 digits long.")]
        public string? PhoneNumber { get; set; }


        [Display(Name = "Առաջին գրանցման ամսաթիվ")]
        [DataType(DataType.Date)]
        public DateTime? FirstAppointmentDate { get; set; }

        public ICollection<Appointment>? Appointments { get; set; }

    }
}
