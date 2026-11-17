using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jerely_api.Migrations
{
    /// <inheritdoc />
    public partial class addtypeauth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Fournisseur_auth",
                table: "Login",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fournisseur_auth",
                table: "Login");
        }
    }
}
