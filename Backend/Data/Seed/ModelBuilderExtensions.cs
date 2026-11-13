using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Data.Seed;

public static class ModelBuilderExtensions
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        var seedTimeUtc = DateTime.SpecifyKind(new DateTime(2026, 1, 1, 0, 0, 0), DateTimeKind.Utc);

        // -------------------------------------------------------------
        // 1. VitalSignTypes (Catalog of 6 metrics)
        // -------------------------------------------------------------
        modelBuilder.Entity<VitalSignType>().HasData(
            new VitalSignType
            {
                Id = 1,
                Code = "HR",
                Name = "Heart Rate",
                Unit = "bpm",
                Description = "Number of cardiac contractions per minute",
                CreatedAt = seedTimeUtc
            },
            new VitalSignType
            {
                Id = 2,
                Code = "BP",
                Name = "Blood Pressure",
                Unit = "mmHg",
                Description = "Arterial vascular pressure (Systolic / Diastolic)",
                CreatedAt = seedTimeUtc
            },
            new VitalSignType
            {
                Id = 3,
                Code = "SPO2",
                Name = "Blood Oxygen Saturation",
                Unit = "%",
                Description = "Percentage of oxygen-saturated hemoglobin in peripheral blood",
                CreatedAt = seedTimeUtc
            },
            new VitalSignType
            {
                Id = 4,
                Code = "TEMP",
                Name = "Core Body Temperature",
                Unit = "°C",
                Description = "Deep tissue core thermal measurement",
                CreatedAt = seedTimeUtc
            },
            new VitalSignType
            {
                Id = 5,
                Code = "GLUCOSE",
                Name = "Blood Glucose",
                Unit = "mg/dL",
                Description = "Concentration of glucose in peripheral capillary blood",
                CreatedAt = seedTimeUtc
            },
            new VitalSignType
            {
                Id = 6,
                Code = "RESP",
                Name = "Respiratory Rate",
                Unit = "breaths/min",
                Description = "Number of ventilation/breath cycles completed per minute",
                CreatedAt = seedTimeUtc
            }
        );

        // -------------------------------------------------------------
        // 2. ReferenceRanges (EarthGeneral, Unreviewed, Placeholder thresholds)
        // -------------------------------------------------------------
        modelBuilder.Entity<ReferenceRange>().HasData(
            // Heart Rate: Normal 60-100 bpm
            new ReferenceRange
            {
                Id = 1,
                VitalSignTypeId = 1,
                MinNormal = 60,
                MaxNormal = 100,
                EmergencyBelow = 40,
                EmergencyAbove = 140,
                Source = "Terrestrial Clinical Standards",
                Scope = ReferenceRangeScope.EarthGeneral,
                IsMissionApproved = false,
                ReviewedBy = null,
                ReviewedAt = null,
                Notes = "PLACEHOLDER - requires review by authorized staff",
                IsActive = true,
                CreatedAt = seedTimeUtc
            },
            // Blood Pressure: Normal 90-120 systolic / 60-80 diastolic mmHg
            new ReferenceRange
            {
                Id = 2,
                VitalSignTypeId = 2,
                MinNormal = 90,
                MaxNormal = 120,
                EmergencyBelow = 70,
                EmergencyAbove = 160,
                MinSecondaryNormal = 60,
                MaxSecondaryNormal = 80,
                SecondaryEmergencyBelow = 45,
                SecondaryEmergencyAbove = 100,
                Source = "Terrestrial Clinical Standards",
                Scope = ReferenceRangeScope.EarthGeneral,
                IsMissionApproved = false,
                ReviewedBy = null,
                ReviewedAt = null,
                Notes = "PLACEHOLDER - requires review by authorized staff",
                IsActive = true,
                CreatedAt = seedTimeUtc
            },
            // SpO2: Normal 95-100 % (Emergency: < 90%)
            new ReferenceRange
            {
                Id = 3,
                VitalSignTypeId = 3,
                MinNormal = 95,
                MaxNormal = 100,
                EmergencyBelow = 90,
                EmergencyAbove = null,
                Source = "Terrestrial Clinical Standards",
                Scope = ReferenceRangeScope.EarthGeneral,
                IsMissionApproved = false,
                ReviewedBy = null,
                ReviewedAt = null,
                Notes = "PLACEHOLDER - requires review by authorized staff",
                IsActive = true,
                CreatedAt = seedTimeUtc
            },
            // Temperature: Normal 36.5-37.3 °C
            new ReferenceRange
            {
                Id = 4,
                VitalSignTypeId = 4,
                MinNormal = 36.5,
                MaxNormal = 37.3,
                EmergencyBelow = 35.0,
                EmergencyAbove = 38.5,
                Source = "Terrestrial Clinical Standards",
                Scope = ReferenceRangeScope.EarthGeneral,
                IsMissionApproved = false,
                ReviewedBy = null,
                ReviewedAt = null,
                Notes = "PLACEHOLDER - requires review by authorized staff",
                IsActive = true,
                CreatedAt = seedTimeUtc
            },
            // Blood Glucose: Fasting Normal up to 99 mg/dL (70 - 99 mg/dL)
            new ReferenceRange
            {
                Id = 5,
                VitalSignTypeId = 5,
                MinNormal = 70,
                MaxNormal = 99,
                EmergencyBelow = 50,
                EmergencyAbove = 200,
                Source = "Terrestrial Clinical Standards",
                Scope = ReferenceRangeScope.EarthGeneral,
                IsMissionApproved = false,
                ReviewedBy = null,
                ReviewedAt = null,
                Notes = "PLACEHOLDER - requires review by authorized staff. 70 is a PLACEHOLDER; fasting reference up to 99 mg/dL.",
                IsActive = true,
                CreatedAt = seedTimeUtc
            },
            // Respiratory Rate: Normal 12-18 breaths/min
            new ReferenceRange
            {
                Id = 6,
                VitalSignTypeId = 6,
                MinNormal = 12,
                MaxNormal = 18,
                EmergencyBelow = 8,
                EmergencyAbove = 30,
                Source = "Terrestrial Clinical Standards",
                Scope = ReferenceRangeScope.EarthGeneral,
                IsMissionApproved = false,
                ReviewedBy = null,
                ReviewedAt = null,
                Notes = "PLACEHOLDER - requires review by authorized staff",
                IsActive = true,
                CreatedAt = seedTimeUtc
            }
        );

        // -------------------------------------------------------------
        // 3. HealthMetricGuides (5 Core Questions, sample non-diagnostic)
        // -------------------------------------------------------------
        modelBuilder.Entity<HealthMetricGuide>().HasData(
            new HealthMetricGuide
            {
                Id = 1,
                VitalSignTypeId = 1,
                WhatIsIt = "Heart rate measures how many times your heart beats in one minute.",
                WhyItMatters = "May be associated with cardiovascular adaptation to microgravity and recovery from physical work.",
                HowToMeasure = "Wear the biometric sensor snugly against your skin or use the pulse oximeter while resting quietly.",
                InterpretationGuidance = "Resting rate between 60 and 100 bpm is the Earth general reference. Higher rates may occur after exercise or stress; lower rates can occur during rest.",
                ActionGuidance = "SAMPLE: If elevated or irregular at rest, rest quietly, drink water per your fluid plan, and re-measure. Contact your monitor if it remains abnormal.",
                CreatedAt = seedTimeUtc
            },
            new HealthMetricGuide
            {
                Id = 2,
                VitalSignTypeId = 2,
                WhatIsIt = "Blood pressure measures the pressure in your blood vessels during and between heart beats (systolic and diastolic).",
                WhyItMatters = "May be associated with fluid shifts toward the head and changes in circulatory regulation in space.",
                HowToMeasure = "Fit the automated cuff on your upper arm at heart level while supported. Remain still and quiet during the measurement.",
                InterpretationGuidance = "Standard Earth reference is 90-120 systolic over 60-80 diastolic. Higher readings may occur after exertion; lower readings can cause lightheadedness.",
                ActionGuidance = "SAMPLE: If outside your expected range, rest quietly and re-measure, recording your body position. Check your hydration and report persistent changes to your monitor.",
                CreatedAt = seedTimeUtc
            },
            new HealthMetricGuide
            {
                Id = 3,
                VitalSignTypeId = 3,
                WhatIsIt = "Oxygen saturation (SpO2) measures the percentage of oxygen carried in your blood.",
                WhyItMatters = "Helps verify adequate oxygen delivery from cabin air or life support equipment.",
                HowToMeasure = "Place the sensor on a clean, warm fingertip and keep your hand still until the reading stabilizes.",
                InterpretationGuidance = "Standard Earth reference is 95% to 100%. Readings below 95% may indicate shallow breathing, cold extremities, or low ambient oxygen.",
                ActionGuidance = "SAMPLE: Reposition the sensor. If the reading remains below 95%, check cabin oxygen levels and notify your monitor immediately.",
                CreatedAt = seedTimeUtc
            },
            new HealthMetricGuide
            {
                Id = 4,
                VitalSignTypeId = 4,
                WhatIsIt = "Body temperature measures internal thermal energy.",
                WhyItMatters = "May be associated with infection, inflammation, heavy physical exertion, or ambient heat stress.",
                HowToMeasure = "Use the approved tympanic ear thermometer or forehead sensor according to device instructions.",
                InterpretationGuidance = "Standard Earth reference is 36.5°C to 37.3°C. Body temperature can rise during or immediately after exercise.",
                ActionGuidance = "SAMPLE: Note whether you were resting or had just exercised. Check the sensor, hydrate, and re-measure. Report persistent fever to your monitor.",
                CreatedAt = seedTimeUtc
            },
            new HealthMetricGuide
            {
                Id = 5,
                VitalSignTypeId = 5,
                WhatIsIt = "Blood glucose measures the concentration of sugar in your blood.",
                WhyItMatters = "May be associated with energy availability and metabolic changes in spaceflight.",
                HowToMeasure = "Use a clean test strip in the glucometer with a capillary blood drop, always noting meal context.",
                InterpretationGuidance = "The research gives an Earth fasting reference of up to 99 mg/dL. Values naturally rise after eating. Always interpret with meal context.",
                ActionGuidance = "SAMPLE: Check your meal timing and whether you are fasting. Re-measure and follow your physician's nutrition and glucose management plan.",
                CreatedAt = seedTimeUtc
            },
            new HealthMetricGuide
            {
                Id = 6,
                VitalSignTypeId = 6,
                WhatIsIt = "Respiratory rate is the number of breaths you take in one minute.",
                WhyItMatters = "May be associated with physical exertion, cabin air quality, or carbon dioxide levels.",
                HowToMeasure = "Rest quietly and count your breathing cycles over 60 seconds, or use the biometric chest sensor.",
                InterpretationGuidance = "Standard Earth reference is 12 to 18 breaths per minute. Elevated rates can occur with physical effort or elevated cabin CO2.",
                ActionGuidance = "SAMPLE: If breathing feels fast or labored at rest, check cabin air readings and contact your monitor.",
                CreatedAt = seedTimeUtc
            }
        );

        // -------------------------------------------------------------
        // 4. EmergencyProcedures (Sample content pending review)
        // -------------------------------------------------------------
        modelBuilder.Entity<EmergencyProcedure>().HasData(
            new EmergencyProcedure
            {
                Id = 1,
                Code = "EP-HYPOXIA-01",
                Title = "Suspected Low Oxygen (Hypoxia) Response",
                TargetCondition = "SpO2 reading below 90% or unexplained lightheadedness / confusion",
                Severity = EmergencySeverity.Critical,
                ImmediateAction = "Immediately don portable emergency oxygen mask and verify positive flow indicator.",
                StepByStepInstructions = "1. Don the nearest portable breathing apparatus (PBA).\n2. Verify the oxygen regulator is set to 100% flow.\n3. Inform Commander and Ground Monitor via voice loop.\n4. Check environmental console for cabin partial oxygen pressure.\n5. Remain stationary and secured until SpO2 stabilizes above 95%.",
                RequiredEquipment = "Portable Breathing Apparatus (PBA), Pulse Oximeter",
                GuidanceNotes = "SAMPLE CONTENT - Pending formal review by authorized flight operations staff.",
                CreatedAt = seedTimeUtc
            },
            new EmergencyProcedure
            {
                Id = 2,
                Code = "EP-CO2-02",
                Title = "Elevated Cabin CO2 / Headache Protocol",
                TargetCondition = "Elevated respiratory rate (>18 breaths/min) with persistent headache",
                Severity = EmergencySeverity.Moderate,
                ImmediateAction = "Move immediately toward an area with active cabin ventilation fans.",
                StepByStepInstructions = "1. Relocate away from enclosed dead-air corners to a central ventilation duct.\n2. Confirm local air circulation fan is running.\n3. Report readings and symptoms to Ground Monitor.\n4. Check environmental console for CO2 scrubber status.",
                RequiredEquipment = "Environmental console, personal air sampler",
                GuidanceNotes = "SAMPLE CONTENT - Pending formal review by authorized flight operations staff.",
                CreatedAt = seedTimeUtc
            }
        );

        // -------------------------------------------------------------
        // 5. Symptoms (Standardized, exactly 3 Red Flags)
        // -------------------------------------------------------------
        modelBuilder.Entity<Symptom>().HasData(
            new Symptom { Id = 1, Code = "DIZZINESS", Name = "Dizziness / Lightheadedness", BodyArea = "Head / Neurological", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 2, Code = "FAINTING", Name = "Fainting / Syncope", BodyArea = "Systemic / Cardiovascular", IsRedFlag = true, CreatedAt = seedTimeUtc },
            new Symptom { Id = 3, Code = "CHEST_PAIN", Name = "Chest Pain / Tightness", BodyArea = "Cardiovascular / Chest", IsRedFlag = true, CreatedAt = seedTimeUtc },
            new Symptom { Id = 4, Code = "DIFFICULTY_BREATHING", Name = "Difficulty Breathing / Shortness of Breath", BodyArea = "Respiratory", IsRedFlag = true, CreatedAt = seedTimeUtc },
            new Symptom { Id = 5, Code = "BLURRED_VISION", Name = "Blurred Vision / Visual Acuity Decrease", BodyArea = "Visual / Ocular", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 6, Code = "HEADACHE", Name = "Headache", BodyArea = "Head / Neurological", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 7, Code = "FATIGUE", Name = "Fatigue / General Exhaustion", BodyArea = "Systemic", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 8, Code = "DIFFICULTY_CONCENTRATING", Name = "Difficulty Concentrating / Mental Fog", BodyArea = "Cognitive / Neurological", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 9, Code = "DAYTIME_SLEEPINESS", Name = "Daytime Sleepiness / Excessive Somnolence", BodyArea = "Systemic", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 10, Code = "POOR_SLEEP", Name = "Poor Sleep Quality / Frequent Awakenings", BodyArea = "Systemic", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 11, Code = "INCREASED_STRESS", Name = "Increased Stress / Subjective Tension", BodyArea = "Psychological / Emotional", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 12, Code = "MOOD_CHANGES", Name = "Mood Changes / Irritability", BodyArea = "Psychological / Emotional", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 13, Code = "LONELINESS", Name = "Feelings of Isolation / Loneliness", BodyArea = "Psychological / Emotional", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 14, Code = "TREMOR", Name = "Tremor / Muscle Shaking", BodyArea = "Neurological / Musculoskeletal", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 15, Code = "SWEATING", Name = "Unusual Sweating / Diaphoresis", BodyArea = "Systemic / Autonomic", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 16, Code = "CHILLS", Name = "Chills / Shivering", BodyArea = "Systemic / Thermal", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 17, Code = "SKIN_SYMPTOMS", Name = "Skin Irritation / Rash / Pruritus", BodyArea = "Skin / Dermatological", IsRedFlag = false, CreatedAt = seedTimeUtc },
            new Symptom { Id = 18, Code = "RESPIRATORY_IRRITATION", Name = "Respiratory Irritation / Cough / Congestion", BodyArea = "Respiratory", IsRedFlag = false, CreatedAt = seedTimeUtc }
        );

        // -------------------------------------------------------------
        // 6. SpaceConditions (Exact SourceReferences and Research-Grounded Text)
        // -------------------------------------------------------------
        const string defaultWhenToContact = "Contact a monitor if symptoms are new, persistent, worsening, or readings are repeatedly abnormal.";
        const string defaultSampleNote = "SAMPLE CONTENT - pending staff review";

        modelBuilder.Entity<SpaceCondition>().HasData(
            new SpaceCondition
            {
                Id = 1,
                Code = "ORTHOSTATIC_INTOLERANCE",
                Name = "Orthostatic Intolerance",
                Description = "May be associated with changes in how body fluids are distributed and how the circulatory system adapts to microgravity, especially when returning to gravity.",
                SelfCareGuidance = "Keep up the mission physical activity program, re-measure blood pressure and record your body position, and report to your monitor.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 3.1, 4.1",
                IsMatchable = true,
                Notes = defaultSampleNote,
                CreatedAt = seedTimeUtc
            },
            new SpaceCondition
            {
                Id = 2,
                Code = "SPACEFLIGHT_VISION_CHANGES",
                Name = "Spaceflight-Associated Neuro-ocular Changes",
                Description = "May be associated with the shift of body fluids toward the head in microgravity.",
                SelfCareGuidance = "Report the symptom to your monitor and maintain scheduled eye examinations from the mission plan. Note that this cannot be evaluated from vital signs.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 2.4, 4.4 (NASA: Risk of SANS)",
                IsMatchable = true,
                Notes = "SAMPLE CONTENT - pending staff review. Cannot be identified from symptoms or vital signs; needs specialized eye exams.",
                CreatedAt = seedTimeUtc
            },
            new SpaceCondition
            {
                Id = 3,
                Code = "CLOSED_ENVIRONMENT_AIR_QUALITY",
                Name = "Closed Environment Air Quality Effects",
                Description = "May be associated with carbon dioxide levels or other air quality factors in a closed cabin.",
                SelfCareGuidance = "Tell a monitor and check cabin environment readings.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 2.5",
                IsMatchable = true,
                Notes = "SAMPLE CONTENT - pending staff review. Note that CO2 level is environmental data, not a vital sign. Self-care guidance is a placeholder (source does not specify).",
                CreatedAt = seedTimeUtc
            },
            new SpaceCondition
            {
                Id = 4,
                Code = "DEHYDRATION_NUTRITION",
                Name = "Hydration and Nutrition Depletion",
                Description = "May be associated with stored food, nutritional needs, physical activity, and mission conditions that affect fluid and energy balance.",
                SelfCareGuidance = "Follow the mission nutrition plan and monitor your fluid intake. Report dizziness or fatigue to your monitor.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 4.8, 3.2",
                IsMatchable = true,
                Notes = defaultSampleNote,
                CreatedAt = seedTimeUtc
            },
            new SpaceCondition
            {
                Id = 5,
                Code = "SLEEP_DEFICIENCY",
                Name = "Sleep Deficiency",
                Description = "May be associated with isolation, stress, day/night cycle changes, work schedule, light and noise.",
                SelfCareGuidance = "Keep regular sleep and wake times and reduce things that disrupt sleep, such as light and noise.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 3.7, 4.6",
                IsMatchable = true,
                Notes = defaultSampleNote,
                CreatedAt = seedTimeUtc
            },
            new SpaceCondition
            {
                Id = 6,
                Code = "STRESS_ISOLATION",
                Name = "Stress and Mission Isolation",
                Description = "May be associated with long periods in a small space, distance from family and familiar surroundings, and continuous contact with the same crew.",
                SelfCareGuidance = "Stay in social contact with your crew, complete your daily self-assessment, try relaxation exercises, and contact support staff.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 2.2, 3.8, 4.6",
                IsMatchable = true,
                Notes = "SAMPLE CONTENT - pending staff review. Never label as depression or anxiety.",
                CreatedAt = seedTimeUtc
            },
            new SpaceCondition
            {
                Id = 7,
                Code = "BLOOD_SUGAR_CHANGE",
                Name = "Blood Glucose Variation",
                Description = "May be associated with changes in glucose metabolism during spaceflight.",
                SelfCareGuidance = "Check meal timing, re-measure, and follow the physician's plan.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 3.3",
                IsMatchable = true,
                Notes = defaultSampleNote,
                CreatedAt = seedTimeUtc
            },
            new SpaceCondition
            {
                Id = 8,
                Code = "TEMPERATURE_CHANGE",
                Name = "Thermal Regulation Variation",
                Description = "May be associated with infection, inflammation, heat stress, exercise, or measurement conditions.",
                SelfCareGuidance = "Check the sensor and measurement conditions, note whether you were resting or had just exercised, and record your symptoms.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 3.4",
                IsMatchable = true,
                Notes = defaultSampleNote,
                CreatedAt = seedTimeUtc
            },
            new SpaceCondition
            {
                Id = 9,
                Code = "IMMUNE_SKIN_CHANGES",
                Name = "Immune Dysregulation and Skin Changes",
                Description = "May be associated with changes in the immune system or skin during spaceflight.",
                SelfCareGuidance = "Record symptoms, follow hygiene and prevention procedures, and seek medical follow-up.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 4.5",
                IsMatchable = true,
                Notes = "SAMPLE CONTENT - pending staff review. Note that temperature alone cannot show immune status.",
                CreatedAt = seedTimeUtc
            },
            // Knowledge-only conditions (IsMatchable = false, no direct symptom links)
            new SpaceCondition
            {
                Id = 10,
                Code = "BONE_LOSS",
                Name = "Bone Mineral Density Loss",
                Description = "May be associated with reduced mechanical load on weight-bearing bones in low gravity.",
                SelfCareGuidance = "Follow the mission physical activity program (resistance and aerobic exercise). Assessed by specialized mission exams, not by daily wearables.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 4.2",
                IsMatchable = false,
                Notes = "SAMPLE CONTENT - pending staff review. Knowledge-only; cannot be evaluated from wearables.",
                CreatedAt = seedTimeUtc
            },
            new SpaceCondition
            {
                Id = 11,
                Code = "MUSCLE_ATROPHY",
                Name = "Muscle Mass and Strength Loss",
                Description = "May be associated with reduced use of the muscles that work against gravity.",
                SelfCareGuidance = "Follow the mission physical activity program (resistance and aerobic exercise). Assessed by specialized mission exams, not by daily wearables.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 4.3",
                IsMatchable = false,
                Notes = "SAMPLE CONTENT - pending staff review. Knowledge-only; cannot be evaluated from wearables.",
                CreatedAt = seedTimeUtc
            },
            new SpaceCondition
            {
                Id = 12,
                Code = "VENOUS_THROMBOEMBOLISM",
                Name = "Internal Jugular Venous Flow Alteration / Thrombosis Risk",
                Description = "May be associated with changes in blood flow in the neck (jugular) veins during spaceflight, including slowed, stagnant or reversed flow.",
                SelfCareGuidance = "Requires specialized ultrasound interpreted by medical staff; cannot be detected from heart rate, blood pressure, or self-reported symptoms.",
                WhenToContactMonitor = defaultWhenToContact,
                SourceReference = "Research doc 4.7",
                IsMatchable = false,
                Notes = "SAMPLE CONTENT - pending staff review. Knowledge-only; cannot be evaluated from wearables or self-reported symptoms.",
                CreatedAt = seedTimeUtc
            }
        );

        // -------------------------------------------------------------
        // 7. ConditionSymptoms (Associative mappings)
        // -------------------------------------------------------------
        modelBuilder.Entity<ConditionSymptom>().HasData(
            // ORTHOSTATIC_INTOLERANCE (Id 1) -> DIZZINESS (1), FAINTING (2)
            new ConditionSymptom { Id = 1, ConditionId = 1, SymptomId = 1, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 2, ConditionId = 1, SymptomId = 2, Weight = 1, CreatedAt = seedTimeUtc },

            // SPACEFLIGHT_VISION_CHANGES (Id 2) -> BLURRED_VISION (5)
            new ConditionSymptom { Id = 3, ConditionId = 2, SymptomId = 5, Weight = 1, CreatedAt = seedTimeUtc },

            // CLOSED_ENVIRONMENT_AIR_QUALITY (Id 3) -> HEADACHE (6), FATIGUE (7), DIFFICULTY_CONCENTRATING (8), RESPIRATORY_IRRITATION (18), POOR_SLEEP (10)
            new ConditionSymptom { Id = 4, ConditionId = 3, SymptomId = 6, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 5, ConditionId = 3, SymptomId = 7, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 6, ConditionId = 3, SymptomId = 8, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 7, ConditionId = 3, SymptomId = 18, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 8, ConditionId = 3, SymptomId = 10, Weight = 1, CreatedAt = seedTimeUtc },

            // DEHYDRATION_NUTRITION (Id 4) -> DIZZINESS (1), FATIGUE (7)
            new ConditionSymptom { Id = 9, ConditionId = 4, SymptomId = 1, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 10, ConditionId = 4, SymptomId = 7, Weight = 1, CreatedAt = seedTimeUtc },

            // SLEEP_DEFICIENCY (Id 5) -> DAYTIME_SLEEPINESS (9), FATIGUE (7), DIFFICULTY_CONCENTRATING (8), INCREASED_STRESS (11), POOR_SLEEP (10)
            new ConditionSymptom { Id = 11, ConditionId = 5, SymptomId = 9, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 12, ConditionId = 5, SymptomId = 7, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 13, ConditionId = 5, SymptomId = 8, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 14, ConditionId = 5, SymptomId = 11, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 15, ConditionId = 5, SymptomId = 10, Weight = 1, CreatedAt = seedTimeUtc },

            // STRESS_ISOLATION (Id 6) -> INCREASED_STRESS (11), MOOD_CHANGES (12), LONELINESS (13), POOR_SLEEP (10), DIFFICULTY_CONCENTRATING (8), FATIGUE (7)
            new ConditionSymptom { Id = 16, ConditionId = 6, SymptomId = 11, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 17, ConditionId = 6, SymptomId = 12, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 18, ConditionId = 6, SymptomId = 13, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 19, ConditionId = 6, SymptomId = 10, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 20, ConditionId = 6, SymptomId = 8, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 21, ConditionId = 6, SymptomId = 7, Weight = 1, CreatedAt = seedTimeUtc },

            // BLOOD_SUGAR_CHANGE (Id 7) -> TREMOR (14), SWEATING (15), DIZZINESS (1)
            new ConditionSymptom { Id = 22, ConditionId = 7, SymptomId = 14, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 23, ConditionId = 7, SymptomId = 15, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 24, ConditionId = 7, SymptomId = 1, Weight = 1, CreatedAt = seedTimeUtc },

            // TEMPERATURE_CHANGE (Id 8) -> CHILLS (16), FATIGUE (7), SWEATING (15)
            new ConditionSymptom { Id = 25, ConditionId = 8, SymptomId = 16, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 26, ConditionId = 8, SymptomId = 7, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 27, ConditionId = 8, SymptomId = 15, Weight = 1, CreatedAt = seedTimeUtc },

            // IMMUNE_SKIN_CHANGES (Id 9) -> SKIN_SYMPTOMS (17), FATIGUE (7)
            new ConditionSymptom { Id = 28, ConditionId = 9, SymptomId = 17, Weight = 1, CreatedAt = seedTimeUtc },
            new ConditionSymptom { Id = 29, ConditionId = 9, SymptomId = 7, Weight = 1, CreatedAt = seedTimeUtc }
        );

        // -------------------------------------------------------------
        // 8. ConditionVitalSigns (Associative vital deviations)
        // -------------------------------------------------------------
        modelBuilder.Entity<ConditionVitalSign>().HasData(
            // ORTHOSTATIC_INTOLERANCE (Id 1): BP (2) Low ("mainly when standing"), HR (1) Either
            new ConditionVitalSign { Id = 1, ConditionId = 1, VitalSignTypeId = 2, Direction = VitalSignDirection.Low, ContextNote = "mainly when standing", CreatedAt = seedTimeUtc },
            new ConditionVitalSign { Id = 2, ConditionId = 1, VitalSignTypeId = 1, Direction = VitalSignDirection.Either, ContextNote = null, CreatedAt = seedTimeUtc },

            // CLOSED_ENVIRONMENT_AIR_QUALITY (Id 3): RESP (6) Either
            new ConditionVitalSign { Id = 3, ConditionId = 3, VitalSignTypeId = 6, Direction = VitalSignDirection.Either, ContextNote = null, CreatedAt = seedTimeUtc },

            // DEHYDRATION_NUTRITION (Id 4): HR (1) High
            new ConditionVitalSign { Id = 4, ConditionId = 4, VitalSignTypeId = 1, Direction = VitalSignDirection.High, ContextNote = null, CreatedAt = seedTimeUtc },

            // BLOOD_SUGAR_CHANGE (Id 7): GLUCOSE (5) Either ("interpret with meal context")
            new ConditionVitalSign { Id = 5, ConditionId = 7, VitalSignTypeId = 5, Direction = VitalSignDirection.Either, ContextNote = "interpret with meal context", CreatedAt = seedTimeUtc },

            // TEMPERATURE_CHANGE (Id 8): TEMP (4) Either
            new ConditionVitalSign { Id = 6, ConditionId = 8, VitalSignTypeId = 4, Direction = VitalSignDirection.Either, ContextNote = null, CreatedAt = seedTimeUtc },

            // IMMUNE_SKIN_CHANGES (Id 9): TEMP (4) High (Note: temperature alone cannot show immune status)
            new ConditionVitalSign { Id = 7, ConditionId = 9, VitalSignTypeId = 4, Direction = VitalSignDirection.High, ContextNote = "temperature alone cannot show immune status", CreatedAt = seedTimeUtc }
        );
    }
}
