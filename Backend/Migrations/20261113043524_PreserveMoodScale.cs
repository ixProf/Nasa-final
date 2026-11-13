using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AstronautHealthMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class PreserveMoodScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MoodScaleMaximum",
                table: "MoodEntries",
                type: "integer",
                nullable: false,
                defaultValue: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoodScaleMaximum",
                table: "MoodEntries");
        }
    }
}
