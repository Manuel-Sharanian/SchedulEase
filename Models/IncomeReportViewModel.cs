using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautySalon.Models
{
    public class IncomeReportViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal EmployeeAmount { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal EmployerAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int CompletedAppointments { get; set; }
        public string UserId { get; set; }
    }
}