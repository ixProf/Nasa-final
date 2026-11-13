using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AstronautHealthMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationScheduleMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrescribedBy",
                table: "Recommendations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduledTime",
                table: "Recommendations",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrescribedBy",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "ScheduledTime",
                table: "Recommendations");
        }
    }
}
