using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KidsParadiseByShoptick.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAdvanceAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvanceAmount",
                table: "Orders");
        }
    }
}
