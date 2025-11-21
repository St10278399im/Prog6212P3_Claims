using Microsoft.AspNetCore.Mvc;
using CLAIMS_Application.Part2.Models;
using System.Security.Claims;
using System.Text;

namespace CLAIMS_Application.Part2.Controllers
{
    public class HRController : Controller
    {
        // ... your existing methods ...

        [HttpGet]
        public IActionResult ReviewClaim(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                TempData["ErrorMessage"] = "Access denied. Only HR personnel can review claims.";
                return RedirectToAction("AccessDenied", "Account");
            }

            var claim = GetClaimById(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            // Check if claim is ready for HR review (Administrator must have approved)
            if (claim.AdministratorStatus != ApprovalStatus.Approved)
            {
                TempData["ErrorMessage"] = "This claim is not ready for HR review. It must first be approved by the Administrator.";
                return RedirectToAction("Dashboard");
            }

            // Check if HR has already reviewed
            if (claim.HRStatus != ApprovalStatus.Pending)
            {
                TempData["ErrorMessage"] = "This claim has already been reviewed by HR.";
                return RedirectToAction("Dashboard");
            }

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id, string reviewNotes = "")
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Dashboard");
            }

            var claims = ClaimController.GetClaims();
            var claim = claims.FirstOrDefault(c => c.Id == id);

            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                // Update HR approval
                claim.HRStatus = ApprovalStatus.Approved;
                claim.HRReviewBy = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;
                claim.HRReviewDate = DateTime.Now;
                claim.HRReviewNotes = reviewNotes;

                // Update overall status
                claim.Status = ClaimStatus.Approved;

                // Save changes to the claims list
                var index = claims.FindIndex(c => c.Id == id);
                if (index != -1)
                {
                    claims[index] = claim;
                }

                TempData["SuccessMessage"] = $"Claim #{id} has been approved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving claim: {ex.Message}";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id, string reviewNotes)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Dashboard");
            }

            if (string.IsNullOrWhiteSpace(reviewNotes))
            {
                TempData["ErrorMessage"] = "Review notes are required when rejecting a claim.";
                return RedirectToAction("ReviewClaim", new { id });
            }

            var claims = ClaimController.GetClaims();
            var claim = claims.FirstOrDefault(c => c.Id == id);

            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                // Update HR rejection
                claim.HRStatus = ApprovalStatus.Rejected;
                claim.HRReviewBy = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;
                claim.HRReviewDate = DateTime.Now;
                claim.HRReviewNotes = reviewNotes;

                // Update overall status
                claim.Status = ClaimStatus.Rejected;

                // Save changes to the claims list
                var index = claims.FindIndex(c => c.Id == id);
                if (index != -1)
                {
                    claims[index] = claim;
                }

                TempData["SuccessMessage"] = $"Claim #{id} has been rejected.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error rejecting claim: {ex.Message}";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult PendingClaims()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Dashboard");
            }

            var allClaims = ClaimController.GetClaims();
            var pendingClaims = allClaims
                .Where(c => c.AdministratorStatus == ApprovalStatus.Approved &&
                           c.HRStatus == ApprovalStatus.Pending)
                .OrderByDescending(c => c.SubmittedDate)
                .ToList();

            return View(pendingClaims);
        }

        [HttpGet]
        public IActionResult ClaimHistory()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Dashboard");
            }

            var allClaims = ClaimController.GetClaims();
            var hrReviewedClaims = allClaims
                .Where(c => c.HRStatus != ApprovalStatus.Pending)
                .OrderByDescending(c => c.HRReviewDate)
                .ToList();

            return View(hrReviewedClaims);
        }

        // Helper method to get claims pending HR review for dashboard

        private MonthlyClaim GetClaimById(int id)
        {
            return ClaimController.GetClaims()?.FirstOrDefault(c => c.Id == id);
        }
    }
}