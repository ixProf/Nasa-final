using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AstronautHealthMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmergencyProcedures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetCondition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImmediateAction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StepByStepInstructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RequiredEquipment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GuidanceNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyProcedures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Destination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpaceConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SelfCareGuidance = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WhenToContactMonitor = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsMatchable = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceConditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Symptoms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BodyArea = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsRedFlag = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Symptoms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VitalSignTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSignTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Astronauts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AstronautCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BloodType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    HeightCm = table.Column<double>(type: "double precision", nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false),
                    EmergencyContactInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MissionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Astronauts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Astronauts_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConditionSymptoms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConditionId = table.Column<int>(type: "integer", nullable: false),
                    SymptomId = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionSymptoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConditionSymptoms_SpaceConditions_ConditionId",
                        column: x => x.ConditionId,
                        principalTable: "SpaceConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConditionSymptoms_Symptoms_SymptomId",
                        column: x => x.SymptomId,
                        principalTable: "Symptoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConditionVitalSigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConditionId = table.Column<int>(type: "integer", nullable: false),
                    VitalSignTypeId = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContextNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionVitalSigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConditionVitalSigns_SpaceConditions_ConditionId",
                        column: x => x.ConditionId,
                        principalTable: "SpaceConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConditionVitalSigns_VitalSignTypes_VitalSignTypeId",
                        column: x => x.VitalSignTypeId,
                        principalTable: "VitalSignTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HealthMetricGuides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VitalSignTypeId = table.Column<int>(type: "integer", nullable: false),
                    WhatIsIt = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    WhyItMatters = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    HowToMeasure = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    InterpretationGuidance = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    ActionGuidance = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthMetricGuides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthMetricGuides_VitalSignTypes_VitalSignTypeId",
                        column: x => x.VitalSignTypeId,
                        principalTable: "VitalSignTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VitalSignTypeId = table.Column<int>(type: "integer", nullable: false),
                    MinNormal = table.Column<double>(type: "double precision", nullable: false),
                    MaxNormal = table.Column<double>(type: "double precision", nullable: false),
                    MinWarning = table.Column<double>(type: "double precision", nullable: true),
                    MaxWarning = table.Column<double>(type: "double precision", nullable: true),
                    MinEmergency = table.Column<double>(type: "double precision", nullable: true),
                    MaxEmergency = table.Column<double>(type: "double precision", nullable: true),
                    MinSecondaryNormal = table.Column<double>(type: "double precision", nullable: true),
                    MaxSecondaryNormal = table.Column<double>(type: "double precision", nullable: true),
                    MinSecondaryWarning = table.Column<double>(type: "double precision", nullable: true),
                    MaxSecondaryWarning = table.Column<double>(type: "double precision", nullable: true),
                    MinSecondaryEmergency = table.Column<double>(type: "double precision", nullable: true),
                    MaxSecondaryEmergency = table.Column<double>(type: "double precision", nullable: true),
                    ApplicableGender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MinAge = table.Column<int>(type: "integer", nullable: true),
                    MaxAge = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsMissionApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceRanges_VitalSignTypes_VitalSignTypeId",
                        column: x => x.VitalSignTypeId,
                        principalTable: "VitalSignTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientGeneratedId = table.Column<Guid>(type: "uuid", nullable: false),
                    AstronautId = table.Column<int>(type: "integer", nullable: false),
                    SenderUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SenderRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Astronauts_AstronautId",
                        column: x => x.AstronautId,
                        principalTable: "Astronauts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MoodEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientGeneratedId = table.Column<Guid>(type: "uuid", nullable: false),
                    AstronautId = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MoodScore = table.Column<int>(type: "integer", nullable: false),
                    StressLevel = table.Column<int>(type: "integer", nullable: false),
                    DominantEmotion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoodEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MoodEntries_Astronauts_AstronautId",
                        column: x => x.AstronautId,
                        principalTable: "Astronauts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AstronautId = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recommendations_Astronauts_AstronautId",
                        column: x => x.AstronautId,
                        principalTable: "Astronauts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SleepRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientGeneratedId = table.Column<Guid>(type: "uuid", nullable: false),
                    AstronautId = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationHours = table.Column<double>(type: "double precision", nullable: false),
                    Quality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DeepSleepMinutes = table.Column<int>(type: "integer", nullable: false),
                    RemSleepMinutes = table.Column<int>(type: "integer", nullable: false),
                    LightSleepMinutes = table.Column<int>(type: "integer", nullable: false),
                    AwakeMinutes = table.Column<int>(type: "integer", nullable: false),
                    SleepEfficiencyPercentage = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SleepRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SleepRecords_Astronauts_AstronautId",
                        column: x => x.AstronautId,
                        principalTable: "Astronauts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VitalSignReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientGeneratedId = table.Column<Guid>(type: "uuid", nullable: false),
                    AstronautId = table.Column<int>(type: "integer", nullable: false),
                    VitalSignTypeId = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    SecondaryValue = table.Column<double>(type: "double precision", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MeasurementMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SensorLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BodyPosition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActivityContext = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DataQuality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MealContext = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MinutesSinceLastMeal = table.Column<int>(type: "integer", nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSignReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VitalSignReadings_Astronauts_AstronautId",
                        column: x => x.AstronautId,
                        principalTable: "Astronauts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VitalSignReadings_VitalSignTypes_VitalSignTypeId",
                        column: x => x.VitalSignTypeId,
                        principalTable: "VitalSignTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HealthAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AstronautId = table.Column<int>(type: "integer", nullable: false),
                    VitalSignReadingId = table.Column<int>(type: "integer", nullable: true),
                    EmergencyProcedureId = table.Column<int>(type: "integer", nullable: true),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActionRequired = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ActionTaken = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDelayed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthAlerts_Astronauts_AstronautId",
                        column: x => x.AstronautId,
                        principalTable: "Astronauts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthAlerts_EmergencyProcedures_EmergencyProcedureId",
                        column: x => x.EmergencyProcedureId,
                        principalTable: "EmergencyProcedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HealthAlerts_VitalSignReadings_VitalSignReadingId",
                        column: x => x.VitalSignReadingId,
                        principalTable: "VitalSignReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "EmergencyProcedures",
                columns: new[] { "Id", "Code", "CreatedAt", "GuidanceNotes", "ImmediateAction", "RequiredEquipment", "Severity", "StepByStepInstructions", "TargetCondition", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "EP-HYPOXIA-01", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SAMPLE CONTENT - Pending formal review by authorized flight operations staff.", "Immediately don portable emergency oxygen mask and verify positive flow indicator.", "Portable Breathing Apparatus (PBA), Pulse Oximeter", "Critical", "1. Don the nearest portable breathing apparatus (PBA).\n2. Verify the oxygen regulator is set to 100% flow.\n3. Inform Commander and Ground Monitor via voice loop.\n4. Check environmental console for cabin partial oxygen pressure.\n5. Remain stationary and secured until SpO2 stabilizes above 95%.", "SpO2 reading below 90% or unexplained lightheadedness / confusion", "Suspected Low Oxygen (Hypoxia) Response", null },
                    { 2, "EP-CO2-02", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SAMPLE CONTENT - Pending formal review by authorized flight operations staff.", "Move immediately toward an area with active cabin ventilation fans.", "Environmental console, personal air sampler", "Moderate", "1. Relocate away from enclosed dead-air corners to a central ventilation duct.\n2. Confirm local air circulation fan is running.\n3. Report readings and symptoms to Ground Monitor.\n4. Check environmental console for CO2 scrubber status.", "Elevated respiratory rate (>18 breaths/min) with persistent frontal headache", "Elevated Cabin CO2 / Headache Protocol", null }
                });

            migrationBuilder.InsertData(
                table: "SpaceConditions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsMatchable", "Name", "Notes", "SelfCareGuidance", "SourceReference", "UpdatedAt", "WhenToContactMonitor" },
                values: new object[,]
                {
                    { 1, "ORTHOSTATIC_INTOLERANCE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with cardiovascular deconditioning and fluid volume redistribution following transition between gravitational environments.", true, "Orthostatic Intolerance", "SAMPLE CONTENT - pending staff review", "Keep up the mission physical activity program, re-measure blood pressure and record your body position, and report to your monitor.", "Research doc 3.1, 4.1", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 2, "SPACEFLIGHT_VISION_CHANGES", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with the shift of body fluids toward the head in microgravity.", true, "Spaceflight-Associated Neuro-ocular Changes", "SAMPLE CONTENT - pending staff review. Cannot be identified from symptoms or vital signs; needs specialized eye exams.", "Report the symptom to your monitor and maintain scheduled eye examinations from the mission plan. Note that this cannot be evaluated from vital signs.", "Research doc 2.4, 4.4 (NASA: Risk of SANS)", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 3, "CLOSED_ENVIRONMENT_AIR_QUALITY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with carbon dioxide levels or other air quality factors in a closed cabin.", true, "Closed Environment Air Quality Effects", "SAMPLE CONTENT - pending staff review. Note that CO2 level is environmental data, not a vital sign. Self-care guidance is a placeholder (source does not specify).", "Tell a monitor and check cabin environment readings.", "Research doc 2.5", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 4, "DEHYDRATION_NUTRITION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with reduced circulating plasma volume or inadequate nutritional replenishment during flight.", true, "Hydration and Nutrition Depletion", "SAMPLE CONTENT - pending staff review", "Follow the mission nutrition plan and monitor your fluid intake. Report dizziness or fatigue to your monitor.", "Research doc 4.8, 3.2", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 5, "SLEEP_DEFICIENCY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with isolation, stress, day/night cycle changes, work schedule, light and noise.", true, "Sleep Deficiency", "SAMPLE CONTENT - pending staff review", "Keep regular sleep and wake times and reduce things that disrupt sleep, such as light and noise.", "Research doc 3.7, 4.6", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 6, "STRESS_ISOLATION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with prolonged confinement, high operational workload, sensory deprivation, and separation from home.", true, "Stress and Mission Isolation", "SAMPLE CONTENT - pending staff review. Never label as depression or anxiety.", "Stay in social contact with your crew, complete your daily self-assessment, try relaxation exercises, and contact support staff.", "Research doc 2.2, 3.8, 4.6", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 7, "BLOOD_SUGAR_CHANGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with changes in glucose metabolism during spaceflight.", true, "Blood Glucose Variation", "SAMPLE CONTENT - pending staff review", "Check meal timing, re-measure, and follow the physician's plan.", "Research doc 3.3", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 8, "TEMPERATURE_CHANGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with infection, inflammation, heat stress, exercise, or measurement conditions.", true, "Thermal Regulation Variation", "SAMPLE CONTENT - pending staff review", "Check the sensor and measurement conditions, note whether you were resting or had just exercised, and record your symptoms.", "Research doc 3.4", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 9, "IMMUNE_SKIN_CHANGES", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with changes in the immune system or skin during spaceflight.", true, "Immune Dysregulation and Skin Changes", "SAMPLE CONTENT - pending staff review. Note that temperature alone cannot show immune status.", "Record symptoms, follow hygiene and prevention procedures, and seek medical follow-up.", "Research doc 4.5", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 10, "BONE_LOSS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with reduced mechanical load on weight-bearing bones in low gravity.", false, "Bone Mineral Density Loss", "SAMPLE CONTENT - pending staff review. Knowledge-only; cannot be evaluated from wearables.", "Follow the mission physical activity program (resistance and aerobic exercise). Assessed by specialized mission exams, not by daily wearables.", "Research doc 4.2", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 11, "MUSCLE_ATROPHY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with reduced use of the muscles that work against gravity.", false, "Muscle Mass and Strength Loss", "SAMPLE CONTENT - pending staff review. Knowledge-only; cannot be evaluated from wearables.", "Follow the mission physical activity program (resistance and aerobic exercise). Assessed by specialized mission exams, not by daily wearables.", "Research doc 4.3", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." },
                    { 12, "VENOUS_THROMBOEMBOLISM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "May be associated with changes in blood flow in the neck (jugular) veins during spaceflight, including slowed, stagnant or reversed flow.", false, "Internal Jugular Venous Flow Alteration / Thrombosis Risk", "SAMPLE CONTENT - pending staff review. Knowledge-only; cannot be evaluated from wearables or self-reported symptoms.", "Requires specialized ultrasound interpreted by medical staff; cannot be detected from heart rate, blood pressure, or self-reported symptoms.", "Research doc 4.7", null, "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal." }
                });

            migrationBuilder.InsertData(
                table: "Symptoms",
                columns: new[] { "Id", "BodyArea", "Code", "CreatedAt", "IsRedFlag", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Head / Neurological", "DIZZINESS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Dizziness / Lightheadedness", null },
                    { 2, "Systemic / Cardiovascular", "FAINTING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Fainting / Syncope", null },
                    { 3, "Cardiovascular / Chest", "CHEST_PAIN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Chest Pain / Tightness", null },
                    { 4, "Respiratory", "DIFFICULTY_BREATHING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Difficulty Breathing / Shortness of Breath", null },
                    { 5, "Visual / Ocular", "BLURRED_VISION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Blurred Vision / Visual Acuity Decrease", null },
                    { 6, "Head / Neurological", "HEADACHE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Headache", null },
                    { 7, "Systemic", "FATIGUE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Fatigue / General Exhaustion", null },
                    { 8, "Cognitive / Neurological", "DIFFICULTY_CONCENTRATING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Difficulty Concentrating / Mental Fog", null },
                    { 9, "Systemic", "DAYTIME_SLEEPINESS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Daytime Sleepiness / Excessive Somnolence", null },
                    { 10, "Systemic", "POOR_SLEEP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Poor Sleep Quality / Frequent Awakenings", null },
                    { 11, "Psychological / Emotional", "INCREASED_STRESS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Increased Stress / Subjective Tension", null },
                    { 12, "Psychological / Emotional", "MOOD_CHANGES", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Mood Changes / Irritability", null },
                    { 13, "Psychological / Emotional", "LONELINESS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Feelings of Isolation / Loneliness", null },
                    { 14, "Neurological / Musculoskeletal", "TREMOR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Tremor / Muscle Shaking", null },
                    { 15, "Systemic / Autonomic", "SWEATING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Unusual Sweating / Diaphoresis", null },
                    { 16, "Systemic / Thermal", "CHILLS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Chills / Shivering", null },
                    { 17, "Skin / Dermatological", "SKIN_SYMPTOMS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Skin Irritation / Rash / Pruritus", null },
                    { 18, "Respiratory", "RESPIRATORY_IRRITATION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Respiratory Irritation / Cough / Congestion", null }
                });

            migrationBuilder.InsertData(
                table: "VitalSignTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Name", "Unit", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "HR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Number of cardiac contractions per minute", "Heart Rate", "bpm", null },
                    { 2, "BP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Arterial vascular pressure (Systolic / Diastolic)", "Blood Pressure", "mmHg", null },
                    { 3, "SPO2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Percentage of oxygen-saturated hemoglobin in peripheral blood", "Blood Oxygen Saturation", "%", null },
                    { 4, "TEMP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deep tissue core thermal measurement", "Core Body Temperature", "°C", null },
                    { 5, "GLUCOSE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Concentration of glucose in peripheral capillary blood", "Blood Glucose", "mg/dL", null },
                    { 6, "RESP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Number of ventilation/breath cycles completed per minute", "Respiratory Rate", "breaths/min", null }
                });

            migrationBuilder.InsertData(
                table: "ConditionSymptoms",
                columns: new[] { "Id", "ConditionId", "CreatedAt", "SymptomId", "UpdatedAt", "Weight" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, 1 },
                    { 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, 1 },
                    { 3, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 1 },
                    { 4, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, 1 },
                    { 5, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, 1 },
                    { 6, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, null, 1 },
                    { 7, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 18, null, 1 },
                    { 8, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, null, 1 },
                    { 9, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, 1 },
                    { 10, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, 1 },
                    { 11, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, null, 1 },
                    { 12, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, 1 },
                    { 13, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, null, 1 },
                    { 14, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 11, null, 1 },
                    { 15, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, null, 1 },
                    { 16, 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 11, null, 1 },
                    { 17, 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, null, 1 },
                    { 18, 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 13, null, 1 },
                    { 19, 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, null, 1 },
                    { 20, 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, null, 1 },
                    { 21, 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, 1 },
                    { 22, 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 14, null, 1 },
                    { 23, 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 15, null, 1 },
                    { 24, 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, 1 },
                    { 25, 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 16, null, 1 },
                    { 26, 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, 1 },
                    { 27, 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 15, null, 1 },
                    { 28, 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 17, null, 1 },
                    { 29, 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, 1 }
                });

            migrationBuilder.InsertData(
                table: "ConditionVitalSigns",
                columns: new[] { "Id", "ConditionId", "ContextNote", "CreatedAt", "Direction", "UpdatedAt", "VitalSignTypeId" },
                values: new object[,]
                {
                    { 1, 1, "mainly when standing", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Low", null, 2 },
                    { 2, 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Either", null, 1 },
                    { 3, 3, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Either", null, 6 },
                    { 4, 4, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "High", null, 1 },
                    { 5, 7, "interpret with meal context", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Either", null, 5 },
                    { 6, 8, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Either", null, 4 },
                    { 7, 9, "temperature alone cannot show immune status", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "High", null, 4 }
                });

            migrationBuilder.InsertData(
                table: "HealthMetricGuides",
                columns: new[] { "Id", "ActionGuidance", "CreatedAt", "HowToMeasure", "InterpretationGuidance", "UpdatedAt", "VitalSignTypeId", "WhatIsIt", "WhyItMatters" },
                values: new object[,]
                {
                    { 1, "SAMPLE: If elevated or irregular at rest, rest quietly, drink water per your fluid plan, and re-measure. Contact your monitor if it remains abnormal.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Wear the biometric sensor snugly against your skin or use the pulse oximeter while resting quietly.", "Resting rate between 60 and 100 bpm is the Earth general reference. Higher rates may occur after exercise or stress; lower rates can occur during rest.", null, 1, "Heart rate measures how many times your heart beats in one minute.", "May be associated with cardiovascular adaptation to microgravity and recovery from physical work." },
                    { 2, "SAMPLE: If outside your expected range, rest quietly and re-measure, recording your body position. Check your hydration and report persistent changes to your monitor.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Fit the automated cuff on your upper arm at heart level while supported. Remain still and quiet during the measurement.", "Standard Earth reference is 90-120 systolic over 60-80 diastolic. Higher readings may occur after exertion; lower readings can cause lightheadedness.", null, 2, "Blood pressure measures the pressure in your blood vessels during and between heart beats (systolic and diastolic).", "May be associated with fluid shifts toward the head and changes in circulatory regulation in space." },
                    { 3, "SAMPLE: Take calm, deep breaths and reposition the sensor. If the reading remains below 95%, check cabin oxygen levels and notify your monitor immediately.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Place the sensor on a clean, warm fingertip and keep your hand still until the reading stabilizes.", "Standard Earth reference is 95% to 100%. Readings below 95% may indicate shallow breathing, cold extremities, or low ambient oxygen.", null, 3, "Oxygen saturation (SpO2) measures the percentage of oxygen carried in your blood.", "Helps verify adequate oxygen delivery from cabin air or life support equipment." },
                    { 4, "SAMPLE: Note whether you were resting or had just exercised. Check the sensor, adjust clothing, hydrate, and re-measure. Report persistent fever to your monitor.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Use the approved tympanic ear thermometer or forehead sensor according to device instructions.", "Standard Earth reference is 36.5°C to 37.3°C. Body temperature can rise during or immediately after exercise.", null, 4, "Body temperature measures internal thermal energy.", "May be associated with infection, inflammation, heavy physical exertion, or ambient heat stress." },
                    { 5, "SAMPLE: Check your meal timing and whether you are fasting. Re-measure and follow your physician's nutrition and glucose management plan.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Use a clean test strip in the glucometer with a capillary blood drop, always noting meal context.", "Standard Earth fasting reference is 70 to 99 mg/dL. Values naturally rise after eating. Must always be interpreted with meal context.", null, 5, "Blood glucose measures the concentration of sugar in your blood.", "May be associated with energy availability and metabolic changes in spaceflight." },
                    { 6, "SAMPLE: If breathing feels fast or labored at rest, check cabin air readings, ensure you are in an area with good air circulation, and contact your monitor.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rest quietly and count your breathing cycles over 60 seconds, or use the biometric chest sensor.", "Standard Earth reference is 12 to 18 breaths per minute. Elevated rates can occur with physical effort or elevated cabin CO2.", null, 6, "Respiratory rate is the number of breaths you take in one minute.", "May be associated with physical exertion, cabin air quality, or carbon dioxide levels." }
                });

            migrationBuilder.InsertData(
                table: "ReferenceRanges",
                columns: new[] { "Id", "ApplicableGender", "CreatedAt", "IsActive", "IsMissionApproved", "MaxAge", "MaxEmergency", "MaxNormal", "MaxSecondaryEmergency", "MaxSecondaryNormal", "MaxSecondaryWarning", "MaxWarning", "MinAge", "MinEmergency", "MinNormal", "MinSecondaryEmergency", "MinSecondaryNormal", "MinSecondaryWarning", "MinWarning", "Notes", "ReviewedAt", "ReviewedBy", "Scope", "Source", "UpdatedAt", "VitalSignTypeId" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, 140.0, 100.0, null, null, null, 110.0, null, 40.0, 60.0, null, null, null, 50.0, "PLACEHOLDER - requires review by authorized staff", null, null, "EarthGeneral", "Terrestrial Clinical Standards", null, 1 },
                    { 2, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, 160.0, 120.0, 100.0, 80.0, 89.0, 139.0, null, 70.0, 90.0, 45.0, 60.0, 50.0, 80.0, "PLACEHOLDER - requires review by authorized staff", null, null, "EarthGeneral", "Terrestrial Clinical Standards", null, 2 },
                    { 3, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, 89.0, 100.0, null, null, null, 94.0, null, null, 95.0, null, null, null, 90.0, "PLACEHOLDER - requires review by authorized staff", null, null, "EarthGeneral", "Terrestrial Clinical Standards", null, 3 },
                    { 4, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, 38.5, 37.299999999999997, null, null, null, 37.799999999999997, null, 35.0, 36.5, null, null, null, 35.5, "PLACEHOLDER - requires review by authorized staff", null, null, "EarthGeneral", "Terrestrial Clinical Standards", null, 4 },
                    { 5, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, 200.0, 99.0, null, null, null, 125.0, null, 50.0, 70.0, null, null, null, 60.0, "PLACEHOLDER - requires review by authorized staff. Fasting only (up to 99 mg/dL); post-meal reference ranges are not established in source.", null, null, "EarthGeneral", "Terrestrial Clinical Standards", null, 5 },
                    { 6, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, 30.0, 18.0, null, null, null, 24.0, null, 8.0, 12.0, null, null, null, 10.0, "PLACEHOLDER - requires review by authorized staff", null, null, "EarthGeneral", "Terrestrial Clinical Standards", null, 6 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Astronauts_AstronautCode",
                table: "Astronauts",
                column: "AstronautCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Astronauts_FullName",
                table: "Astronauts",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Astronauts_MissionId",
                table: "Astronauts",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Astronauts_UserId",
                table: "Astronauts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_AstronautId_SentAt",
                table: "ChatMessages",
                columns: new[] { "AstronautId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ClientGeneratedId",
                table: "ChatMessages",
                column: "ClientGeneratedId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConditionSymptoms_ConditionId_SymptomId",
                table: "ConditionSymptoms",
                columns: new[] { "ConditionId", "SymptomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConditionSymptoms_SymptomId",
                table: "ConditionSymptoms",
                column: "SymptomId");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionVitalSigns_ConditionId_VitalSignTypeId",
                table: "ConditionVitalSigns",
                columns: new[] { "ConditionId", "VitalSignTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConditionVitalSigns_VitalSignTypeId",
                table: "ConditionVitalSigns",
                column: "VitalSignTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyProcedures_Code",
                table: "EmergencyProcedures",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthAlerts_AstronautId_Status_TriggeredAt",
                table: "HealthAlerts",
                columns: new[] { "AstronautId", "Status", "TriggeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthAlerts_EmergencyProcedureId",
                table: "HealthAlerts",
                column: "EmergencyProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthAlerts_VitalSignReadingId",
                table: "HealthAlerts",
                column: "VitalSignReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthMetricGuides_VitalSignTypeId",
                table: "HealthMetricGuides",
                column: "VitalSignTypeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Missions_Code",
                table: "Missions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MoodEntries_AstronautId_RecordedAt",
                table: "MoodEntries",
                columns: new[] { "AstronautId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MoodEntries_ClientGeneratedId",
                table: "MoodEntries",
                column: "ClientGeneratedId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_AstronautId_Status",
                table: "Recommendations",
                columns: new[] { "AstronautId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceRanges_VitalSignTypeId_Scope_IsActive",
                table: "ReferenceRanges",
                columns: new[] { "VitalSignTypeId", "Scope", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SleepRecords_AstronautId_RecordedAt",
                table: "SleepRecords",
                columns: new[] { "AstronautId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SleepRecords_ClientGeneratedId",
                table: "SleepRecords",
                column: "ClientGeneratedId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpaceConditions_Code",
                table: "SpaceConditions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Symptoms_Code",
                table: "Symptoms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignReadings_AstronautId_RecordedAt",
                table: "VitalSignReadings",
                columns: new[] { "AstronautId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignReadings_ClientGeneratedId",
                table: "VitalSignReadings",
                column: "ClientGeneratedId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignReadings_VitalSignTypeId",
                table: "VitalSignReadings",
                column: "VitalSignTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignTypes_Code",
                table: "VitalSignTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ConditionSymptoms");

            migrationBuilder.DropTable(
                name: "ConditionVitalSigns");

            migrationBuilder.DropTable(
                name: "HealthAlerts");

            migrationBuilder.DropTable(
                name: "HealthMetricGuides");

            migrationBuilder.DropTable(
                name: "MoodEntries");

            migrationBuilder.DropTable(
                name: "Recommendations");

            migrationBuilder.DropTable(
                name: "ReferenceRanges");

            migrationBuilder.DropTable(
                name: "SleepRecords");

            migrationBuilder.DropTable(
                name: "Symptoms");

            migrationBuilder.DropTable(
                name: "SpaceConditions");

            migrationBuilder.DropTable(
                name: "EmergencyProcedures");

            migrationBuilder.DropTable(
                name: "VitalSignReadings");

            migrationBuilder.DropTable(
                name: "Astronauts");

            migrationBuilder.DropTable(
                name: "VitalSignTypes");

            migrationBuilder.DropTable(
                name: "Missions");
        }
    }
}
