using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepTabs.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ExtendEntitiesWithAuditableFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Monitors",
                newName: "LastModified");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "MonitorChecks",
                newName: "LastModified");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AlertRules",
                newName: "LastModified");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AlertLogs",
                newName: "LastModified");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Created",
                table: "Monitors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Monitors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Monitors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Created",
                table: "MonitorChecks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "MonitorChecks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "MonitorChecks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Created",
                table: "AlertRules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AlertRules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "AlertRules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Created",
                table: "AlertLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AlertLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "AlertLogs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "MonitorChecks");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MonitorChecks");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "MonitorChecks");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "AlertLogs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AlertLogs");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "AlertLogs");

            migrationBuilder.RenameColumn(
                name: "LastModified",
                table: "Monitors",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "LastModified",
                table: "MonitorChecks",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "LastModified",
                table: "AlertRules",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "LastModified",
                table: "AlertLogs",
                newName: "CreatedAt");
        }
    }
}
