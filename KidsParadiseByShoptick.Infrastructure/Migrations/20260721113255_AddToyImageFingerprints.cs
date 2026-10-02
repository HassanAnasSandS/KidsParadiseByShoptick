using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KidsParadiseByShoptick.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddToyImageFingerprints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Embedding",
                table: "ToyImages",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PerceptualHash",
                table: "ToyImages",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToyImages_PerceptualHash",
                table: "ToyImages",
                column: "PerceptualHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ToyImages_PerceptualHash",
                table: "ToyImages");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "ToyImages");

            migrationBuilder.DropColumn(
                name: "PerceptualHash",
                table: "ToyImages");
        }
    }
}
