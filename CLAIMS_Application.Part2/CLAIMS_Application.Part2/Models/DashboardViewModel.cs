namespace CLAIMS_Application.Part2.Models
{
    public class DashboardViewModel
    {
        public string Username { get; set; }
        public string? Role { get; set; }
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public List<MonthlyClaim> Claims { get; set; } = new List<MonthlyClaim>();

        
        public DashboardViewModel()
        {
        }
        public DashboardViewModel(List<MonthlyClaim> claims)
        {
            Claims = claims;
        }
    }
}