using Microsoft.AspNetCore.Mvc;
using CLAIMS_Application.Part2.Models;
using System.Security.Claims;

namespace CLAIMS_Application.Part2.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // Access claims via the static method
            var allClaims = ClaimController.GetClaims();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userName = User.FindFirst(ClaimTypes.GivenName)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;

            // Filter claims based on user role
            var userClaims = userRole == "Lecturer"
                ? allClaims.Where(c => c.LecturerName.Contains(userName) || c.LecturerName.Contains(userEmail)).ToList()
                : allClaims;

            var model = new DashboardViewModel
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

            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator")
            {
                TempData["ErrorMessage"] = "Access denied. Mentor dashboard is only for coordinators and administrators.";
                return RedirectToAction("AccessDenied", "Account");
            }

            var pendingClaims = ClaimController.GetClaims().Where(c => c.Status == ClaimStatus.Pending).ToList();
            var model = new DashboardViewModel
            {
                Username = User.FindFirst(ClaimTypes.GivenName)?.Value,
                Role = userRole,
                Claims = pendingClaims,
                TotalClaims = pendingClaims.Count,
                PendingClaims = pendingClaims.Count
            };

            return View(model);
        }
    }
}