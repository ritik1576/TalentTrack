using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicFeedbackSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "job_skills",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "job_skills",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "job_skills",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "feedback_skill_ratings",
                columns: table => new
                {
                    feedback_skill_rating_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    feedback_id = table.Column<int>(type: "int", nullable: false),
                    job_skill_id = table.Column<int>(type: "int", nullable: true),
                    skill_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    rating = table.Column<int>(type: "int", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feedback_skill_ratings", x => x.feedback_skill_rating_id);
                    table.ForeignKey(
                        name: "fk_feedback_skill_ratings_interview_feedbacks_feedback_id",
                        column: x => x.feedback_id,
                        principalTable: "interview_feedbacks",
                        principalColumn: "feedback_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_feedback_skill_ratings_job_skills_job_skill_id",
                        column: x => x.job_skill_id,
                        principalTable: "job_skills",
                        principalColumn: "job_skill_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_skill_ratings_feedback_id",
                table: "feedback_skill_ratings",
                column: "feedback_id");

            migrationBuilder.CreateIndex(
                name: "ix_feedback_skill_ratings_job_skill_id",
                table: "feedback_skill_ratings",
                column: "job_skill_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feedback_skill_ratings");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "job_skills");

            migrationBuilder.DropColumn(
                name: "source",
                table: "job_skills");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "job_skills");
        }
    }
}
