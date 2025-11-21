using Microsoft.AspNetCore.Mvc;
using CLAIMS_Application.Part2.Models;
using System.Security.Claims;

namespace CLAIMS_Application.Part2.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var allClaims = ClaimController.GetClaims();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userName = User.FindFirst(ClaimTypes.GivenName)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;

            // Lecturers only see their own claims
            List<MonthlyClaim> userClaims;
            if (userRole == "Lecturer")
            {
                userClaims = allClaims.Where(c =>
                    c.LecturerName.Contains(userName) ||
                    c.LecturerName.Contains(userEmail) ||
                    (userName != null && c.LecturerName != null && c.LecturerName.Contains(userName))).ToList();
            }
            else
            {
                userClaims = allClaims;
            }

            var model = new DashboardViewModel(userClaims)
            {
                Username = userName,
                Role = userRole,
                TotalClaims = userClaims.Count,
                PendingClaims = userClaims.Count(c => c.Status == ClaimStatus.Pending),
                ApprovedClaims = userClaims.Count(c => c.Status == ClaimStatus.Approved),
                RejectedClaims = userClaims.Count(c => c.Status == ClaimStatus.Rejected),
                Claims = userClaims
            };

            return View(model);
        }

        public IActionResult MentorDashboard()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator" && userRole != "HR")
            {
                TempData["ErrorMessage"] = "Access denied. Mentor dashboard is only for coordinators, administrators, and HR.";
                return RedirectToAction("AccessDenied", "Account");
            }

            // Get claims that are ready for current user's review
            var pendingClaims = GetClaimsForUserReview(userRole);

            var model = new DashboardViewModel(pendingClaims)
            {
                Username = User.FindFirst(ClaimTypes.GivenName)?.Value,
                Role = userRole,
                Claims = pendingClaims,
                TotalClaims = pendingClaims.Count,
                PendingClaims = pendingClaims.Count
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                TempData["ErrorMessage"] = "Access denied. HR dashboard is only for HR personnel and administrators.";
                return RedirectToAction("AccessDenied", "Account");
            }

            var allUsers = AccountController.GetUsers();
            var allClaims = ClaimController.GetClaims();
            var pendingHRClaims = GetPendingHRClaims();

            var model = new HRDashboardViewModel
            {
                Username = User.FindFirst(ClaimTypes.GivenName)?.Value,
                Role = userRole,
                TotalUsers = allUsers.Count,
                TotalClaims = allClaims.Count,
                PendingClaims = pendingHRClaims.Count, // Now shows only claims pending HR review
                ApprovedClaims = allClaims.Count(c => c.Status == ClaimStatus.Approved),
                RejectedClaims = allClaims.Count(c => c.Status == ClaimStatus.Rejected),
                Users = allUsers,
                Claims = allClaims
            };

            return View(model);
        }

        private List<MonthlyClaim> GetClaimsForUserReview(string userRole)
        {
            var allClaims = ClaimController.GetClaims();

            return userRole switch
            {
                "ProgrammeCoordinator" => allClaims.Where(c => c.CoordinatorStatus == ApprovalStatus.Pending).ToList(),
                "Administrator" => allClaims.Where(c => c.CoordinatorStatus == ApprovalStatus.Approved &&
                                                      c.AdministratorStatus == ApprovalStatus.Pending).ToList(),
                "HR" => allClaims.Where(c => c.CoordinatorStatus == ApprovalStatus.Approved &&
                                           c.AdministratorStatus == ApprovalStatus.Approved &&
                                           c.HRStatus == ApprovalStatus.Pending).ToList(),
                _ => new List<MonthlyClaim>()
            
            };
        }   
            
            private List<MonthlyClaim> GetPendingHRClaims()
        {
            return ClaimController.GetClaims()
                .Where(c => c.AdministratorStatus == ApprovalStatus.Approved &&
                           c.HRStatus == ApprovalStatus.Pending)
                .ToList();
        }
    
    }
}