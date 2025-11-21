using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CLAIMS_Application.Part2.Models
{
    public class MonthlyClaim
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        public string? LecturerName { get; set; }

        [Required(ErrorMessage = "Hours worked is required")]
        [Range(0.1, 1000, ErrorMessage = "Hours worked must be between 0.1 and 1000")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(1, 10000, ErrorMessage = "Hourly rate must be between R1 and R10,000")]
        public decimal HourlyRate { get; set; }

        [StringLength(1000, ErrorMessage = "Additional notes cannot exceed 1000 characters")]
        public string AdditionalNotes { get; set; }

        public decimal TotalAmount => HoursWorked * HourlyRate;
        public ClaimStatus Status { get; set; }
        public DateTime SubmittedDate { get; set; }

        // Multi-level approval fields - MAKE THESE OPTIONAL (remove [Required])
        public string? CoordinatorReviewBy { get; set; }
        public DateTime? CoordinatorReviewDate { get; set; }
        public string? CoordinatorReviewNotes { get; set; }
        public ApprovalStatus CoordinatorStatus { get; set; }

        public string? AdministratorReviewBy { get; set; }
        public DateTime? AdministratorReviewDate { get; set; }
        public string? AdministratorReviewNotes { get; set; }
        public ApprovalStatus AdministratorStatus { get; set; }

        public string? HRReviewBy { get; set; }
        public DateTime? HRReviewDate { get; set; }
        public string? HRReviewNotes { get; set; }
        public ApprovalStatus HRStatus { get; set; }

        // Final status determined by all approvals/rejections
        public bool IsFullyApproved => CoordinatorStatus == ApprovalStatus.Approved &&
                                      AdministratorStatus == ApprovalStatus.Approved &&
                                      HRStatus == ApprovalStatus.Approved;

        public bool IsFullyRejected => CoordinatorStatus == ApprovalStatus.Rejected &&
                                      AdministratorStatus == ApprovalStatus.Rejected &&
                                      HRStatus == ApprovalStatus.Rejected;

        public bool IsPendingReview => !IsFullyApproved && !IsFullyRejected;

        // Export methods
        public string ToCsv()
        {
            return $"{Id},{Title},{Description},{LecturerName},{HoursWorked},{HourlyRate},{TotalAmount},{Status},{SubmittedDate:yyyy-MM-dd HH:mm},{CoordinatorReviewBy},{CoordinatorReviewDate:yyyy-MM-dd HH:mm},{CoordinatorReviewNotes?.Replace(",", ";")},{AdministratorReviewBy},{AdministratorReviewDate:yyyy-MM-dd HH:mm},{AdministratorReviewNotes?.Replace(",", ";")},{HRReviewBy},{HRReviewDate:yyyy-MM-dd HH:mm},{HRReviewNotes?.Replace(",", ";")},{AdditionalNotes?.Replace(",", ";")}";


        }

        public static string GetCsvHeaders()
        {
            return "Id,Title,Description,LecturerName,HoursWorked,HourlyRate,TotalAmount,Status,SubmittedDate,CoordinatorReviewBy,CoordinatorReviewDate,CoordinatorReviewNotes,AdministratorReviewBy,AdministratorReviewDate,AdministratorReviewNotes,HRReviewBy,HRReviewDate,HRReviewNotes,AdditionalNotes";
        }

        public static MonthlyClaim FromCsv(string csvLine)
        {
            var values = csvLine.Split(',');

            if (values.Length < 8) return null;

            return new MonthlyClaim
            {
                Id = int.TryParse(values[0], out int id) ? id : 0,
                Title = values[1],
                Description = values[2],
                LecturerName = values[3],
                HoursWorked = decimal.TryParse(values[4], out decimal hours) ? hours : 0,
                HourlyRate = decimal.TryParse(values[5], out decimal rate) ? rate : 0,
                Status = Enum.TryParse<ClaimStatus>(values[7], out var status) ? status : ClaimStatus.Pending,
                SubmittedDate = DateTime.TryParse(values[8], out var submitted) ? submitted : DateTime.Now,
                CoordinatorReviewBy = values.Length > 9 ? values[9] : string.Empty,
                CoordinatorReviewDate = values.Length > 10 && DateTime.TryParse(values[10], out var coordReviewed) ? coordReviewed : null,
                CoordinatorReviewNotes = values.Length > 11 ? values[11].Replace(";", ",") : string.Empty,
                AdministratorReviewBy = values.Length > 12 ? values[12] : string.Empty,
                AdministratorReviewDate = values.Length > 13 && DateTime.TryParse(values[13], out var adminReviewed) ? adminReviewed : null,
                AdministratorReviewNotes = values.Length > 14 ? values[14].Replace(";", ",") : string.Empty,
                HRReviewBy = values.Length > 15 ? values[15] : string.Empty,
                HRReviewDate = values.Length > 16 && DateTime.TryParse(values[16], out var hrReviewed) ? hrReviewed : null,
                HRReviewNotes = values.Length > 17 ? values[17].Replace(";", ",") : string.Empty,
                AdditionalNotes = values.Length > 18 ? values[18].Replace(";", ",") : string.Empty
            };
        }
        public decimal CalculateTotalAmount()
        {
            return HoursWorked * HourlyRate; //10 x 2 = 20
        }
    }

    public enum ClaimStatus
    {
        Pending,
        UnderReview,
        Approved,
        Rejected
    }

    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }


}