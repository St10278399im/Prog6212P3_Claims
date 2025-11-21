using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAIMS_Application.Part2.Migrations
{
    /// <inheritdoc />
    public partial class InitialCLaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyClaimsModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LecturerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HoursWorked = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CoordinatorReviewBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoordinatorReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CoordinatorReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoordinatorStatus = table.Column<int>(type: "int", nullable: false),
                    AdministratorReviewBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdministratorReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdministratorReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdministratorStatus = table.Column<int>(type: "int", nullable: false),
                    HRReviewBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HRReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HRReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HRStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyClaimsModels", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyClaimsModels");
        }
    }
}
