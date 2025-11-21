using Microsoft.EntityFrameworkCore;

namespace CLAIMS_Application.Part2.Models
{
    public class ClaimsContext : DbContext 
    {
        
        public DbSet<MonthlyClaim> MonthlyClaimsModels { get; set; }

        public ClaimsContext(DbContextOptions<ClaimsContext> options) : base(options)
        {

        }
    }
}   
