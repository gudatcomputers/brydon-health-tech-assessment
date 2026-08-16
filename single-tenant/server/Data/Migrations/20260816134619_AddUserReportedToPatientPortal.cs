using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrydonServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserReportedToPatientPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReportedToPatientPortal",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportedToPatientPortal",
                table: "users");
        }
    }
}
