using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityElderCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCareEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SourceEventId = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ResponsibilityQueue = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    CurrentOwnerUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Resolution = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RequiresFollowUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFollowUpCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasIncompleteMandatoryTask = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareEventEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SourceEventId = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    IsSimulation = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareEventEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareEventEvidence_CareEvents_CareEventId",
                        column: x => x.CareEventId,
                        principalTable: "CareEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareEventTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ToStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ActorKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsSimulation = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareEventTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareEventTransitions_CareEvents_CareEventId",
                        column: x => x.CareEventId,
                        principalTable: "CareEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeterministicAttemptId = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    TargetLabel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsSimulation = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactAttempts_CareEvents_CareEventId",
                        column: x => x.CareEventId,
                        principalTable: "CareEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareEventEvidence_CareEventId_RecordedAt",
                table: "CareEventEvidence",
                columns: new[] { "CareEventId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CareEventEvidence_CareEventId_SourceEventId",
                table: "CareEventEvidence",
                columns: new[] { "CareEventId", "SourceEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareEvents_ElderId_SourceEventId",
                table: "CareEvents",
                columns: new[] { "ElderId", "SourceEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareEvents_ElderId_Status",
                table: "CareEvents",
                columns: new[] { "ElderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CareEventTransitions_CareEventId_OccurredAt",
                table: "CareEventTransitions",
                columns: new[] { "CareEventId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttempts_CareEventId_DeterministicAttemptId",
                table: "ContactAttempts",
                columns: new[] { "CareEventId", "DeterministicAttemptId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareEventEvidence");

            migrationBuilder.DropTable(
                name: "CareEventTransitions");

            migrationBuilder.DropTable(
                name: "ContactAttempts");

            migrationBuilder.DropTable(
                name: "CareEvents");
        }
    }
}
