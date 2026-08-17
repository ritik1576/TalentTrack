using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferLetters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "offer_letters",
                columns: table => new
                {
                    offer_letter_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    candidate_id = table.Column<int>(type: "int", nullable: false),
                    application_id = table.Column<int>(type: "int", nullable: false),
                    joining_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    offer_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_letters", x => x.offer_letter_id);
                    table.ForeignKey(
                        name: "fk_offer_letters_candidate_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "candidate_applications",
                        principalColumn: "application_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_offer_letters_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "candidates",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_application_id",
                table: "offer_letters",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_candidate_id",
                table: "offer_letters",
                column: "candidate_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "offer_letters");
        }
    }
}
