using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTestSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingGradeColumnsToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "StudentAnswer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "StudentAnswer",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "Session",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalScore",
                table: "Enrollment",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "Enrollment",
                type: "nvarchar(2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GradePoint",
                table: "Enrollment",
                type: "decimal(3,1)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "StudentAnswer");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "StudentAnswer");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "FinalScore",
                table: "Enrollment");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Enrollment");

            migrationBuilder.DropColumn(
                name: "GradePoint",
                table: "Enrollment");
        }
    }
}
