using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMentorIdToRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "mentor_id",
                table: "requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reject_reason",
                table: "requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_requests_mentor_id",
                table: "requests",
                column: "mentor_id");

            migrationBuilder.AddForeignKey(
                name: "FK_requests_users_mentor_id",
                table: "requests",
                column: "mentor_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_requests_users_mentor_id",
                table: "requests");

            migrationBuilder.DropIndex(
                name: "IX_requests_mentor_id",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "mentor_id",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "reject_reason",
                table: "requests");
        }
    }
}
