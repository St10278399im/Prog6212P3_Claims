using Xunit;
using CLAIMS_Application.Part2.Models;

namespace P3Claim
{
    public class ClaimTests
    {
        [Fact]
        public void CalculatedTotalAmount()
        {
            //arrange phase
            var claim = new MonthlyClaim();

            claim.HoursWorked = 20;

            claim.HourlyRate = 160;

            //simulation

            var getResult = claim.CalculateTotalAmount();

            //assert phase (3200)

            Assert.Equal(3200, getResult);


        }

        [Fact]

        public void AdditionalNotes_Simulation()
        {
            //arrange 

            var claim = new MonthlyClaim();
            claim.AdditionalNotes = "Additional notes submitted.";

            var notes = claim.AdditionalNotes;

            Assert.Equal("Additional notes submitted.", notes);

        }
        [Fact]
        public void ToCsv_ShouldReturnCorrectCsvFormat()
        {
            // Arrange
            var claim = new MonthlyClaim
            {
                Id = 1,
                Title = "Monthly Report",
                Description = "Work done in November",
                LecturerName = "John Doe",
                HoursWorked = 10,
                HourlyRate = 2,
                Status = ClaimStatus.Pending,
                SubmittedDate = new DateTime(2025, 11, 21, 14, 30, 0),
                CoordinatorReviewBy = "Coordinator1",
                CoordinatorReviewDate = new DateTime(2025, 11, 22, 10, 0, 0),
                CoordinatorReviewNotes = "Looks good",
                AdministratorReviewBy = "Admin1",
                AdministratorReviewDate = new DateTime(2025, 11, 23, 15, 0, 0),
                AdministratorReviewNotes = "Approved",
                HRReviewBy = "HR1",
                HRReviewDate = new DateTime(2025, 11, 24, 12, 0, 0),
                HRReviewNotes = "Final check",
                AdditionalNotes = "No issues"
            };

            var expectedCsv =
                "1,Monthly Report,Work done in November,John Doe,10,2,20,Pending,2025-11-21 14:30,Coordinator1,2025-11-22 10:00,Looks good,Admin1,2025-11-23 15:00,Approved,HR1,2025-11-24 12:00,Final check,No issues";

            // Act
            var actualCsv = claim.ToCsv();

            // Assert
            Assert.Equal(expectedCsv, actualCsv);
        }

        [Fact]
        public void FromCsv_ShouldParseCsvBackToMonthlyClaim()
        {
            // Arrange
            var csvLine =
                "1,Monthly Report,Work done in November,John Doe,10,2,20,Pending,2025-11-21 14:30,Coordinator1,2025-11-22 10:00,Looks good,Admin1,2025-11-23 15:00,Approved,HR1,2025-11-24 12:00,Final check,No issues";

            // Act
            var claim = MonthlyClaim.FromCsv(csvLine);

            // Assert
            Assert.NotNull(claim);
            Assert.Equal(1, claim.Id);
            Assert.Equal("Monthly Report", claim.Title);
            Assert.Equal(10, claim.HoursWorked);
            Assert.Equal(2, claim.HourlyRate);
            Assert.Equal(ClaimStatus.Pending, claim.Status);
            Assert.Equal(new DateTime(2025, 11, 21, 14, 30, 0), claim.SubmittedDate);
            Assert.Equal("Looks good", claim.CoordinatorReviewNotes);
        }
    }
}



