using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMentorApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "application_note",
                table: "mentor_profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "approval_status",
                table: "mentor_profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "certification_url",
                table: "mentor_profiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "portfolio_url",
                table: "mentor_profiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "mentor_profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "mentor_profiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "reviewed_by",
                table: "mentor_profiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specialization",
                table: "mentor_profiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_mentor_profiles_reviewed_by",
                table: "mentor_profiles",
                column: "reviewed_by");

            migrationBuilder.AddForeignKey(
                name: "FK_mentor_profiles_users_reviewed_by",
                table: "mentor_profiles",
                column: "reviewed_by",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_mentor_profiles_users_reviewed_by",
                table: "mentor_profiles");

            migrationBuilder.DropIndex(
                name: "IX_mentor_profiles_reviewed_by",
                table: "mentor_profiles");

            migrationBuilder.DropColumn(
                name: "application_note",
                table: "mentor_profiles");

            migrationBuilder.DropColumn(
                name: "approval_status",
                table: "mentor_profiles");

            migrationBuilder.DropColumn(
                name: "certification_url",
                table: "mentor_profiles");

            migrationBuilder.DropColumn(
                name: "portfolio_url",
                table: "mentor_profiles");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "mentor_profiles");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "mentor_profiles");

            migrationBuilder.DropColumn(
                name: "reviewed_by",
                table: "mentor_profiles");

            migrationBuilder.DropColumn(
                name: "specialization",
                table: "mentor_profiles");
        }
    }
}
