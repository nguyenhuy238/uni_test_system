using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTestSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyRandomFieldsFromTestAddAssessmentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssessmentType",
                table: "Test",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropColumn(
                name: "FrozenRandom",
                table: "Test");

            migrationBuilder.DropColumn(
                name: "RandomEssay",
                table: "Test");

            migrationBuilder.DropColumn(
                name: "RandomMCQ",
                table: "Test");

            migrationBuilder.DropColumn(
                name: "SubjectIdFilter",
                table: "Test");

            migrationBuilder.DropColumn(
                name: "RandomTF",
                table: "Test");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssessmentType",
                table: "Test");

            migrationBuilder.AddColumn<string>(
                name: "FrozenRandom",
                table: "Test",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RandomEssay",
                table: "Test",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RandomMCQ",
                table: "Test",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RandomTF",
                table: "Test",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SubjectIdFilter",
                table: "Test",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
