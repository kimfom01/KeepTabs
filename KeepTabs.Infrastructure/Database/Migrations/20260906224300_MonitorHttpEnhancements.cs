using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepTabs.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class MonitorHttpEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseHeadRequest",
                table: "Monitors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SslDaysRemaining",
                table: "MonitorChecks",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseHeadRequest",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "SslDaysRemaining",
                table: "MonitorChecks");
        }
    }
}
