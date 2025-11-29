using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendenceManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeZoneToInstitute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Institutes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Institutes");
        }
    }
}
