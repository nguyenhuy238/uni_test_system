using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTestSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddYearEndTranscriptFinalizationAndSettingsThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transcript_StudentId",
                table: "Transcript");

            migrationBuilder.AddColumn<string>(
                name: "AcademicStatus",
                table: "Transcript",
                type: "nvarchar(16)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "Transcript",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsYearEndFinalized",
                table: "Transcript",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsYearEndLocked",
                table: "Transcript",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "YearEndFinalizedAt",
                table: "Transcript",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YearEndFinalizedBy",
                table: "Transcript",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YearEndGpa10",
                table: "Transcript",
                type: "decimal(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YearEndGpa4",
                table: "Transcript",
                type: "decimal(3,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearEndTotalCreditsEarned",
                table: "Transcript",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FailGpaThreshold",
                table: "SystemSettings",
                type: "decimal(4,2)",
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<bool>(
                name: "TreatOutstandingDebtAsFail",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WarningGpaThreshold",
                table: "SystemSettings",
                type: "decimal(4,2)",
                nullable: false,
                defaultValue: 2.0m);

            migrationBuilder.CreateIndex(
                name: "IX_Transcript_StudentId_AcademicYear",
                table: "Transcript",
                columns: new[] { "StudentId", "AcademicYear" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transcript_StudentId_AcademicYear",
                table: "Transcript");

            migrationBuilder.DropColumn(
                name: "AcademicStatus",
                table: "Transcript");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "Transcript");

            migrationBuilder.DropColumn(
                name: "IsYearEndFinalized",
                table: "Transcript");

            migrationBuilder.DropColumn(
                name: "IsYearEndLocked",
                table: "Transcript");

            migrationBuilder.DropColumn(
                name: "YearEndFinalizedAt",
                table: "Transcript");

            migrationBuilder.DropColumn(
                name: "YearEndFinalizedBy",
                table: "Transcript");

            migrationBuilder.DropColumn(
                name: "YearEndGpa10",
                table: "Transcript");

            migrationBuilder.DropColumn(
                name: "YearEndGpa4",
                table: "Transcript");

            migrationBuilder.DropColumn(
                name: "YearEndTotalCreditsEarned",
                table: "Transcript");

            migrationBuilder.DropColumn(
                name: "FailGpaThreshold",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TreatOutstandingDebtAsFail",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WarningGpaThreshold",
                table: "SystemSettings");

            migrationBuilder.CreateIndex(
                name: "IX_Transcript_StudentId",
                table: "Transcript",
                column: "StudentId");
        }
    }
}
