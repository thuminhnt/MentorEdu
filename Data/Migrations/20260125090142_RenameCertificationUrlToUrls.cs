using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameCertificationUrlToUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "certification_url",
                table: "mentor_profiles");

            migrationBuilder.AddColumn<string>(
                name: "certification_urls",
                table: "mentor_profiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "certification_urls",
                table: "mentor_profiles");

            migrationBuilder.AddColumn<string>(
                name: "certification_url",
                table: "mentor_profiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
