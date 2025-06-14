using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepTabs.Database.Migrations;

/// <inheritdoc />
public partial class RenameJobTrackingTableToMonitor : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ResponseStatuses_JobTrackings_JobTrackingId",
            table: "ResponseStatuses");

        migrationBuilder.DropTable(
            name: "JobTrackings");

        migrationBuilder.RenameColumn(
            name: "JobTrackingId",
            table: "ResponseStatuses",
            newName: "MonitorId");

        migrationBuilder.RenameIndex(
            name: "IX_ResponseStatuses_JobTrackingId",
            table: "ResponseStatuses",
            newName: "IX_ResponseStatuses_MonitorId");

        migrationBuilder.CreateTable(
            name: "Monitors",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                JobTitle = table.Column<string>(type: "text", nullable: false),
                JobUrl = table.Column<string>(type: "text", nullable: false),
                RequestInterval = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Monitors", x => x.Id);
            });

        migrationBuilder.AddForeignKey(
            name: "FK_ResponseStatuses_Monitors_MonitorId",
            table: "ResponseStatuses",
            column: "MonitorId",
            principalTable: "Monitors",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ResponseStatuses_Monitors_MonitorId",
            table: "ResponseStatuses");

        migrationBuilder.DropTable(
            name: "Monitors");

        migrationBuilder.RenameColumn(
            name: "MonitorId",
            table: "ResponseStatuses",
            newName: "JobTrackingId");

        migrationBuilder.RenameIndex(
            name: "IX_ResponseStatuses_MonitorId",
            table: "ResponseStatuses",
            newName: "IX_ResponseStatuses_JobTrackingId");

        migrationBuilder.CreateTable(
            name: "JobTrackings",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                JobTitle = table.Column<string>(type: "text", nullable: false),
                JobUrl = table.Column<string>(type: "text", nullable: false),
                RequestInterval = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobTrackings", x => x.Id);
            });

        migrationBuilder.AddForeignKey(
            name: "FK_ResponseStatuses_JobTrackings_JobTrackingId",
            table: "ResponseStatuses",
            column: "JobTrackingId",
            principalTable: "JobTrackings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}