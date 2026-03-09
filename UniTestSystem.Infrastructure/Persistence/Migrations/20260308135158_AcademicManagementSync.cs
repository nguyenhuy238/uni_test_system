using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTestSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AcademicManagementSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentAcademicYear",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentSemester",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Faculty",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Faculty SET Code = Id");

            migrationBuilder.CreateIndex(
                name: "IX_Faculty_Code",
                table: "Faculty",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faculty_Code",
                table: "Faculty");

            migrationBuilder.DropColumn(
                name: "CurrentAcademicYear",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "CurrentSemester",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Faculty");
        }
    }
}
