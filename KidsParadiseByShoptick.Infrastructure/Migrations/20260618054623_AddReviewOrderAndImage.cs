using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KidsParadiseByShoptick.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewOrderAndImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_CustomerId_ToyId",
                table: "Reviews");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Reviews",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "Reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrderId_ToyId",
                table: "Reviews",
                columns: new[] { "OrderId", "ToyId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Orders_OrderId",
                table: "Reviews",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Orders_OrderId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_OrderId_ToyId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId_ToyId",
                table: "Reviews",
                columns: new[] { "CustomerId", "ToyId" },
                unique: true);
        }
    }
}
