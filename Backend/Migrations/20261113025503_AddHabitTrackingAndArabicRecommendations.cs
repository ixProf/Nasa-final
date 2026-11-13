using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AstronautHealthMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitTrackingAndArabicRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentAr",
                table: "Recommendations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HabitPoints",
                table: "Recommendations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDailyHabit",
                table: "Recommendations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReasonAr",
                table: "Recommendations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleAr",
                table: "Recommendations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HabitCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecommendationId = table.Column<int>(type: "integer", nullable: false),
                    AstronautId = table.Column<int>(type: "integer", nullable: false),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HabitCompletions_Recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HabitCompletions_AstronautId_CompletedOn",
                table: "HabitCompletions",
                columns: new[] { "AstronautId", "CompletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_HabitCompletions_RecommendationId_CompletedOn",
                table: "HabitCompletions",
                columns: new[] { "RecommendationId", "CompletedOn" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HabitCompletions");

            migrationBuilder.DropColumn(
                name: "ContentAr",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "HabitPoints",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "IsDailyHabit",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "ReasonAr",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "TitleAr",
                table: "Recommendations");
        }
    }
}
