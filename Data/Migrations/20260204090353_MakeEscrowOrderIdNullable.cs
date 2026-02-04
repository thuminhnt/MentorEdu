using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorEdu.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeEscrowOrderIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_escrows_order_id",
                table: "escrows");

            migrationBuilder.AlterColumn<long>(
                name: "order_id",
                table: "escrows",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_escrows_order_id",
                table: "escrows",
                column: "order_id",
                unique: true,
                filter: "[order_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_escrows_order_id",
                table: "escrows");

            migrationBuilder.AlterColumn<long>(
                name: "order_id",
                table: "escrows",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_escrows_order_id",
                table: "escrows",
                column: "order_id",
                unique: true);
        }
    }
}
