using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantRouter.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertColumnsToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tenant_usernames_subdomains_SubdomainId",
                table: "tenant_usernames");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tenant_usernames",
                table: "tenant_usernames");

            migrationBuilder.DropPrimaryKey(
                name: "PK_subdomains",
                table: "subdomains");

            migrationBuilder.RenameColumn(
                name: "UsernameHash",
                table: "tenant_usernames",
                newName: "username_hash");

            migrationBuilder.RenameColumn(
                name: "SubdomainId",
                table: "tenant_usernames",
                newName: "subdomain_id");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_usernames_UsernameHash",
                table: "tenant_usernames",
                newName: "ix_tenant_usernames_username_hash");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "subdomains",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "subdomains",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ServerUrl",
                table: "subdomains",
                newName: "server_url");

            migrationBuilder.RenameIndex(
                name: "IX_subdomains_Name",
                table: "subdomains",
                newName: "ix_subdomains_name");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tenant_usernames",
                table: "tenant_usernames",
                columns: new[] { "subdomain_id", "username_hash" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_subdomains",
                table: "subdomains",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_usernames_subdomains_subdomain_id",
                table: "tenant_usernames",
                column: "subdomain_id",
                principalTable: "subdomains",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tenant_usernames_subdomains_subdomain_id",
                table: "tenant_usernames");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tenant_usernames",
                table: "tenant_usernames");

            migrationBuilder.DropPrimaryKey(
                name: "pk_subdomains",
                table: "subdomains");

            migrationBuilder.RenameColumn(
                name: "username_hash",
                table: "tenant_usernames",
                newName: "UsernameHash");

            migrationBuilder.RenameColumn(
                name: "subdomain_id",
                table: "tenant_usernames",
                newName: "SubdomainId");

            migrationBuilder.RenameIndex(
                name: "ix_tenant_usernames_username_hash",
                table: "tenant_usernames",
                newName: "IX_tenant_usernames_UsernameHash");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "subdomains",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "subdomains",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "server_url",
                table: "subdomains",
                newName: "ServerUrl");

            migrationBuilder.RenameIndex(
                name: "ix_subdomains_name",
                table: "subdomains",
                newName: "IX_subdomains_Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tenant_usernames",
                table: "tenant_usernames",
                columns: new[] { "SubdomainId", "UsernameHash" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_subdomains",
                table: "subdomains",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_usernames_subdomains_SubdomainId",
                table: "tenant_usernames",
                column: "SubdomainId",
                principalTable: "subdomains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
