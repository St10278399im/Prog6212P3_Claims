using CLAIMS_Application.Part2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace CLAIMS_Application.Part2.Controllers
{
    public class HRController : Controller
    {
        private bool IsAuthorized()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return role == "HR" || role == "Administrator";
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            if (!IsAuthorized())
            {
                TempData["ErrorMessage"] = "Access denied. HR dashboard is only for HR personnel and administrators.";
                return RedirectToAction("AccessDenied", "Account");
            }

            var allUsers = AccountController.GetUsers();
            var allClaims = ClaimController.GetClaims();

            var model = new HRDashboardViewModel
            {
                Username = User.FindFirst(ClaimTypes.GivenName)?.Value ?? "Unknown",
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown",
                TotalUsers = allUsers.Count,
                TotalClaims = allClaims.Count,
                PendingClaims = allClaims.Count(c => c.Status == ClaimStatus.Pending),
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
            if (!IsAuthorized())
                return RedirectToAction("AccessDenied", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterUser(User model)
        {
            if (!IsAuthorized())
                return RedirectToAction("AccessDenied", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var users = AccountController.GetUsers();

            if (users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "User with this email already exists.");
                return View(model);
            }

            // Validate role
            if (model.Role != "User" && model.Role != "HR" && model.Role != "Administrator")
            {
                ModelState.AddModelError("", "Invalid role selected.");
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
            if (!IsAuthorized())
                return RedirectToAction("AccessDenied", "Account");

            return View(AccountController.GetUsers());
        }

        [HttpGet]
        public IActionResult DeleteUser(int id)
        {
            if (!IsAuthorized())
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

            // Compare with logged-in user's email
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value;

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
            if (!IsAuthorized())
                return RedirectToAction("AccessDenied", "Account");

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
            if (!IsAuthorized())
                return RedirectToAction("AccessDenied", "Account");

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
    }
}
