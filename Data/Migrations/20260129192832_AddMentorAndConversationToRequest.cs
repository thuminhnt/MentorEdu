using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMentorAndConversationToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "conversation_id",
                table: "requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "mentor_id",
                table: "requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_requests_conversation_id",
                table: "requests",
                column: "conversation_id",
                unique: true,
                filter: "[conversation_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_requests_mentor_id",
                table: "requests",
                column: "mentor_id");

            migrationBuilder.AddForeignKey(
                name: "FK_requests_conversations_conversation_id",
                table: "requests",
                column: "conversation_id",
                principalTable: "conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_requests_users_mentor_id",
                table: "requests",
                column: "mentor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_requests_conversations_conversation_id",
                table: "requests");

            migrationBuilder.DropForeignKey(
                name: "FK_requests_users_mentor_id",
                table: "requests");

            migrationBuilder.DropIndex(
                name: "IX_requests_conversation_id",
                table: "requests");

            migrationBuilder.DropIndex(
                name: "IX_requests_mentor_id",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "conversation_id",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "mentor_id",
                table: "requests");
        }
    }
}
