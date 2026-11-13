using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AstronautHealthMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class FixReferenceRangesAndTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old warning/emergency columns (present in InitialCreate applied to DB)
            migrationBuilder.DropColumn(name: "MinWarning", table: "ReferenceRanges");
            migrationBuilder.DropColumn(name: "MaxWarning", table: "ReferenceRanges");
            migrationBuilder.DropColumn(name: "MinEmergency", table: "ReferenceRanges");
            migrationBuilder.DropColumn(name: "MaxEmergency", table: "ReferenceRanges");
            migrationBuilder.DropColumn(name: "MinSecondaryWarning", table: "ReferenceRanges");
            migrationBuilder.DropColumn(name: "MaxSecondaryWarning", table: "ReferenceRanges");
            migrationBuilder.DropColumn(name: "MinSecondaryEmergency", table: "ReferenceRanges");
            migrationBuilder.DropColumn(name: "MaxSecondaryEmergency", table: "ReferenceRanges");

            // Add new emergency threshold columns (same type: double precision)
            migrationBuilder.AddColumn<double>(
                name: "EmergencyAbove",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EmergencyBelow",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SecondaryEmergencyAbove",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SecondaryEmergencyBelow",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            // Seed new emergency values into the new columns
            migrationBuilder.UpdateData(
                table: "ReferenceRanges",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EmergencyAbove", "EmergencyBelow" },
                values: new object[] { 140.0, 40.0 });

            migrationBuilder.UpdateData(
                table: "ReferenceRanges",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EmergencyAbove", "EmergencyBelow", "SecondaryEmergencyAbove", "SecondaryEmergencyBelow" },
                values: new object[] { 160.0, 70.0, 100.0, 45.0 });

            migrationBuilder.UpdateData(
                table: "ReferenceRanges",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "EmergencyAbove", "EmergencyBelow" },
                values: new object[] { null, 90.0 });

            migrationBuilder.UpdateData(
                table: "ReferenceRanges",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "EmergencyAbove", "EmergencyBelow" },
                values: new object[] { 38.5, 35.0 });

            migrationBuilder.UpdateData(
                table: "ReferenceRanges",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "EmergencyAbove", "EmergencyBelow", "Notes" },
                values: new object[] { 200.0, 50.0, "PLACEHOLDER - requires review by authorized staff. 70 is a PLACEHOLDER; fasting reference up to 99 mg/dL." });

            migrationBuilder.UpdateData(
                table: "ReferenceRanges",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "EmergencyAbove", "EmergencyBelow" },
                values: new object[] { 30.0, 8.0 });

            migrationBuilder.UpdateData(
                table: "EmergencyProcedures",
                keyColumn: "Id",
                keyValue: 2,
                column: "TargetCondition",
                value: "Elevated respiratory rate (>18 breaths/min) with persistent headache");

            migrationBuilder.UpdateData(
                table: "HealthMetricGuides",
                keyColumn: "Id",
                keyValue: 3,
                column: "ActionGuidance",
                value: "SAMPLE: Reposition the sensor. If the reading remains below 95%, check cabin oxygen levels and notify your monitor immediately.");

            migrationBuilder.UpdateData(
                table: "HealthMetricGuides",
                keyColumn: "Id",
                keyValue: 4,
                column: "ActionGuidance",
                value: "SAMPLE: Note whether you were resting or had just exercised. Check the sensor, hydrate, and re-measure. Report persistent fever to your monitor.");

            migrationBuilder.UpdateData(
                table: "HealthMetricGuides",
                keyColumn: "Id",
                keyValue: 5,
                column: "InterpretationGuidance",
                value: "The research gives an Earth fasting reference of up to 99 mg/dL. Values naturally rise after eating. Always interpret with meal context.");

            migrationBuilder.UpdateData(
                table: "HealthMetricGuides",
                keyColumn: "Id",
                keyValue: 6,
                column: "ActionGuidance",
                value: "SAMPLE: If breathing feels fast or labored at rest, check cabin air readings and contact your monitor.");

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "May be associated with changes in how body fluids are distributed and how the circulatory system adapts to microgravity, especially when returning to gravity.");

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "May be associated with stored food, nutritional needs, physical activity, and mission conditions that affect fluid and energy balance.");

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "May be associated with long periods in a small space, distance from family and familiar surroundings, and continuous contact with the same crew.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EmergencyProcedures",
                keyColumn: "Id",
                keyValue: 2,
                column: "TargetCondition",
                value: "Elevated respiratory rate (>18 breaths/min) with persistent frontal headache");

            migrationBuilder.UpdateData(
                table: "HealthMetricGuides",
                keyColumn: "Id",
                keyValue: 3,
                column: "ActionGuidance",
                value: "SAMPLE: Take calm, deep breaths and reposition the sensor. If the reading remains below 95%, check cabin oxygen levels and notify your monitor immediately.");

            migrationBuilder.UpdateData(
                table: "HealthMetricGuides",
                keyColumn: "Id",
                keyValue: 4,
                column: "ActionGuidance",
                value: "SAMPLE: Note whether you were resting or had just exercised. Check the sensor, adjust clothing, hydrate, and re-measure. Report persistent fever to your monitor.");

            migrationBuilder.UpdateData(
                table: "HealthMetricGuides",
                keyColumn: "Id",
                keyValue: 5,
                column: "InterpretationGuidance",
                value: "Standard Earth fasting reference is 70 to 99 mg/dL. Values naturally rise after eating. Must always be interpreted with meal context.");

            migrationBuilder.UpdateData(
                table: "HealthMetricGuides",
                keyColumn: "Id",
                keyValue: 6,
                column: "ActionGuidance",
                value: "SAMPLE: If breathing feels fast or labored at rest, check cabin air readings, ensure you are in an area with good air circulation, and contact your monitor.");

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "May be associated with cardiovascular deconditioning and fluid volume redistribution following transition between gravitational environments.");

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "May be associated with reduced circulating plasma volume or inadequate nutritional replenishment during flight.");

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "May be associated with prolonged confinement, high operational workload, sensory deprivation, and separation from home.");

            // Drop new emergency columns
            migrationBuilder.DropColumn(name: "EmergencyAbove", table: "ReferenceRanges");
            migrationBuilder.DropColumn(name: "EmergencyBelow", table: "ReferenceRanges");
            migrationBuilder.DropColumn(name: "SecondaryEmergencyAbove", table: "ReferenceRanges");
            migrationBuilder.DropColumn(name: "SecondaryEmergencyBelow", table: "ReferenceRanges");

            // Restore old warning/emergency columns (double precision, nullable)
            migrationBuilder.AddColumn<double>(
                name: "MinWarning",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxWarning",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinEmergency",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxEmergency",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinSecondaryWarning",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxSecondaryWarning",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinSecondaryEmergency",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxSecondaryEmergency",
                table: "ReferenceRanges",
                type: "double precision",
                nullable: true);
        }
    }
}
