using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TenantRouter.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subdomains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ServerUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subdomains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_usernames",
                columns: table => new
                {
                    SubdomainId = table.Column<int>(type: "integer", nullable: false),
                    UsernameHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_usernames", x => new { x.SubdomainId, x.UsernameHash });
                    table.ForeignKey(
                        name: "FK_tenant_usernames_subdomains_SubdomainId",
                        column: x => x.SubdomainId,
                        principalTable: "subdomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subdomains_Name",
                table: "subdomains",
                column: "Name",
                unique: true);

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

            migrationBuilder.DropTable(
                name: "subdomains");
        }
    }
}
