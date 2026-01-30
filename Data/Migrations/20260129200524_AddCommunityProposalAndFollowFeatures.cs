using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityProposalAndFollowFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "request_id",
                table: "proposals",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "post_id",
                table: "proposals",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_follows",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    follower_id = table.Column<long>(type: "bigint", nullable: false),
                    following_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_follows", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_follows_users_follower_id",
                        column: x => x.follower_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_follows_users_following_id",
                        column: x => x.following_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_proposals_post_id",
                table: "proposals",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_follows_follower_id_following_id",
                table: "user_follows",
                columns: new[] { "follower_id", "following_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_follows_following_id",
                table: "user_follows",
                column: "following_id");

            migrationBuilder.AddForeignKey(
                name: "FK_proposals_community_posts_post_id",
                table: "proposals",
                column: "post_id",
                principalTable: "community_posts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_proposals_community_posts_post_id",
                table: "proposals");

            migrationBuilder.DropTable(
                name: "user_follows");

            migrationBuilder.DropIndex(
                name: "IX_proposals_post_id",
                table: "proposals");

            migrationBuilder.DropColumn(
                name: "post_id",
                table: "proposals");

            migrationBuilder.AlterColumn<long>(
                name: "request_id",
                table: "proposals",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
