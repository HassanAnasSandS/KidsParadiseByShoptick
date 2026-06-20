using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KidsParadiseByShoptick.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CustomerWhatsappKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Customers
                SET Whatsapp = Phone
                WHERE Whatsapp IS NULL OR LTRIM(RTRIM(Whatsapp)) = '';
                UPDATE Customers
                SET Whatsapp = Email
                WHERE Whatsapp IS NULL OR LTRIM(RTRIM(Whatsapp)) = '';
                """);

            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Whatsapp",
                table: "Customers",
                column: "Whatsapp",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_Whatsapp",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);
        }
    }
}
