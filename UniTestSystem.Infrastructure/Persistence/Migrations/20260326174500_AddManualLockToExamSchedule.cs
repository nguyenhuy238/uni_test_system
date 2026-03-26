using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using UniTestSystem.Infrastructure.Persistence;

#nullable disable

namespace UniTestSystem.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260326174500_AddManualLockToExamSchedule")]
    public partial class AddManualLockToExamSchedule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManuallyLocked",
                table: "ExamSchedule",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManuallyLocked",
                table: "ExamSchedule");
        }
    }
}
