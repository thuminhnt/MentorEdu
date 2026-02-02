using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCategoryParentChild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_categories_parent_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_parent_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "categories");

            migrationBuilder.AddColumn<string>(
                name: "details",
                table: "reports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "review_id",
                table: "reports",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reports_review_id",
                table: "reports",
                column: "review_id");

            migrationBuilder.AddForeignKey(
                name: "FK_reports_reviews_review_id",
                table: "reports",
                column: "review_id",
                principalTable: "reviews",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reports_reviews_review_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "IX_reports_review_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "details",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "review_id",
                table: "reports");

            migrationBuilder.AddColumn<long>(
                name: "parent_id",
                table: "categories",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_parent_id",
                table: "categories",
                column: "parent_id");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_categories_parent_id",
                table: "categories",
                column: "parent_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
