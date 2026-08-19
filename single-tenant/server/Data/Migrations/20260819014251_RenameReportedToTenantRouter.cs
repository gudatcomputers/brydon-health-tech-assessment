using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrydonServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameReportedToTenantRouter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReportedToPatientPortal",
                table: "users",
                newName: "ReportedToTenantRouter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReportedToTenantRouter",
                table: "users",
                newName: "ReportedToPatientPortal");
        }
    }
}
