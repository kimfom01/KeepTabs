using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepTabs.Database.Migrations;

/// <inheritdoc />
public partial class AddMonitoringHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_JobTrackings_ResponseStatus_ResponseStatusId",
            table: "JobTrackings");

        migrationBuilder.DropIndex(
            name: "IX_JobTrackings_ResponseStatusId",
            table: "JobTrackings");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ResponseStatus",
            table: "ResponseStatus");

        migrationBuilder.DropColumn(
            name: "ResponseStatusId",
            table: "JobTrackings");

        migrationBuilder.RenameTable(
            name: "ResponseStatus",
            newName: "ResponseStatuses");

        migrationBuilder.AddColumn<string>(
            name: "JobTrackingId",
            table: "ResponseStatuses",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ResponseStatuses",
            table: "ResponseStatuses",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "IX_ResponseStatuses_JobTrackingId",
            table: "ResponseStatuses",
            column: "JobTrackingId");

        migrationBuilder.AddForeignKey(
            name: "FK_ResponseStatuses_JobTrackings_JobTrackingId",
            table: "ResponseStatuses",
            column: "JobTrackingId",
            principalTable: "JobTrackings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ResponseStatuses_JobTrackings_JobTrackingId",
            table: "ResponseStatuses");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ResponseStatuses",
            table: "ResponseStatuses");

        migrationBuilder.DropIndex(
            name: "IX_ResponseStatuses_JobTrackingId",
            table: "ResponseStatuses");

        migrationBuilder.DropColumn(
            name: "JobTrackingId",
            table: "ResponseStatuses");

        migrationBuilder.RenameTable(
            name: "ResponseStatuses",
            newName: "ResponseStatus");

        migrationBuilder.AddColumn<Guid>(
            name: "ResponseStatusId",
            table: "JobTrackings",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddPrimaryKey(
            name: "PK_ResponseStatus",
            table: "ResponseStatus",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "IX_JobTrackings_ResponseStatusId",
            table: "JobTrackings",
            column: "ResponseStatusId");

        migrationBuilder.AddForeignKey(
            name: "FK_JobTrackings_ResponseStatus_ResponseStatusId",
            table: "JobTrackings",
            column: "ResponseStatusId",
            principalTable: "ResponseStatus",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}