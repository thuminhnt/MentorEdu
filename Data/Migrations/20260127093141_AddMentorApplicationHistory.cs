using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMentorApplicationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mentor_application_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    action = table.Column<int>(type: "int", nullable: false),
                    application_snapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    certification_urls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ai_confidence_score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ai_model_version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    submission_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mentor_application_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_mentor_application_history_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_mentor_application_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mentor_application_history_action",
                table: "mentor_application_history",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_application_history_created_at",
                table: "mentor_application_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_application_history_created_by",
                table: "mentor_application_history",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_application_history_user_id",
                table: "mentor_application_history",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mentor_application_history");
        }
    }
}
