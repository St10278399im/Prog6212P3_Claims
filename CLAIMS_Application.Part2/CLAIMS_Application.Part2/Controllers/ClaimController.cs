using CLAIMS_Application.Part2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace CLAIMS_Application.Part2.Controllers
{
    public class ClaimController : Controller
    {
        private static List<CLAIMS_Application.Part2.Models.MonthlyClaim> _claims = new List<CLAIMS_Application.Part2.Models.MonthlyClaim>();
        private static int _nextClaimId = 1;

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CLAIMS_Application.Part2.Models.MonthlyClaim());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CLAIMS_Application.Part2.Models.MonthlyClaim model)
        {

            try
            {
                var newClaim = new CLAIMS_Application.Part2.Models.MonthlyClaim
                {
                    Id = _nextClaimId++,
                    Title = model.Title,
                    Description = model.Description,
                    HoursWorked = model.HoursWorked,
                    HourlyRate = model.HourlyRate,
                    AdditionalNotes = model.AdditionalNotes,
                    Status = ClaimStatus.Pending,
                    SubmittedDate = DateTime.Now,
                    LecturerName = User.FindFirst(ClaimTypes.GivenName)?.Value + " " + User.FindFirst(ClaimTypes.Surname)?.Value
                };

                _claims.Add(newClaim);

                TempData["SuccessMessage"] = $"Claim submitted successfully! Total Amount: R{newClaim.TotalAmount:F2}";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while submitting your claim. Please try again.";
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
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, CLAIMS_Application.Part2.Models.MonthlyClaim model)
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

            // Update only editable fields
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

            _claims.Remove(claim);
            TempData["SuccessMessage"] = "Claim deleted successfully!";
            return RedirectToAction("Index", "Dashboard");
        }
        [HttpGet]
        public IActionResult ExportClaims(string format = "csv")
        {
            var MonthlyClaims = _claims;

            if (format.ToLower() == "csv")
            {
                var csvContent = new StringBuilder();
                csvContent.AppendLine(MonthlyClaim.GetCsvHeaders());

                foreach (var claim in MonthlyClaims)
                {
                    csvContent.AppendLine(claim.ToCsv());
                }

                var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
                return File(bytes, "text/csv", $"claims_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            else
            {
                // Default to CSV
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
                    // Skip header row
                    var header = await stream.ReadLineAsync();

                    while (!stream.EndOfStream)
                    {
                        var line = await stream.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var claim = MonthlyClaim.FromCsv(line);
                        if (claim != null && claim.Id == 0) // Only import new claims (ID = 0)
                        {
                            claim.Id = _nextClaimId++;
                            claim.SubmittedDate = DateTime.Now;
                            claim.LecturerName = claim.LecturerName ?? "Imported User";

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

        // Helper method for other controllers to access claims
        public static List<CLAIMS_Application.Part2.Models.MonthlyClaim> GetClaims()
        {
            return _claims;
        }
    }
}