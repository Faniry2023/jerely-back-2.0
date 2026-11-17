using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jerely_api.Migrations
{
    /// <inheritdoc />
    public partial class addcodeconfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeVerification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Expire = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tentative = table.Column<int>(type: "int", nullable: false),
                    Use = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeVerification", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeVerification");
        }
    }
}
