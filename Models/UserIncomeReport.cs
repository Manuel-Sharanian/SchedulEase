using System.ComponentModel.DataAnnotations.Schema;

using BeautySalon.Models;
using Microsoft.EntityFrameworkCore;

namespace BeautySalon.Models
{
    public class UserIncomeReport
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EmployeeAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EmployerAmount { get; set; }
        public int IncomeReportId { get; set; }
        public int CompletedAppointments { get; set; }
        public IncomeReport IncomeReport { get; set; }
    }
}