using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KidsParadiseByShoptick.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAdminPasswordHashToPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "AdminUsers");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "AdminUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "AdminUsers");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "AdminUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
