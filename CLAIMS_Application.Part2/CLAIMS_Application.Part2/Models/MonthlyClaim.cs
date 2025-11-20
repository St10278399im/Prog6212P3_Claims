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

        public string LecturerName { get; set; }

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
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string ReviewNotes { get; set; }

        // Export methods
        public string ToCsv()
        {
            return $"{Id},{Title},{Description},{LecturerName},{HoursWorked},{HourlyRate},{TotalAmount},{Status},{SubmittedDate:yyyy-MM-dd HH:mm},{ReviewedBy},{ReviewedDate:yyyy-MM-dd HH:mm},{ReviewNotes?.Replace(",", ";")},{AdditionalNotes?.Replace(",", ";")}";
        }

        public static string GetCsvHeaders()
        {
            return "Id,Title,Description,LecturerName,HoursWorked,HourlyRate,TotalAmount,Status,SubmittedDate,ReviewedBy,ReviewedDate,ReviewNotes,AdditionalNotes";
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
                ReviewedBy = values.Length > 9 ? values[9] : string.Empty,
                ReviewedDate = values.Length > 10 && DateTime.TryParse(values[10], out var reviewed) ? reviewed : null,
                ReviewNotes = values.Length > 11 ? values[11].Replace(";", ",") : string.Empty,
                AdditionalNotes = values.Length > 12 ? values[12].Replace(";", ",") : string.Empty
            };
        }
    }

    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected
    }
}