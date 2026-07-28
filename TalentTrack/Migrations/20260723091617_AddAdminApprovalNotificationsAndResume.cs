using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TalentTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminApprovalNotificationsAndResume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetUserEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    UserAccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.UserAccountId);
                });

            migrationBuilder.UpdateData(
                table: "Interviewers",
                keyColumn: "InterviewerId",
                keyValue: 1,
                column: "Bio",
                value: "Senior Software Engineer with 8+ years of experience.");

            migrationBuilder.InsertData(
                table: "Interviewers",
                columns: new[] { "InterviewerId", "AvatarInitials", "Bio", "CreatedAt", "Department", "Email", "LinkedIn", "Name", "Password", "Phone", "Specialization", "YearsExperience" },
                values: new object[,]
                {
                    { 2, "RV", "Lead Cloud Architect specializing in Microservices.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Engineering", "rahul.verma@talenttrack.com", "linkedin.com/in/rahulverma", "Rahul Verma", "interviewer123", "+91 98765 55555", "Backend & Cloud Architecture", 10 },
                    { 3, "AP", "Product Design Lead with expertise in Frontend & UX.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Product", "ananya.patel@talenttrack.com", "linkedin.com/in/ananyapatel", "Ananya Patel", "interviewer123", "+91 98765 66666", "UI/UX & Mobile Apps", 6 }
                });

            migrationBuilder.InsertData(
                table: "UserAccounts",
                columns: new[] { "UserAccountId", "CreatedAt", "Department", "Email", "IsApproved", "Name", "Password", "Phone", "Role", "Status" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Administration", "admin@talenttrack.com", true, "System Admin", "admin123", "+91 99999 88888", "Admin", "Approved" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Human Resources", "recruiter@talenttrack.com", true, "Mansi Verma", "recruiter123", "+91 98765 11111", "Recruiter", "Approved" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Engineering", "interviewer@talenttrack.com", true, "Priya Sharma", "interviewer123", "+91 98765 43210", "Interviewer", "Approved" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DeleteData(
                table: "Interviewers",
                keyColumn: "InterviewerId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Interviewers",
                keyColumn: "InterviewerId",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "Interviewers",
                keyColumn: "InterviewerId",
                keyValue: 1,
                column: "Bio",
                value: "Senior Software Engineer with 8+ years of experience in hiring and technical assessments.");
        }
    }
}
