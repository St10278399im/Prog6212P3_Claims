using Microsoft.AspNetCore.Mvc;
using CLAIMS_Application.Part2.Models;
using System.Security.Claims;
using System.Text;

namespace CLAIMS_Application.Part2.Controllers
{
    public class ClaimController : Controller
    {
        private static List<MonthlyClaim> _claims = new List<MonthlyClaim>();
        private static int _nextClaimId = 1;

        public static List<MonthlyClaim> GetClaims()
        {
            return _claims;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new MonthlyClaim());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MonthlyClaim model)
        {
            if (!ModelState.IsValid)
            {
                // Debug: Check what validation errors exist
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Validation Error in {state.Key}: {error.ErrorMessage}");
                    }
                }

                TempData["ErrorMessage"] = "Please fix the validation errors below.";
                return View(model);
            }

            try
            {
                // Get user information for LecturerName
                var userFirstName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? "Unknown";
                var userLastName = User.FindFirst(ClaimTypes.Surname)?.Value ?? "User";
                var lecturerName = $"{userFirstName} {userLastName}";

                var newClaim = new MonthlyClaim
                {
                    Id = _nextClaimId++,
                    Title = model.Title,
                    Description = model.Description,
                    HoursWorked = model.HoursWorked,
                    HourlyRate = model.HourlyRate,
                    AdditionalNotes = model.AdditionalNotes,
                    Status = ClaimStatus.Pending,
                    SubmittedDate = DateTime.Now,
                    LecturerName = lecturerName,

                    // Initialize all approval statuses to Pending - these should be empty/null initially
                    CoordinatorStatus = ApprovalStatus.Pending,
                    AdministratorStatus = ApprovalStatus.Pending,
                    HRStatus = ApprovalStatus.Pending,

                    // Leave review fields empty - they will be filled during the review process
                    CoordinatorReviewBy = null,
                    CoordinatorReviewDate = null,
                    CoordinatorReviewNotes = null,
                    AdministratorReviewBy = null,
                    AdministratorReviewDate = null,
                    AdministratorReviewNotes = null,
                    HRReviewBy = null,
                    HRReviewDate = null,
                    HRReviewNotes = null
                };

                _claims.Add(newClaim);

                TempData["SuccessMessage"] = $"Claim submitted successfully! It will be reviewed by Coordinator, Administrator, and HR.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while submitting your claim: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Index", "Dashboard");
            }
            return View(claim);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (claim.Status != ClaimStatus.Pending)
            {
                TempData["ErrorMessage"] = "Cannot edit claim that is already under review.";
                return RedirectToAction("Details", new { id });
            }

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, MonthlyClaim model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Invalid claim ID.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix the validation errors.";
                return View(model);
            }

            var existingClaim = _claims.FirstOrDefault(c => c.Id == id);
            if (existingClaim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (existingClaim.Status != ClaimStatus.Pending)
            {
                TempData["ErrorMessage"] = "Cannot edit claim that is already under review.";
                return RedirectToAction("Details", new { id });
            }

            existingClaim.Title = model.Title;
            existingClaim.Description = model.Description;
            existingClaim.HoursWorked = model.HoursWorked;
            existingClaim.HourlyRate = model.HourlyRate;
            existingClaim.AdditionalNotes = model.AdditionalNotes;

            TempData["SuccessMessage"] = "Claim updated successfully!";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (claim.Status != ClaimStatus.Pending)
            {
                TempData["ErrorMessage"] = "Cannot delete claim that is already under review.";
                return RedirectToAction("Details", new { id });
            }

            _claims.Remove(claim);
            TempData["SuccessMessage"] = "Claim deleted successfully!";
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult ExportClaims(string format = "csv")
        {
            var userRole = User?.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "ProgrammeCoordinator" && userRole != "Administrator" && userRole != "HR")
            {
                TempData["ErrorMessage"] = "Access denied. Only coordinators, administrators, and HR can export all claims.";
                return RedirectToAction("Index", "Dashboard");
            }

            var claims = _claims;

            if (format.ToLower() == "csv")
            {
                var csvContent = new StringBuilder();
                csvContent.AppendLine(MonthlyClaim.GetCsvHeaders());

                foreach (var claim in claims)
                {
                    csvContent.AppendLine(claim.ToCsv());
                }

                var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
                return File(bytes, "text/csv", $"claims_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            else
            {
                TempData["ErrorMessage"] = "Unsupported export format. Using CSV.";
                return ExportClaims("csv");
            }
        }

        [HttpGet]
        public IActionResult ExportMyClaims(string format = "csv")
        {
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            var userName = User.FindFirst(ClaimTypes.GivenName)?.Value + " " + User.FindFirst(ClaimTypes.Surname)?.Value;

            var myClaims = _claims.Where(c =>
                c.LecturerName.Contains(userName) || c.LecturerName == userEmail).ToList();

            if (format.ToLower() == "csv")
            {
                var csvContent = new StringBuilder();
                csvContent.AppendLine(MonthlyClaim.GetCsvHeaders());

                foreach (var claim in myClaims)
                {
                    csvContent.AppendLine(claim.ToCsv());
                }

                var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
                return File(bytes, "text/csv", $"my_claims_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            else
            {
                TempData["ErrorMessage"] = "Unsupported export format. Using CSV.";
                return ExportMyClaims("csv");
            }
        }

        [HttpGet]
        public IActionResult ImportClaims()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportClaims(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to import.";
                return View();
            }

            if (Path.GetExtension(file.FileName).ToLower() != ".csv")
            {
                TempData["ErrorMessage"] = "Only CSV files are supported for import.";
                return View();
            }

            try
            {
                var importedCount = 0;
                var skippedCount = 0;

                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    var header = await stream.ReadLineAsync();

                    while (!stream.EndOfStream)
                    {
                        var line = await stream.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var claim = MonthlyClaim.FromCsv(line);
                        if (claim != null && claim.Id == 0)
                        {
                            claim.Id = _nextClaimId++;
                            claim.SubmittedDate = DateTime.Now;
                            claim.LecturerName = claim.LecturerName ?? "Imported User";
                            claim.Status = ClaimStatus.Pending;
                            claim.CoordinatorStatus = ApprovalStatus.Pending;
                            claim.AdministratorStatus = ApprovalStatus.Pending;
                            claim.HRStatus = ApprovalStatus.Pending;

                            _claims.Add(claim);
                            importedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }

                TempData["SuccessMessage"] = $"Import completed! {importedCount} claims imported successfully. {skippedCount} claims skipped.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error importing file: {ex.Message}";
                return View();
            }
        }
    }
}    