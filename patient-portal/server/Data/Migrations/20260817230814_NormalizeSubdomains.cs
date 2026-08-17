using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PatientPortalServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSubdomains : Migration
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
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subdomains", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subdomains_Name",
                table: "subdomains",
                column: "Name",
                unique: true);

            // Backfill: one subdomains row per distinct existing Subdomain value.
            migrationBuilder.Sql(
                """
                INSERT INTO subdomains ("Name")
                SELECT DISTINCT "Subdomain" FROM tenant_usernames;
                """);

            migrationBuilder.AddColumn<int>(
                name: "SubdomainId",
                table: "tenant_usernames",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE tenant_usernames t
                SET "SubdomainId" = s."Id"
                FROM subdomains s
                WHERE s."Name" = t."Subdomain";
                """);

            migrationBuilder.DropPrimaryKey(
                name: "PK_tenant_usernames",
                table: "tenant_usernames");

            migrationBuilder.DropColumn(
                name: "Subdomain",
                table: "tenant_usernames");

            migrationBuilder.AlterColumn<int>(
                name: "SubdomainId",
                table: "tenant_usernames",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_tenant_usernames",
                table: "tenant_usernames",
                columns: new[] { "SubdomainId", "UsernameHash" });

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_usernames_subdomains_SubdomainId",
                table: "tenant_usernames",
                column: "SubdomainId",
                principalTable: "subdomains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tenant_usernames_subdomains_SubdomainId",
                table: "tenant_usernames");

            migrationBuilder.AddColumn<string>(
                name: "Subdomain",
                table: "tenant_usernames",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE tenant_usernames t
                SET "Subdomain" = s."Name"
                FROM subdomains s
                WHERE s."Id" = t."SubdomainId";
                """);

            migrationBuilder.DropPrimaryKey(
                name: "PK_tenant_usernames",
                table: "tenant_usernames");

            migrationBuilder.DropColumn(
                name: "SubdomainId",
                table: "tenant_usernames");

            migrationBuilder.AlterColumn<string>(
                name: "Subdomain",
                table: "tenant_usernames",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_tenant_usernames",
                table: "tenant_usernames",
                columns: new[] { "Subdomain", "UsernameHash" });

            migrationBuilder.DropTable(
                name: "subdomains");
        }
    }
}
