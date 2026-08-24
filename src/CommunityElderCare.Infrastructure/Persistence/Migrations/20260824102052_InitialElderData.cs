using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityElderCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialElderData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElderProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemoDisplayName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    AreaCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    AttentionLevel = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    NextCheckInDueAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElderProfiles", x => x.Id);
                    table.CheckConstraint("CK_ElderProfiles_IsDemoData", "\"IsDemoData\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "EmergencyContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemoName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ContactOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyContacts_ElderProfiles_ElderProfileId",
                        column: x => x.ElderProfileId,
                        principalTable: "ElderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthRisks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DemoLabel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthRisks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthRisks_ElderProfiles_ElderProfileId",
                        column: x => x.ElderProfileId,
                        principalTable: "ElderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceNeeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElderProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DemoLabel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceNeeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceNeeds_ElderProfiles_ElderProfileId",
                        column: x => x.ElderProfileId,
                        principalTable: "ElderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElderProfiles_AreaCode",
                table: "ElderProfiles",
                column: "AreaCode");

            migrationBuilder.CreateIndex(
                name: "IX_ElderProfiles_AttentionLevel",
                table: "ElderProfiles",
                column: "AttentionLevel");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_ElderProfileId_ContactOrder",
                table: "EmergencyContacts",
                columns: new[] { "ElderProfileId", "ContactOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthRisks_ElderProfileId_Code",
                table: "HealthRisks",
                columns: new[] { "ElderProfileId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceNeeds_ElderProfileId_Code",
                table: "ServiceNeeds",
                columns: new[] { "ElderProfileId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyContacts");

            migrationBuilder.DropTable(
                name: "HealthRisks");

            migrationBuilder.DropTable(
                name: "ServiceNeeds");

            migrationBuilder.DropTable(
                name: "ElderProfiles");
        }
    }
}
