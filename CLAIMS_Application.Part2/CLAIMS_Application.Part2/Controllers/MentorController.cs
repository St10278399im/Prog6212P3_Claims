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
            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator" && userRole != "HR")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var claim = GetClaimById(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("MentorDashboard", "Dashboard");
            }

            if (!IsClaimReadyForReview(claim, userRole))
            {
                TempData["ErrorMessage"] = "Claim not ready for your review or already reviewed.";
                return RedirectToAction("MentorDashboard", "Dashboard");
            }

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id, string reviewNotes)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator" && userRole != "HR")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var claim = GetClaimById(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("MentorDashboard", "Dashboard");
            }

            UpdateApprovalStatus(claim, userRole, ApprovalStatus.Approved, reviewNotes);
            UpdateFinalClaimStatus(claim);

            string nextStep = GetNextReviewStep(claim, userRole);
            TempData["SuccessMessage"] = $"Claim #{id} approved. {nextStep}";
            return RedirectToAction("MentorDashboard", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id, string reviewNotes)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator" && userRole != "HR")
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

            UpdateApprovalStatus(claim, userRole, ApprovalStatus.Rejected, reviewNotes);
            UpdateFinalClaimStatus(claim);

            string nextStep = GetNextReviewStep(claim, userRole);
            TempData["SuccessMessage"] = $"Claim #{id} rejected. {nextStep}";
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
            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator" && userRole != "HR")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var claims = status.ToLower() switch
            {
                "pending" => ClaimController.GetClaims().Where(c => c.Status == ClaimStatus.Pending).ToList(),
                "approved" => ClaimController.GetClaims().Where(c => c.Status == ClaimStatus.Approved).ToList(),
                "rejected" => ClaimController.GetClaims().Where(c => c.Status == ClaimStatus.Rejected).ToList(),
                "underreview" => ClaimController.GetClaims().Where(c => c.Status == ClaimStatus.UnderReview).ToList(),
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

        // Helper Methods
        private bool IsClaimReadyForReview(MonthlyClaim claim, string userRole)
        {
            return userRole switch
            {
                "ProgrammeCoordinator" => claim.CoordinatorStatus == ApprovalStatus.Pending,
                "Administrator" => claim.CoordinatorStatus == ApprovalStatus.Approved &&
                                 claim.AdministratorStatus == ApprovalStatus.Pending,
                "HR" => claim.CoordinatorStatus == ApprovalStatus.Approved &&
                       claim.AdministratorStatus == ApprovalStatus.Approved &&
                       claim.HRStatus == ApprovalStatus.Pending,
                _ => false
            };
        }

        private void UpdateApprovalStatus(MonthlyClaim claim, string userRole, ApprovalStatus status, string reviewNotes)
        {
            var reviewerName = User.FindFirst(ClaimTypes.GivenName)?.Value + " " + User.FindFirst(ClaimTypes.Surname)?.Value;
            var reviewDate = DateTime.Now;

            switch (userRole)
            {
                case "ProgrammeCoordinator":
                    claim.CoordinatorStatus = status;
                    claim.CoordinatorReviewBy = reviewerName;
                    claim.CoordinatorReviewDate = reviewDate;
                    claim.CoordinatorReviewNotes = reviewNotes;
                    claim.Status = ClaimStatus.UnderReview;
                    break;

                case "Administrator":
                    claim.AdministratorStatus = status;
                    claim.AdministratorReviewBy = reviewerName;
                    claim.AdministratorReviewDate = reviewDate;
                    claim.AdministratorReviewNotes = reviewNotes;
                    claim.Status = ClaimStatus.UnderReview;
                    break;

                case "HR":
                    claim.HRStatus = status;
                    claim.HRReviewBy = reviewerName;
                    claim.HRReviewDate = reviewDate;
                    claim.HRReviewNotes = reviewNotes;
                    // HR makes the final decision
                    if (status == ApprovalStatus.Approved)
                    {
                        claim.Status = ClaimStatus.Approved;
                    }
                    else
                    {
                        claim.Status = ClaimStatus.Rejected;
                    }
                    break;
            }
        }

        private void UpdateFinalClaimStatus(MonthlyClaim claim)
        {
            // Final status is now handled in UpdateApprovalStatus for HR
            // This method can be removed or kept for additional logic
        }

        private string GetNextReviewStep(MonthlyClaim claim, string currentUserRole)
        {
            return currentUserRole switch
            {
                "ProgrammeCoordinator" => "Waiting for Administrator review.",
                "Administrator" => "Waiting for HR final review.",
                "HR" when claim.Status == ClaimStatus.Approved => "Claim fully approved! Lecturer will see the final status.",
                "HR" when claim.Status == ClaimStatus.Rejected => "Claim fully rejected! Lecturer will see the final status.",
                _ => "Review completed."
            };
        }

        private MonthlyClaim GetClaimById(int id)
        {
            return ClaimController.GetClaims()?.FirstOrDefault(c => c.Id == id);
        }
    }
}