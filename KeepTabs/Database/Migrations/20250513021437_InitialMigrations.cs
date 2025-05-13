using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeepTabs.Database.Migrations;

/// <inheritdoc />
public partial class InitialMigrations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ResponseStatus",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StatusCode = table.Column<int>(type: "integer", nullable: true),
                ResponseLatency = table.Column<double>(type: "double precision", nullable: false),
                StatusMessage = table.Column<string>(type: "text", nullable: true),
                RunningState = table.Column<int>(type: "integer", nullable: false),
                RunningStateName = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResponseStatus", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "JobTrackings",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                JobTitle = table.Column<string>(type: "text", nullable: false),
                JobUrl = table.Column<string>(type: "text", nullable: false),
                RequestInterval = table.Column<int>(type: "integer", nullable: false),
                ResponseStatusId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobTrackings", x => x.Id);
                table.ForeignKey(
                    name: "FK_JobTrackings_ResponseStatus_ResponseStatusId",
                    column: x => x.ResponseStatusId,
                    principalTable: "ResponseStatus",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_JobTrackings_ResponseStatusId",
            table: "JobTrackings",
            column: "ResponseStatusId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "JobTrackings");

        migrationBuilder.DropTable(
            name: "ResponseStatus");
    }
}