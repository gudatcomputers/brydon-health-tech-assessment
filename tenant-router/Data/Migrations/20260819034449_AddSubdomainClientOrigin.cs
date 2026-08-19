using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantRouter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubdomainClientOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_origin",
                table: "subdomains",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_origin",
                table: "subdomains");
        }
    }
}
