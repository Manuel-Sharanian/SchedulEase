namespace BeautySalon.Models
{
    public class UserDashboardViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int CompletedAppointmentsCount { get; set; }
        public bool HasIncomeReport { get; set; }
    }
}
