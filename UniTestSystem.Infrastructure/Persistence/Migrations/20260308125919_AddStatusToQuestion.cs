using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTestSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Question",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Question");
        }
    }
}
