using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AstronautHealthMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class PreserveSymptomLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 19,
                column: "Name",
                value: "Nausea Vomiting");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 20,
                column: "Name",
                value: "Loss Of Appetite");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 21,
                column: "Name",
                value: "Near Vision Change");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 22,
                column: "Name",
                value: "Flank Pain Hematuria");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 23,
                column: "Name",
                value: "Seizure");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 24,
                column: "Name",
                value: "Sudden Vision Loss");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 25,
                column: "Name",
                value: "Severe Sudden Headache");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 26,
                column: "Name",
                value: "Confusion Disorientation");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 27,
                column: "Name",
                value: "Nasal Congestion");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 28,
                column: "Name",
                value: "Eye Irritation");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 29,
                column: "Name",
                value: "Musculoskeletal Pain");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 30,
                column: "Name",
                value: "Light Flashes");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 31,
                column: "Name",
                value: "Palpitations");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 32,
                column: "Name",
                value: "Neck Face Swelling");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 19,
                column: "Name",
                value: "nausea vomiting");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 20,
                column: "Name",
                value: "loss of appetite");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 21,
                column: "Name",
                value: "near vision change");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 22,
                column: "Name",
                value: "flank pain hematuria");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 23,
                column: "Name",
                value: "seizure");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 24,
                column: "Name",
                value: "sudden vision loss");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 25,
                column: "Name",
                value: "severe sudden headache");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 26,
                column: "Name",
                value: "confusion disorientation");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 27,
                column: "Name",
                value: "nasal congestion");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 28,
                column: "Name",
                value: "eye irritation");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 29,
                column: "Name",
                value: "musculoskeletal pain");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 30,
                column: "Name",
                value: "light flashes");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 31,
                column: "Name",
                value: "palpitations");

            migrationBuilder.UpdateData(
                table: "Symptoms",
                keyColumn: "Id",
                keyValue: 32,
                column: "Name",
                value: "neck face swelling");
        }
    }
}
