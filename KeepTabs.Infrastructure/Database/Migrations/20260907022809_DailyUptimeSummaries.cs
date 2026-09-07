using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepTabs.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class DailyUptimeSummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyUptimeSummaries",
                columns: table => new
                {
                    MonitorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalChecks = table.Column<int>(type: "integer", nullable: false),
                    UpCount = table.Column<int>(type: "integer", nullable: false),
                    AverageResponseTimeMs = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyUptimeSummaries", x => new { x.MonitorId, x.Date });
                    table.ForeignKey(
                        name: "FK_DailyUptimeSummaries_Monitors_MonitorId",
                        column: x => x.MonitorId,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyUptimeSummaries");
        }
    }
}
