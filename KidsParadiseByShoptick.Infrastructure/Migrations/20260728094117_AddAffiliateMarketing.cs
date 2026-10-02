using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KidsParadiseByShoptick.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAffiliateMarketing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AffiliatePartnerId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AffiliatePartners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Whatsapp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliatePartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AffiliateLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AffiliatePartnerId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateLedgerEntries_AffiliatePartners_AffiliatePartnerId",
                        column: x => x.AffiliatePartnerId,
                        principalTable: "AffiliatePartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AffiliateLedgerEntries_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AffiliatePartnerId",
                table: "Orders",
                column: "AffiliatePartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateLedgerEntries_AffiliatePartnerId",
                table: "AffiliateLedgerEntries",
                column: "AffiliatePartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateLedgerEntries_OrderId_Type",
                table: "AffiliateLedgerEntries",
                columns: new[] { "OrderId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliatePartners_Code",
                table: "AffiliatePartners",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AffiliatePartners_AffiliatePartnerId",
                table: "Orders",
                column: "AffiliatePartnerId",
                principalTable: "AffiliatePartners",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AffiliatePartners_AffiliatePartnerId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "AffiliateLedgerEntries");

            migrationBuilder.DropTable(
                name: "AffiliatePartners");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AffiliatePartnerId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AffiliatePartnerId",
                table: "Orders");
        }
    }
}
