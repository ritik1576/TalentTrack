using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentTrack.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCandidateName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Candidates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Candidates",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Candidates");
        }
    }
}
