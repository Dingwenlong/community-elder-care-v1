using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityElderCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCareWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FollowUps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedStaffUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Result = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsMandatory = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowUps_CareEvents_CareEventId",
                        column: x => x.CareEventId,
                        principalTable: "CareEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceType = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    ScheduledWindow = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ContactInstruction = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AssignedWorkerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsMandatory = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Result = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrders_CareEvents_CareEventId",
                        column: x => x.CareEventId,
                        principalTable: "CareEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedStaffUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduledStartAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ScheduledEndAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    RawStaffNote = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ConfirmedSummary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Result = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsMandatory = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitTasks_CareEvents_CareEventId",
                        column: x => x.CareEventId,
                        principalTable: "CareEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_AssignedStaffUserId_DueAt",
                table: "FollowUps",
                columns: new[] { "AssignedStaffUserId", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_CareEventId_Status",
                table: "FollowUps",
                columns: new[] { "CareEventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_AssignedWorkerUserId_Status",
                table: "ServiceOrders",
                columns: new[] { "AssignedWorkerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_CareEventId_Status",
                table: "ServiceOrders",
                columns: new[] { "CareEventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitTasks_AssignedStaffUserId_ScheduledStartAt",
                table: "VisitTasks",
                columns: new[] { "AssignedStaffUserId", "ScheduledStartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitTasks_CareEventId_Status",
                table: "VisitTasks",
                columns: new[] { "CareEventId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FollowUps");

            migrationBuilder.DropTable(
                name: "ServiceOrders");

            migrationBuilder.DropTable(
                name: "VisitTasks");
        }
    }
}
