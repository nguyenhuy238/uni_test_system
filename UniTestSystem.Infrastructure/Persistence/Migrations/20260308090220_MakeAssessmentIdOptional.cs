using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTestSystem.Migrations
{
    /// <inheritdoc />
    public partial class MakeAssessmentIdOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Test_AssessmentId",
                table: "Test");

            migrationBuilder.AlterColumn<string>(
                name: "AssessmentId",
                table: "Test",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Test_AssessmentId",
                table: "Test",
                column: "AssessmentId",
                unique: true,
                filter: "[AssessmentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Test_AssessmentId",
                table: "Test");

            migrationBuilder.AlterColumn<string>(
                name: "AssessmentId",
                table: "Test",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Test_AssessmentId",
                table: "Test",
                column: "AssessmentId",
                unique: true);
        }
    }
}
