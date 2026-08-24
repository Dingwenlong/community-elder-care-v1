using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityElderCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAndConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessAuditRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FieldList = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessAuditRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BreakGlassGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CommunityStaffUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakGlassGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsentGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GranteeUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ElderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AreaCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    AssignedTaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsentGrantFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConsentGrantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Field = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentGrantFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentGrantFields_ConsentGrants_ConsentGrantId",
                        column: x => x.ConsentGrantId,
                        principalTable: "ConsentGrants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditRecords_ElderId_OccurredAt",
                table: "AccessAuditRecords",
                columns: new[] { "ElderId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BreakGlassGrants_ElderId_CommunityStaffUserId_CareEventId_ExpiresAt",
                table: "BreakGlassGrants",
                columns: new[] { "ElderId", "CommunityStaffUserId", "CareEventId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentGrantFields_ConsentGrantId_Field",
                table: "ConsentGrantFields",
                columns: new[] { "ConsentGrantId", "Field" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsentGrants_ElderId_GranteeUserId_ExpiresAt",
                table: "ConsentGrants",
                columns: new[] { "ElderId", "GranteeUserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Username",
                table: "UserAccounts",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessAuditRecords");

            migrationBuilder.DropTable(
                name: "BreakGlassGrants");

            migrationBuilder.DropTable(
                name: "ConsentGrantFields");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropTable(
                name: "ConsentGrants");
        }
    }
}
