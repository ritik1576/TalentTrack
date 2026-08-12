using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "candidate_documents",
                columns: table => new
                {
                    document_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    candidate_id = table.Column<int>(type: "int", nullable: false),
                    document_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_documents", x => x.document_id);
                    table.ForeignKey(
                        name: "fk_candidate_documents_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "candidates",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_candidate_documents_candidate_id",
                table: "candidate_documents",
                column: "candidate_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidate_documents");
        }
    }
}
