using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientPortalServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_usernames",
                columns: table => new
                {
                    Subdomain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UsernameHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_usernames", x => new { x.Subdomain, x.UsernameHash });
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_usernames_UsernameHash",
                table: "tenant_usernames",
                column: "UsernameHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_usernames");
        }
    }
}
