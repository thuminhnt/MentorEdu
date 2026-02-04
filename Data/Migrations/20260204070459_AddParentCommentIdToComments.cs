using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParentCommentIdToComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "parent_comment_id",
                table: "community_comments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_community_comments_parent_comment_id",
                table: "community_comments",
                column: "parent_comment_id");

            migrationBuilder.AddForeignKey(
                name: "FK_community_comments_community_comments_parent_comment_id",
                table: "community_comments",
                column: "parent_comment_id",
                principalTable: "community_comments",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_community_comments_community_comments_parent_comment_id",
                table: "community_comments");

            migrationBuilder.DropIndex(
                name: "IX_community_comments_parent_comment_id",
                table: "community_comments");

            migrationBuilder.DropColumn(
                name: "parent_comment_id",
                table: "community_comments");
        }
    }
}
