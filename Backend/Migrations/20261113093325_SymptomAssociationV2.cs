using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AstronautHealthMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class SymptomAssociationV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfidenceCap",
                table: "SpaceConditions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HazardCode",
                table: "SpaceConditions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "ConditionSymptoms",
                type: "numeric(12,9)",
                precision: 12,
                scale: 9,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 1,
                column: "Weight",
                value: 0.25m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 2,
                column: "Weight",
                value: 1m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 3,
                column: "Weight",
                value: 1m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 4,
                column: "Weight",
                value: 0.3333333333333333333333333333m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 5,
                column: "Weight",
                value: 0.1666666666666666666666666667m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 6,
                column: "Weight",
                value: 0.3333333333333333333333333333m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 7,
                column: "Weight",
                value: 1m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 8,
                column: "Weight",
                value: 0.3333333333333333333333333333m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 9,
                column: "Weight",
                value: 0.25m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 10,
                column: "Weight",
                value: 0.1666666666666666666666666667m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 11,
                column: "Weight",
                value: 1m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 12,
                column: "Weight",
                value: 0.1666666666666666666666666667m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 13,
                column: "Weight",
                value: 0.3333333333333333333333333333m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 14,
                column: "Weight",
                value: 0.5m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 15,
                column: "Weight",
                value: 0.3333333333333333333333333333m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 16,
                column: "Weight",
                value: 0.5m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 17,
                column: "Weight",
                value: 1m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 18,
                column: "Weight",
                value: 1m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 19,
                column: "Weight",
                value: 0.3333333333333333333333333333m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 20,
                column: "Weight",
                value: 0.3333333333333333333333333333m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 21,
                column: "Weight",
                value: 0.1666666666666666666666666667m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 22,
                column: "Weight",
                value: 1m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 23,
                column: "Weight",
                value: 0.3333333333333333333333333333m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 24,
                column: "Weight",
                value: 0.25m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 25,
                column: "Weight",
                value: 1m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 26,
                column: "Weight",
                value: 0.1666666666666666666666666667m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 27,
                column: "Weight",
                value: 0.3333333333333333333333333333m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 28,
                column: "Weight",
                value: 1m);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 29,
                column: "Weight",
                value: 0.1666666666666666666666666667m);

            migrationBuilder.InsertData(
                table: "ConditionSymptoms",
                columns: new[] { "Id", "ConditionId", "CreatedAt", "SymptomId", "UpdatedAt", "Weight" },
                values: new object[,]
                {
                    { 31, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, null, 0.30m },
                    { 33, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, 0.3333333333333333333333333333m }
                });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { 85, "GRAVITY" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { 60, "GRAVITY" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { 85, "CLOSED_ENV" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { 70, "CLOSED_ENV" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { 85, "ISOLATION" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { 70, "ISOLATION" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { 85, "CLOSED_ENV" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { 85, "CLOSED_ENV" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { 50, "CLOSED_ENV" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { null, "GRAVITY" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { null, "GRAVITY" });

            migrationBuilder.UpdateData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "ConfidenceCap", "HazardCode" },
                values: new object[] { null, "GRAVITY" });

            migrationBuilder.InsertData(
                table: "SpaceConditions",
                columns: new[] { "Id", "Code", "ConfidenceCap", "CreatedAt", "Description", "HazardCode", "IsMatchable", "Name", "Notes", "SelfCareGuidance", "SourceReference", "UpdatedAt", "WhenToContactMonitor" },
                values: new object[,]
                {
                    { 13, "SPACE_MOTION_SICKNESS", 85, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Symptoms may match early adaptation after a gravity transition.", "GRAVITY", true, "Space Motion Sickness", "SAMPLE CONTENT - pending staff review", "Move slowly and avoid abrupt head movements.", "Symptom-Space Hazard Report v2, sections 3-5", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 14, "RENAL_STONE_RISK", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cannot be assessed from symptoms alone; requires specialized assessment within the mission plan.", "GRAVITY", false, "Renal Stone Risk", "SAMPLE CONTENT - pending staff review", "Follow the mission plan.", "Symptom-Space Hazard Report v2", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." }
                });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "dizziness" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "fainting" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "chest pain" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "difficulty breathing" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "blurred vision" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "headache" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "fatigue");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "difficulty concentrating" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "daytime sleepiness");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 10,
                column: "Name",
                value: "poor sleep");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Psychological", "increased stress" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Psychological", "mood changes" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Psychological", "loneliness" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "tremor" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "sweating" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "chills" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "skin symptoms" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic", "respiratory irritation" });

            migrationBuilder.InsertData(
                table: "Symptoms",
                columns: new[] { "Id", "BodyArea", "Code", "CreatedAt", "IsRedFlag", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 19, "Systemic", "NAUSEA_VOMITING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "nausea vomiting", null },
                    { 20, "Systemic", "LOSS_OF_APPETITE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "loss of appetite", null },
                    { 21, "Systemic", "NEAR_VISION_CHANGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "near vision change", null },
                    { 22, "Systemic", "FLANK_PAIN_HEMATURIA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "flank pain hematuria", null },
                    { 23, "Systemic", "SEIZURE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "seizure", null },
                    { 24, "Systemic", "SUDDEN_VISION_LOSS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "sudden vision loss", null },
                    { 25, "Systemic", "SEVERE_SUDDEN_HEADACHE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "severe sudden headache", null },
                    { 26, "Systemic", "CONFUSION_DISORIENTATION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "confusion disorientation", null },
                    { 27, "Systemic", "NASAL_CONGESTION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "nasal congestion", null },
                    { 28, "Systemic", "EYE_IRRITATION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "eye irritation", null },
                    { 29, "Systemic", "MUSCULOSKELETAL_PAIN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "musculoskeletal pain", null },
                    { 30, "Systemic", "LIGHT_FLASHES", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "light flashes", null },
                    { 31, "Systemic", "PALPITATIONS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "palpitations", null },
                    { 32, "Systemic", "NECK_FACE_SWELLING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "neck face swelling", null }
                });

            migrationBuilder.InsertData(
                table: "ConditionSymptoms",
                columns: new[] { "Id", "ConditionId", "CreatedAt", "SymptomId", "UpdatedAt", "Weight" },
                values: new object[,]
                {
                    { 30, 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, 0.25m },
                    { 32, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 21, null, 1m },
                    { 34, 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, 0.3333333333333333333333333333m },
                    { 35, 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 15, null, 0.3333333333333333333333333333m },
                    { 36, 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 19, null, 1m },
                    { 37, 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 20, null, 0.5m },
                    { 38, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 20, null, 0.5m },
                    { 39, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 27, null, 0.5m },
                    { 40, 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 27, null, 0.5m },
                    { 41, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 28, null, 0.5m },
                    { 42, 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 28, null, 0.5m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "SpaceConditions",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DropColumn(
                name: "ConfidenceCap",
                table: "SpaceConditions");

            migrationBuilder.DropColumn(
                name: "HazardCode",
                table: "SpaceConditions");

            migrationBuilder.AlterColumn<int>(
                name: "Weight",
                table: "ConditionSymptoms",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,9)",
                oldPrecision: 12,
                oldScale: 9);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 1,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 2,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 3,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 4,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 5,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 6,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 7,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 8,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 9,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 10,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 11,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 12,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 13,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 14,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 15,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 16,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 17,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 18,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 19,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 20,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 21,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 22,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 23,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 24,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 25,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 26,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 27,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 28,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ConditionSymptoms",
                keyColumn: "Id",
                keyValue: 29,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Head / Neurological", "Dizziness / Lightheadedness" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic / Cardiovascular", "Fainting / Syncope" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Cardiovascular / Chest", "Chest Pain / Tightness" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Respiratory", "Difficulty Breathing / Shortness of Breath" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Visual / Ocular", "Blurred Vision / Visual Acuity Decrease" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Head / Neurological", "Headache" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Fatigue / General Exhaustion");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Cognitive / Neurological", "Difficulty Concentrating / Mental Fog" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "Daytime Sleepiness / Excessive Somnolence");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 10,
                column: "Name",
                value: "Poor Sleep Quality / Frequent Awakenings");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Psychological / Emotional", "Increased Stress / Subjective Tension" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Psychological / Emotional", "Mood Changes / Irritability" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Psychological / Emotional", "Feelings of Isolation / Loneliness" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Neurological / Musculoskeletal", "Tremor / Muscle Shaking" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic / Autonomic", "Unusual Sweating / Diaphoresis" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Systemic / Thermal", "Chills / Shivering" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Skin / Dermatological", "Skin Irritation / Rash / Pruritus" });

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "BodyArea", "Name" },
                values: new object[] { "Respiratory", "Respiratory Irritation / Cough / Congestion" });
        }
    }
}
