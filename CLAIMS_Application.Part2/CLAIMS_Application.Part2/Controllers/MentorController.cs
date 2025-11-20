using CLAIMS_Application.Part2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace CLAIMS_Application.Part2.Controllers
{
    public class MentorController : Controller
    {
        [HttpGet]
        public IActionResult Review(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var claim = GetClaimById(id);
            if (claim == null || claim.Status != ClaimStatus.Pending)
            {
                TempData["ErrorMessage"] = "Claim not found or already reviewed.";
                return RedirectToAction("MentorDashboard", "Dashboard");
            }
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id, string reviewNotes)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var claim = GetClaimById(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("MentorDashboard", "Dashboard");
            }

            // Update claim status
            claim.Status = ClaimStatus.Approved;
            claim.ReviewedBy = User.FindFirst(ClaimTypes.GivenName)?.Value + " " + User.FindFirst(ClaimTypes.Surname)?.Value;
            claim.ReviewedDate = DateTime.Now;
            claim.ReviewNotes = reviewNotes;

            TempData["SuccessMessage"] = $"Claim #{id} has been approved successfully!";
            return RedirectToAction("MentorDashboard", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id, string reviewNotes)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrWhiteSpace(reviewNotes))
            {
                TempData["ErrorMessage"] = "Please provide a reason for rejection.";
                return RedirectToAction("Review", new { id });
            }

            var claim = GetClaimById(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("MentorDashboard", "Dashboard");
            }

            // Update claim status
            claim.Status = ClaimStatus.Rejected;
            claim.ReviewedBy = User.FindFirst(ClaimTypes.GivenName)?.Value + " " + User.FindFirst(ClaimTypes.Surname)?.Value;
            claim.ReviewedDate = DateTime.Now;
            claim.ReviewNotes = reviewNotes;

            TempData["SuccessMessage"] = $"Claim #{id} has been rejected.";
            return RedirectToAction("MentorDashboard", "Dashboard");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var claim = GetClaimById(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("MentorDashboard", "Dashboard");
            }
            return View(claim);
        }

        [HttpGet]
        public IActionResult ExportClaimsByStatus(string status = "all")
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var claims = status.ToLower() switch
            {
                "pending" => ClaimController.GetClaims().Where(c => c.Status == ClaimStatus.Pending).ToList(),
                "approved" => ClaimController.GetClaims().Where(c => c.Status == ClaimStatus.Approved).ToList(),
                "rejected" => ClaimController.GetClaims().Where(c => c.Status == ClaimStatus.Rejected).ToList(),
                _ => ClaimController.GetClaims()
            };

            var csvContent = new StringBuilder();
            csvContent.AppendLine(MonthlyClaim.GetCsvHeaders());

            foreach (var claim in claims)
            {
                csvContent.AppendLine(claim.ToCsv());
            }

            var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            var fileName = $"claims_{status}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }

        // Helper method to get claim by ID
        private CLAIMS_Application.Part2.Models.MonthlyClaim GetClaimById(int id)
        {
            return ClaimController.GetClaims()?.FirstOrDefault(c => c.Id == id);
        }
    }
}