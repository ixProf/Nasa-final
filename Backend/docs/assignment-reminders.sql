START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113035038_AddAutomaticAssignmentReminders') THEN
    CREATE TABLE "AssignmentReminderDeliveries" (
        "RecommendationId" integer NOT NULL,
        "Token" uuid NOT NULL,
        "SentAt" timestamp with time zone NOT NULL,
        "AcknowledgedAt" timestamp with time zone,
        "IsManual" boolean NOT NULL,
        "SoundEnabled" boolean NOT NULL,
        "SenderName" character varying(255),
        CONSTRAINT "PK_AssignmentReminderDeliveries" PRIMARY KEY ("RecommendationId"),
        CONSTRAINT "FK_AssignmentReminderDeliveries_Recommendations_Recommendation~" FOREIGN KEY ("RecommendationId") REFERENCES "Recommendations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113035038_AddAutomaticAssignmentReminders') THEN
    CREATE TABLE "AssignmentReminderSettings" (
        "RecommendationId" integer NOT NULL,
        "Enabled" boolean NOT NULL,
        "IntervalMinutes" integer NOT NULL,
        "SoundEnabled" boolean NOT NULL,
        "NextDueAt" timestamp with time zone,
        "Revision" uuid NOT NULL,
        CONSTRAINT "PK_AssignmentReminderSettings" PRIMARY KEY ("RecommendationId"),
        CONSTRAINT "FK_AssignmentReminderSettings_Recommendations_RecommendationId" FOREIGN KEY ("RecommendationId") REFERENCES "Recommendations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113035038_AddAutomaticAssignmentReminders') THEN
    CREATE UNIQUE INDEX "IX_AssignmentReminderDeliveries_Token" ON "AssignmentReminderDeliveries" ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113035038_AddAutomaticAssignmentReminders') THEN
    CREATE INDEX "IX_AssignmentReminderSettings_Enabled_NextDueAt" ON "AssignmentReminderSettings" ("Enabled", "NextDueAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113035038_AddAutomaticAssignmentReminders') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261113035038_AddAutomaticAssignmentReminders', '9.0.2');
    END IF;
END $EF$;
COMMIT;

