using System.Security.Claims;

namespace CLAIMS_Application.Part2.Models
{
    public class HRDashboardViewModel
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public int TotalUsers { get; set; }
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public List<User> Users { get; set; } = new List<User>();
        public List<MonthlyClaim> Claims { get; set; } = new List<MonthlyClaim>();
    }
}