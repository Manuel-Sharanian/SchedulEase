using System.ComponentModel.DataAnnotations.Schema;

namespace BeautySalon.Models
{
    public class IncomeReport
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalEmployeeAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalEmployerAmount { get; set; }
        public List<UserIncomeReport> UserReports { get; set; } = new List<UserIncomeReport>();
    }
}
