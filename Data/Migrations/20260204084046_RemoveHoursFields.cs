using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHoursFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expected_hours",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "estimated_hours",
                table: "proposals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "expected_hours",
                table: "requests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_hours",
                table: "proposals",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
