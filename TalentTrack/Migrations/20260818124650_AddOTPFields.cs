using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddOTPFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reset_otp",
                table: "recruiters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reset_otp_expiry",
                table: "recruiters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                table: "candidates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                table: "candidates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reset_otp",
                table: "candidates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reset_otp_expiry",
                table: "candidates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "recruiters",
                keyColumn: "recruiter_id",
                keyValue: 1,
                columns: new[] { "reset_otp", "reset_otp_expiry" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "recruiters",
                keyColumn: "recruiter_id",
                keyValue: 2,
                columns: new[] { "reset_otp", "reset_otp_expiry" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "recruiters",
                keyColumn: "recruiter_id",
                keyValue: 3,
                columns: new[] { "reset_otp", "reset_otp_expiry" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reset_otp",
                table: "recruiters");

            migrationBuilder.DropColumn(
                name: "reset_otp_expiry",
                table: "recruiters");

            migrationBuilder.DropColumn(
                name: "reset_otp",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "reset_otp_expiry",
                table: "candidates");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                table: "candidates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                table: "candidates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
