using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KidsParadiseByShoptick.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddToyVideoLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoLink",
                table: "Toys",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoLink",
                table: "Toys");
        }
    }
}
