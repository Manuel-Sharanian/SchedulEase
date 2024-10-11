using System.ComponentModel.DataAnnotations;

namespace BeautySalon.Models
{
    public class UserReportViewModel
    {
        public string Id { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        public string UserName { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal EmployeeAmount { get; set; }
        [Required]
        [Range(0, double.MaxValue)]
        public decimal EmployerAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int CompletedAppointments { get; set; }
    }
}
