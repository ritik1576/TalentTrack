using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentTrack.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePanelInterviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_interviews_interviewers_interviewer_id",
                table: "interviews");

            migrationBuilder.DropIndex(
                name: "ix_interviews_interviewer_id",
                table: "interviews");

            migrationBuilder.DropColumn(
                name: "interviewer_id",
                table: "interviews");

            migrationBuilder.CreateTable(
                name: "interview_interviewers",
                columns: table => new
                {
                    interview_id = table.Column<int>(type: "int", nullable: false),
                    interviewer_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_interviewers", x => new { x.interview_id, x.interviewer_id });
                    table.ForeignKey(
                        name: "fk_interview_interviewers_interviewers_interviewer_id",
                        column: x => x.interviewer_id,
                        principalTable: "interviewers",
                        principalColumn: "interviewer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_interview_interviewers_interviews_interview_id",
                        column: x => x.interview_id,
                        principalTable: "interviews",
                        principalColumn: "interview_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_interview_interviewers_interviewer_id",
                table: "interview_interviewers",
                column: "interviewer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interview_interviewers");

            migrationBuilder.AddColumn<int>(
                name: "interviewer_id",
                table: "interviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_interviews_interviewer_id",
                table: "interviews",
                column: "interviewer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_interviews_interviewers_interviewer_id",
                table: "interviews",
                column: "interviewer_id",
                principalTable: "interviewers",
                principalColumn: "interviewer_id");
        }
    }
}
