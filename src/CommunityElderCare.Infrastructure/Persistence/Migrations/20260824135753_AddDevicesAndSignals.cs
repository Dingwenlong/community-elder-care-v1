using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityElderCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDevicesAndSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_ElderProfiles_ElderId",
                        column: x => x.ElderId,
                        principalTable: "ElderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SignalType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ButtonState = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    IsSimulation = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceSignals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceSignals_CareEvents_CareEventId",
                        column: x => x.CareEventId,
                        principalTable: "CareEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceSignals_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ElderId",
                table: "Devices",
                column: "ElderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSignals_CareEventId",
                table: "DeviceSignals",
                column: "CareEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSignals_DeviceId_EventId",
                table: "DeviceSignals",
                columns: new[] { "DeviceId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSignals_DeviceId_ReceivedAt",
                table: "DeviceSignals",
                columns: new[] { "DeviceId", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceSignals");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
