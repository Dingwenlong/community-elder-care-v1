using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityElderCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiDraftsAndMemory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SessionIdHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    GeneratedText = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    VisitId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiDrafts_ElderProfiles_ElderId",
                        column: x => x.ElderId,
                        principalTable: "ElderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemoryCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionIdHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    GeneratedText = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoryCandidates_ElderProfiles_ElderId",
                        column: x => x.ElderId,
                        principalTable: "ElderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiDrafts_ElderId_Status_CreatedAt",
                table: "AiDrafts",
                columns: new[] { "ElderId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryCandidates_ElderId_ConfirmedAt",
                table: "MemoryCandidates",
                columns: new[] { "ElderId", "ConfirmedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiDrafts");

            migrationBuilder.DropTable(
                name: "MemoryCandidates");
        }
    }
}
