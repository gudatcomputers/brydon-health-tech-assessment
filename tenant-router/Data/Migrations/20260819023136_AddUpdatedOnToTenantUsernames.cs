using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantRouter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedOnToTenantUsernames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "updated_on",
                table: "tenant_usernames",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "timezone('utc', now())");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_on",
                table: "tenant_usernames");
        }
    }
}
