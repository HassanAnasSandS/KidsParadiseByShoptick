using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KidsParadiseByShoptick.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyAffiliatePartnerToNameOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "AffiliatePartners");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "AffiliatePartners");

            migrationBuilder.DropColumn(
                name: "Whatsapp",
                table: "AffiliatePartners");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "AffiliatePartners",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "AffiliatePartners",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Whatsapp",
                table: "AffiliatePartners",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
