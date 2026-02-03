using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityPostHashtagsAndFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "category_id",
                table: "community_posts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "community_post_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    post_id = table.Column<long>(type: "bigint", nullable: false),
                    file_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    file_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_community_post_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_community_post_files_community_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "community_post_hashtags",
                columns: table => new
                {
                    post_id = table.Column<long>(type: "bigint", nullable: false),
                    hashtag_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_community_post_hashtags", x => new { x.post_id, x.hashtag_id });
                    table.ForeignKey(
                        name: "FK_community_post_hashtags_community_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_community_post_hashtags_hashtags_hashtag_id",
                        column: x => x.hashtag_id,
                        principalTable: "hashtags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_community_posts_category_id",
                table: "community_posts",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_community_post_files_post_id",
                table: "community_post_files",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_community_post_hashtags_hashtag_id",
                table: "community_post_hashtags",
                column: "hashtag_id");

            migrationBuilder.AddForeignKey(
                name: "FK_community_posts_categories_category_id",
                table: "community_posts",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_community_posts_categories_category_id",
                table: "community_posts");

            migrationBuilder.DropTable(
                name: "community_post_files");

            migrationBuilder.DropTable(
                name: "community_post_hashtags");

            migrationBuilder.DropIndex(
                name: "IX_community_posts_category_id",
                table: "community_posts");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "community_posts");
        }
    }
}
