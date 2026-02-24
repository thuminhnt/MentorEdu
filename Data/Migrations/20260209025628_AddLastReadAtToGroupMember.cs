using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLastReadAtToGroupMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_read_at",
                table: "group_members",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_read_at",
                table: "group_members");
        }
    }
}
