using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TalentTrack.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Interviewers",
                keyColumn: "InterviewerId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Interviewers",
                keyColumn: "InterviewerId",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Interviewers",
                columns: new[] { "InterviewerId", "AvatarInitials", "Bio", "CreatedAt", "Department", "Email", "LinkedIn", "Name", "Password", "Phone", "Specialization", "YearsExperience" },
                values: new object[,]
                {
                    { 2, "RV", "Lead Cloud Architect specializing in Microservices.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Engineering", "rahul.verma@talenttrack.com", "linkedin.com/in/rahulverma", "Rahul Verma", "interviewer123", "+91 98765 55555", "Backend & Cloud Architecture", 10 },
                    { 3, "AP", "Product Design Lead with expertise in Frontend & UX.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Product", "ananya.patel@talenttrack.com", "linkedin.com/in/ananyapatel", "Ananya Patel", "interviewer123", "+91 98765 66666", "UI/UX & Mobile Apps", 6 }
                });
        }
    }
}
