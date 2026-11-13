using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AstronautHealthMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomaticAssignmentReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssignmentReminderDeliveries",
                columns: table => new
                {
                    RecommendationId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<Guid>(type: "uuid", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsManual = table.Column<bool>(type: "boolean", nullable: false),
                    SoundEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SenderName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentReminderDeliveries", x => x.RecommendationId);
                    table.ForeignKey(
                        name: "FK_AssignmentReminderDeliveries_Recommendations_Recommendation~",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentReminderSettings",
                columns: table => new
                {
                    RecommendationId = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    SoundEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NextDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentReminderSettings", x => x.RecommendationId);
                    table.ForeignKey(
                        name: "FK_AssignmentReminderSettings_Recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentReminderDeliveries_Token",
                table: "AssignmentReminderDeliveries",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentReminderSettings_Enabled_NextDueAt",
                table: "AssignmentReminderSettings",
                columns: new[] { "Enabled", "NextDueAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentReminderDeliveries");

            migrationBuilder.DropTable(
                name: "AssignmentReminderSettings");
        }
    }
}
