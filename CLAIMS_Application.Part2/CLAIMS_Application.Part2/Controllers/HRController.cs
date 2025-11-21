using Microsoft.AspNetCore.Mvc;
using CLAIMS_Application.Part2.Models;
using System.Security.Claims;
using System.Text;

namespace CLAIMS_Application.Part2.Controllers
{
    public class HRController : Controller
    {
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

            var model = new HRDashboardViewModel
            {
                Username = User.FindFirst(ClaimTypes.GivenName)?.Value,
                Role = userRole,
                TotalUsers = allUsers.Count,
                TotalClaims = allClaims.Count,
                PendingClaims = allClaims.Count(c => c.Status == ClaimStatus.Pending || c.Status == ClaimStatus.UnderReview),
                ApprovedClaims = allClaims.Count(c => c.Status == ClaimStatus.Approved),
                RejectedClaims = allClaims.Count(c => c.Status == ClaimStatus.Rejected),
                Users = allUsers,
                Claims = allClaims
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterUser()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterUser(User model)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var users = AccountController.GetUsers();

            if (users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "User with this email already exists.");
                return View(model);
            }

            var newUser = new User
            {
                Id = AccountController.GetNextUserId(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password,
                Role = model.Role,
                CreatedDate = DateTime.Now
            };

            users.Add(newUser);

            TempData["SuccessMessage"] = $"User {newUser.FullName} registered successfully as {newUser.Role}!";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult ManageUsers()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var users = AccountController.GetUsers();
            return View(users);
        }

        [HttpGet]
        public IActionResult DeleteUser(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Dashboard");
            }

            var users = AccountController.GetUsers();
            var user = users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            var currentUserEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            if (user.Email == currentUserEmail)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction("ManageUsers");
            }

            users.Remove(user);
            TempData["SuccessMessage"] = $"User {user.FullName} deleted successfully.";
            return RedirectToAction("ManageUsers");
        }

        [HttpGet]
        public IActionResult ExportUsers()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var users = AccountController.GetUsers();
            var csvContent = new StringBuilder();

            csvContent.AppendLine("Id,FirstName,LastName,Email,Role,CreatedDate");

            foreach (var user in users)
            {
                csvContent.AppendLine($"{user.Id},{user.FirstName},{user.LastName},{user.Email},{user.Role},{user.CreatedDate:yyyy-MM-dd HH:mm}");
            }

            var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            return File(bytes, "text/csv", $"users_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet]
        public IActionResult ExportAllData()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "HR" && userRole != "Administrator")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var users = AccountController.GetUsers();
            var claims = ClaimController.GetClaims();

            var csvContent = new StringBuilder();

            csvContent.AppendLine("USERS");
            csvContent.AppendLine("Id,FirstName,LastName,Email,Role,CreatedDate");

            foreach (var user in users)
            {
                csvContent.AppendLine($"{user.Id},{user.FirstName},{user.LastName},{user.Email},{user.Role},{user.CreatedDate:yyyy-MM-dd HH:mm}");
            }

            csvContent.AppendLine();
            csvContent.AppendLine("CLAIMS");
            csvContent.AppendLine(MonthlyClaim.GetCsvHeaders());

            foreach (var claim in claims)
            {
                csvContent.AppendLine(claim.ToCsv());
            }

            var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            return File(bytes, "text/csv", $"complete_data_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

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

            if (claim.AdministratorStatus != ApprovalStatus.Approved)
            {
                TempData["ErrorMessage"] = "This claim is not ready for HR review. It must first be approved by the Administrator.";
                return RedirectToAction("Dashboard");
            }

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

            claim.HRStatus = ApprovalStatus.Approved;
            claim.HRReviewBy = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;
            claim.HRReviewDate = DateTime.Now;
            claim.HRReviewNotes = reviewNotes;
            claim.Status = ClaimStatus.Approved;

            var index = claims.FindIndex(c => c.Id == id);
            if (index != -1)
            {
                claims[index] = claim;
            }

            TempData["SuccessMessage"] = $"Claim #{id} has been approved successfully.";
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

            claim.HRStatus = ApprovalStatus.Rejected;
            claim.HRReviewBy = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;
            claim.HRReviewDate = DateTime.Now;
            claim.HRReviewNotes = reviewNotes;
            claim.Status = ClaimStatus.Rejected;

            var index = claims.FindIndex(c => c.Id == id);
            if (index != -1)
            {
                claims[index] = claim;
            }

            TempData["SuccessMessage"] = $"Claim #{id} has been rejected.";
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

        private MonthlyClaim GetClaimById(int id)
        {
            return ClaimController.GetClaims()?.FirstOrDefault(c => c.Id == id);
        }
    }
}
