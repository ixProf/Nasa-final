# Astronaut Health Monitoring System - Project Status Report

> **Generated**: 2026-11-09  
> **Target Framework**: .NET 9.0 (`net9.0`) | **Database**: PostgreSQL (Supabase) via EF Core 9.0.2 / Npgsql 9.0.3  
> **Solution Status**: Build Clean (0 Warnings, 0 Errors) | **Automated Tests**: 301 Passed, 0 Failed  

---

## 1. Overall Status per Area

| Area | Planned | Status | Evidence (Files, Tests, Verification) | Notes |
|------|---------|--------|---------------------------------------|-------|
| **Active Domain Entities** | 18 Core Entities for MVP telemetry, reference data, chat, and auth | **Done** | `Models/Entities/*.cs` (18 files: `ApplicationUser`, `Astronaut`, `ChatMessage`, `ConditionSymptom`, `ConditionVitalSign`, `EmergencyProcedure`, `HealthAlert`, `HealthMetricGuide`, `Mission`, `MoodEntry`, `Recommendation`, `ReferenceRange`, `RefreshToken`, `SleepRecord`, `SpaceCondition`, `Symptom`, `VitalSignReading`, `VitalSignType`). `Models/Common/BaseEntity.cs`, `Models/Enums/AppEnums.cs`. | Base properties, navigation collections, and domain enums fully aligned. |
| **Backlog Entities** | 21 Extended Telemetry / Medical Entities deferred to Phase 5+ | **Done (Archived)** | `Models/Backlog/Entities/*.cs` (21 files: `CommunicationStatus`, `CrewMember`, `DailyTask`, `EmergencyIncident`, `EnvironmentStatus`, `ExerciseLog`, `FatigueEntry`, `HealthReport`, `HydrationNutritionLog`, `MedicalCheckup`, `MedicalHistoryEntry`, `MedicalMessage`, `MedicalSupportRequest`, `Medication`, `PersonalBaseline`, `PhysicalActivity`, `PvtTestResult`, `RadiationDoseRecord`, `SimulationAttempt`, `SimulationScenario`, `SymptomLog`). | Excluded from compilation in `AstronautHealthMonitoring.csproj` via `<Compile Remove="Models/Backlog/**" />`. |
| **DbContext & Entity Configurations** | Fluent API mapping for all 18 tables + Identity tables | **Done** | `Data/AppDbContext.cs`, `Data/Configurations/*.cs` (17 configuration files). | `AppDbContext` inherits from `IdentityDbContext<ApplicationUser>`. Cascade/restrict/set-null deletion rules and indexes enforced. |
| **EF Core Migrations & Model Snapshot** | Schema migration pipeline in single root directory | **Done** | `Migrations/` (4 migrations: `20261107022248_InitialCreate`, `20261107033006_FixReferenceRangesAndTexts`, `20261109042856_AddIdentityAndRefreshTokens`, `20261109160013_AddIsActiveToAstronaut`). Model snapshot: `Migrations/AppDbContextModelSnapshot.cs`. Command: `dotnet tool run dotnet-ef migrations has-pending-model-changes` verified: *"No changes have been made to the model since the last migration."* | Single migration folder at project root. Namespace: `AstronautHealthMonitoring.Migrations`. |
| **Seed Data** | Seed catalogs, clinical guides, reference ranges, conditions, symptoms, and demo users | **Done** | `Data/Seed/ModelBuilderExtensions.cs` (seeded via `modelBuilder.Seed()` on `HasData`): 6 vital sign types, 6 baseline reference ranges, 6 metric guides, 6 emergency procedures, 14 symptoms, 6 space conditions, join tables. `Services/Implementations/IdentityDataSeeder.cs` (Development/flag only): 1 Mission (`ARES-III`), 3 demo Astronauts (`Morgan Vance`, `Jordan Lee`, `Taylor Quinn`), 1 Monitor (`Casey Rivera`). | Passwords and JWT keys must be supplied via user-secrets/environment variables. Fail-fast validation prevents startup with missing secrets when seeding is enabled. |
| **DTOs (Data Transfer Objects)** | Feature-organized request and response payloads | **Done** | `DTOs/` (44 files organized by feature: `Alerts`, `Astronaut`, `Auth`, `Chat`, `Common`, `Dashboard`, `Emergency`, `Guides`, `Missions`, `Mood`, `Recommendations`, `ReferenceRanges`, `Sleep`, `SymptomGuidance`, `Sync`, `VitalSigns`). | `AstronautId` strictly removed from client write request DTOs (resolved securely from JWT). Response DTOs retain `AstronautId`. |
| **Request Validators** | FluentValidation rules for client payloads, timestamps, and ranges | **Done** | `Validators/` (11 validator files). Registered in DI via `AddValidatorsFromAssembly`. | Enforces required fields, non-empty GUIDs, bounds, and configurable future-timestamp tolerance (default 5 min). |
| **Mapster Mapping Profiles** | Compile-safe object mapping replacing AutoMapper | **Done** | `Mapping/` (12 `IRegister` profile files). Configured in `Program.cs`. Verified by test: `MapsterConfigurationTests.Mapster_Configuration_IsValid_And_Compiles`. | Strictly ignores server-set properties (`Id`, `CreatedAt`, `DurationHours`, `AstronautId` on writes). All 5 mapping tests pass. |
| **Service Interfaces** | Async business logic contracts taking DTOs and `CancellationToken` | **Done** | `Services/Interfaces/` (15 interfaces: `IAstronautService`, `IAuthService`, `IChatService`, `ICurrentUserService`, `IDashboardService`, `IHealthAlertService`, `IIdentityDataSeeder`, `IMissionService`, `IMoodService`, `IRecommendationService`, `ISleepService`, `ISymptomGuidanceService`, `ITokenService`, `IVitalSignService`, `IVitalSignStatusEvaluator`). | All methods use DTOs only and take explicit `astronautId` parameters for write endpoints. |
| **Service Implementations** | Core application business logic engines | **Done** | Implemented: `VitalSignStatusEvaluator`, `AuthService`, `TokenService`, `CurrentUserService`, `IdentityDataSeeder`, `VitalSignService`, `HealthAlertService`, `SleepService`, `MoodService`, `SignalRAlertNotifier`, `AstronautService`, `MissionService`, `DashboardService`, `RecommendationService`, `SymptomGuidanceService`, `ChatService`, `SignalRChatNotifier`, `SyncService`. | Core clinical flow (vitals + safety alerts + emergency procedure linking), telemetry (sleep + mood), astronaut/mission management, dashboard aggregation, recommendations engine, symptom guidance, chat module, and offline sync engine completed. |
| **Authentication & Authorization** | ASP.NET Core Identity + JWT + Refresh Token Rotation with Grace Window | **Done** | `Services/Implementations/AuthService.cs`, `TokenService.cs`, `CurrentUserService.cs`. `Controllers/AuthController.cs`. Policies: `AstronautOnly`, `MonitorOnly`, `Authenticated`. Tests: `AuthServiceTests` (7 tests), `TokenServiceTests` (4 tests), `CurrentUserServiceTests` (3 tests), `AuthorizationPolicyTests` (6 tests). | Hex SHA-256 hashed refresh tokens. 30s grace window for retry on flaky links. Full session revocation on token reuse outside grace window. Rate limited login endpoint (5 req/min). |
| **Controllers (REST API)** | Web API HTTP endpoints | **Done** | `Controllers/AuthController.cs`, `Controllers/VitalSignsController.cs`, `Controllers/HealthAlertsController.cs`, `Controllers/SleepController.cs`, `Controllers/MoodController.cs`, `Controllers/AstronautsController.cs`, `Controllers/MissionsController.cs`, `Controllers/DashboardController.cs`, `Controllers/RecommendationsController.cs`, `Controllers/SymptomGuidanceController.cs`, `Controllers/ChatController.cs`, `Controllers/SyncController.cs`. | Proper status codes (201 Created with Location, 400 Bad Request, 401, 403, 404, 409 Conflict), policy enforcement, and FluentValidation integration. Duplicate `/api/healthalerts` route removed in favor of canonical `/api/alerts`. |
| **Swagger UI Documentation** | Interactive API discovery and testing interface | **Done** | `builder.Services.AddOpenApi()` with Bearer JWT security scheme transformer, `app.MapOpenApi()`, `app.UseSwaggerUI()` at `/swagger`. Accessible in Development environment reading `/openapi/v1.json`. | Swashbuckle Swagger UI (`/swagger`) active and configured with Bearer JWT authorize support and relative server URL (`"/"`) preventing mixed-content HTTP issues behind IIS/reverse proxies. Forwarded headers (XForwardedFor, XForwardedProto) applied at start of pipeline. |
| **SignalR (Real-time)** | Live telemetry broadcast and ground-orbit chat streaming | **Done** | `AlertsHub` at `/hubs/alerts`, `ChatHub` at `/hubs/chat`, `[Authorize]`, groups: `monitors` and `astronaut-{id}`, `IAlertNotifier` and `IChatNotifier` abstractions with SignalR implementations. Real-time events dispatched after DB commit. | Query-string access token authentication configured in `Program.cs` (`JwtBearerEvents` for `/hubs/*`). |
| **Offline Synchronization** | Idempotent bulk sync of vitals, sleep, mood, and chat | **Done** | `ISyncService`, `SyncService`, `SyncController` at `POST /api/sync` (`AstronautOnly` policy). Idempotent replay protection via unique `ClientGeneratedId`, batch size limits (default 200), clock drift bounds, and atomic item processing reusing domain services. | Tests: `SyncServiceTests` (8 tests), `SyncControllerTests` (4 tests). |
| **Chat Module** | Unified astronaut thread with ground monitors | **Done** | `IChatService`, `ChatService`, `ChatController`, `ChatHub`. Paged messages, send message, mark as read receipts, unread counts, and real-time SignalR notifications. | Tests: `ChatServiceTests` (8 tests), `ChatControllerTests` (11 tests), `ChatHubTests` (10 tests). |
| **Symptom Guidance** | Space adaptation topic guidance (non-diagnostic) | **Done** | `ISymptomGuidanceService`, `SymptomGuidanceService`, `SymptomGuidanceController`. Catalog search at `/api/symptoms/guidance`, evaluation at `POST /api/symptoms/assess`, automated red-flag Emergency alert escalation via `IHealthAlertService`. | Non-diagnostic educational disclaimer returned on all responses. Tests: `SymptomGuidanceServiceTests` (7 tests), `SymptomGuidanceControllerTests` (8 tests). |
| **Alerts & Evaluator Engine** | Rule-based vital sign status evaluation and alert generation | **Done** | Evaluator Engine & Services **Done**: `IVitalSignStatusEvaluator.cs`, `VitalSignStatusEvaluator.cs`, `IVitalSignService.cs`, `VitalSignService.cs`, `IHealthAlertService.cs`, `HealthAlertService.cs`, `IAlertNotifier.cs`, `SignalRAlertNotifier.cs`, `AlertsHub.cs`. Automatic alert creation on non-routine telemetry and linked EmergencyProcedure (`EP-HYPOXIA-01`, `EP-CO2-02`). Manual alert trigger with rate limiting at canonical route `POST /api/alerts/manual`. Real-time SignalR notifications to monitors and owning astronaut groups after commit. | Deterministic clinical evaluator. Scoped astronaut isolation with monitor oversight. |
| **Recommendations Engine** | Operational and clinical crew guidance | **Done** | `Recommendation` entity, configuration, DTOs, validators, mapping profile, `IRecommendationService.cs`, `RecommendationService.cs`, `RecommendationsController.cs`. Tests: `RecommendationServiceTests` (7 tests), `RecommendationsControllerTests` (6 tests). | Priority sorting, pending/active filtering, astronaut scoping, and status lifecycle transition matrix (`Pending -> Acknowledged -> Completed/Dismissed`) with 409 Conflict guards on invalid transitions. |
| **Logging & Diagnostics** | Diagnostic inspection and operational logging | **Done** | `builder.Services.AddProblemDetails()`, `/health` health check endpoint with relational `DbHealthCheck` database probe (`.AllowAnonymous()`), rate-limiting for login and manual alerts, startup configuration inspection. | RFC 7807 ProblemDetails enabled. |
| **Automated Tests** | Unit, reflection, and real relational integration tests | **Done** | `tests/AstronautHealthMonitoring.Tests/` (301 tests across 27 test classes). Command: `dotnet test` (301 passed, 0 failed, duration: ~4s). Integration suite marked with `[Trait("Category", "Integration")]` (4 tests). | Comprehensive reflection sweep asserts authorization policies on every endpoint. Real relational integration tests with SQLite in-memory assert transaction rollback, unique constraint index enforcement, and $O(1)$ query count. |
| **Documentation** | System architecture, relational model, and status tracking | **Done** | `docs/Relationships.md` (complete relational analysis and ER diagram), `docs/Project-Status.md`, `README.md`. | Neutral monitor terminology verified; no realistic credentials present. |

---

## 2. What Remains to Finish the MVP (Execution Roadmap)

Based on the project roadmap, the following sequential implementation batches remain to achieve full MVP completion:

```
[Service Interface Review]
           │
           ▼
[Batch A: Vitals & Alerts] ───► IVitalSignService + IHealthAlertService
           │
           ▼
[Batch B: Core Telemetry]  ───► Dashboard + ISleepService + IMoodService + IRecommendationService
           │
           ▼
[Batch C: Operations & Chat] ─► IAstronautService + IMissionService + IChatService
           │
           ▼
[Batch D: Guidance & Safety] ─► Guide/Emergency retrieval + ISymptomGuidanceService (restore SymptomLog)
           │
           ▼
[Feature Controllers]      ───► REST endpoints for all services
           │
           ▼
[SignalR Hubs]             ───► TelemetryHub, AlertsHub, ChatHub
           │
           ▼
[Offline Sync Service]     ───► SyncService (atomic processing, deduplication count)
           │
           ▼
[Hardening & Quality]      ───► Health check endpoint, ProblemDetails, SwaggerUI
```

### Batch Summaries
1. **Batch A (Vitals & Safety Alerts)**: `VitalSignService` records readings idempotently, runs `IVitalSignStatusEvaluator` against reference ranges, triggers `HealthAlert`s on non-routine values, links emergency procedures (`EP-HYPOXIA-01`, `EP-CO2-02`), and notifies real-time SignalR subscribers (`AlertsHub`). Monitors can acknowledge/resolve alerts; manual alerts supported at `POST /api/alerts/manual`.
2. **Batch B (Telemetry & Personal Care)**:
   - `DashboardService`: High-efficiency, bounded $O(1)$ queries for crew physiological overview (`GET /api/dashboard/monitor`), individual astronaut snapshots (`GET /api/dashboard/astronauts/{id}`), and personalized astronaut consoles (`GET /api/dashboard/me`).
   - `SleepService`: Computes sleep duration, calculates sleep efficiency, and records records idempotently.
   - `MoodService`: Subjective valence, stress level, and emotion tracking.
   - `RecommendationService`: Operational/clinical recommendations with strict state transitions (`Pending -> Acknowledged -> Completed/Dismissed`).
3. **Batch C (Operations & Communication)**:
   - `AstronautService`: Monitor-side provisioning (`POST /api/astronauts`) with atomic Identity user creation + transaction rollback on failure. Deactivation prevents new data ingestion while keeping records monitor-readable.
   - `MissionService`: Mission lifecycle management, crew assignments, and unassignment guards.
   - `ChatService`: Thread-based communication scoped per astronaut, message pagination, read receipts, and real-time push (`ChatHub`).
4. **Batch D (Knowledge & Symptom Guidance)**:
   - `SymptomGuidanceService`: Topic matching against space adaptation conditions, red-flag emergency alert escalation, and mandatory educational disclaimers.
5. **Batch E (Offline Telemetry Sync Engine)**:
   - `SyncService`: Atomic batch ingestion of vitals, sleep, mood, and chat messages. Idempotency enforced via unique `ClientGeneratedId` and clock-drift tolerance. Replaying batches returns duplicate item statuses with zero duplicated rows.
6. **Infrastructure & Diagnostics**:
   - `DbHealthCheck`: Anonymous DB ping at `GET /health`.
   - Swashbuckle Swagger UI at `/swagger` reading `/openapi/v1.json` with JWT Bearer security scheme.
   - Comprehensive test suite: 296 tests including reflection authorization sweeps and SQLite in-memory relational integration tests.

---

## 3. Open Decisions & Assumptions

| # | Topic | Current Assumption | Decision Required from Stakeholder |
|---|-------|--------------------|-----------------------------------|
| 1 | **Crew Provisioning Model** | **Resolved**: Monitor-side provisioning endpoint implemented at `POST /api/astronauts` (Policy: `MonitorOnly`) with atomic Identity user creation. | Administrative provisioning pattern established. |
| 2 | **Manual Emergency Alert Triggering** | **Resolved**: Flight monitors and astronauts can trigger manual alerts at canonical route `POST /api/alerts/manual` (`MedicalAttention` or `Emergency` severity only). | Duplicate route removed; canonical endpoint confirmed. |
| 3 | **Supabase Connection Pooling** | Migrations require direct/session port (`5432`). Transaction mode (port `6543`) does not support EF Core prepared statements without `No Reset On Close=true;Max Auto Prepare=0;`. | Confirm whether runtime connection uses Supabase Session Pooler or Transaction Pooler with compatibility flags. |
| 4 | **Interactive API Documentation** | **Resolved**: Swashbuckle Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`) enabled at `/swagger` reading `/openapi/v1.json` with Bearer JWT security scheme. `Scalar.AspNetCore` completely removed. | Swagger UI active in Development. |
| 5 | **SymptomLog Roadmap Timing** | `SymptomLog` remains in `Models/Backlog` without breaking existing database schemas. Space adaptation guidance operates via non-diagnostic topic evaluations. | Confirm whether `SymptomLog` persistence is needed in post-MVP clinical extensions. |

---

## 4. Medical Content Requiring Staff Review

All medical rules, emergency steps, and reference ranges must be reviewed and signed off by authorized clinical personnel before operational spaceflight deployment. The following table identifies every placeholder and sample item in the solution:

| Content Item | Code Location | Source / Research Status | Notes & Review Requirements |
|--------------|---------------|--------------------------|-----------------------------|
| **Sleep Thresholds** | `appsettings.json:81-90` | **NOT from research** (Placeholder) | Target: 8.0h, Warning: < 6.0h, Emergency: < 4.5h, Efficiency Warning: < 75%, Efficiency Emergency: < 60%. Needs operational flight surgeon sign-off. |
| **Sync Future Timestamp Tolerance** | `appsettings.json:91-94`, `Validators/*Validators.cs:12` | **Operational assumption** (Placeholder) | Default 5 minutes tolerance for offline clock drift. Requires mission operations review. |
| **Heart Rate Reference Range** | `Data/Seed/ModelBuilderExtensions.cs:91` | Terrestrial Standard (General research) | Normal [60-100] bpm, Emergency (<40 or >140). Marked `IsReviewed = false`, `Notes = "PLACEHOLDER - requires review by authorized staff"`. |
| **Blood Pressure Reference Range** | `Data/Seed/ModelBuilderExtensions.cs:113` | Terrestrial Standard (General research) | Systolic [90-120], Diastolic [60-80]. Emergency (>160 or <70 systolic, >100 or <45 diastolic). Marked `IsReviewed = false`. |
| **SpO2 Reference Range** | `Data/Seed/ModelBuilderExtensions.cs:131` | Terrestrial Standard (General research) | Normal [95-100]%, Emergency (<89%). Marked `IsReviewed = false`. Spacecraft cabin pressure variations may alter baseline. |
| **Core Temperature Reference Range** | `Data/Seed/ModelBuilderExtensions.cs:149` | Terrestrial Standard (General research) | Normal [36.5-37.3] °C, Emergency (<35.0 or >38.5). Marked `IsReviewed = false`. |
| **Blood Glucose Reference Range** | `Data/Seed/ModelBuilderExtensions.cs:167` | Partial research (Fasting only) | Normal [70-99] mg/dL, Emergency (<50 or >200). Lower bound 70 is marked placeholder; post-prandial ranges not specified in research. |
| **Respiratory Rate Reference Range** | `Data/Seed/ModelBuilderExtensions.cs:185` | Terrestrial Standard (General research) | Normal [12-18] br/min, Emergency (<8 or >30). Marked `IsReviewed = false`. |
| **Closed Environment Air Quality Guidance** | `Data/Seed/ModelBuilderExtensions.cs:362` | Sample Content (Non-vital environmental) | Self-care guidance is placeholder. Note that ambient CO2 is environmental sensor telemetry, not human vitals. |
| **Emergency Procedures Instructions** | `Data/Seed/ModelBuilderExtensions.cs:220-310` | Simplified clinical protocols | Protocols for Hypoxia, Tachycardia, Bradycardia, Hyperthermia, Hypoglycemia, Hypotension. Immediate actions need operational verification. |

---

## 5. Known Risks & Technical Debt

1. **Project Directory Layout (Nested Tests)**:
   - *Risk*: `tests/AstronautHealthMonitoring.Tests` is nested inside the main Web API root directory `Health-Monitoring/`. In previous builds, recursive MSBuild evaluation caused duplicate `bin/obj` artifact copying.
   - *Mitigation Applied*: Added `<DefaultItemExcludes>$(DefaultItemExcludes);tests/**</DefaultItemExcludes>` and `<Compile Remove="tests/**" />` to `AstronautHealthMonitoring.csproj`.
   - *Recommended Structural Fix*: Restructure solution to standard `src/AstronautHealthMonitoring` and `tests/AstronautHealthMonitoring.Tests` layout (requires stakeholder approval).
2. **In-Memory Test Provider vs PostgreSQL Reality**:
   - *Risk*: Unit tests execute against `Microsoft.EntityFrameworkCore.InMemory`. In-memory EF Core does not enforce relational foreign key constraints, column lengths, transaction isolation levels, or PostgreSQL-specific syntax (such as ILIKE or unique filtered index `WHERE "UserId" IS NOT NULL`).
   - *Mitigation*: Comprehensive manual/integration tests must be executed against a real PostgreSQL/Supabase instance prior to release.
3. **Configuration Precedence / Stale Secrets**:
   - *Risk*: When running `dotnet ef`, configuration sources (like environment variables) may point to an older or alternate Supabase host than local user-secrets.
   - *Mitigation Applied*: Added startup diagnostic logger that outputs the resolved connection host (without credentials) and the exact configuration provider name.
4. **Linux Headless Dev Certs**:
   - *Risk*: On Linux or Docker environments without a local certificate store, Kestrel may fail to establish HTTPS if developers do not trust certificates.
   - *Mitigation*: Provided explicit instructions to run via `http` profile or execute `dotnet dev-certs https --trust`.

---

## 6. Verification Summary (Real Command Proofs)

All conclusions in this report are backed by verified tool execution outputs from this session:
- **Build**: `dotnet build AstronautHealthMonitoring.sln -m:1 --no-restore` -> 0 Warnings, 0 Errors.
- **Full Test Run**: `dotnet test --no-restore` -> 301 Passed, 0 Failed, Duration: ~4s.
- **Relational Integration Suite**: `dotnet test --no-restore --filter "Category=Integration"` -> 4 Passed, 0 Failed (SQLite in-memory real relational provider, unique constraint enforcement, transaction rollback, and command interception).
- **Fast Test Suite**: `dotnet test --no-restore --filter "Category!=Integration"` -> 297 Passed, 0 Failed.
- **Model Drift Check**: `dotnet tool run dotnet-ef migrations has-pending-model-changes --context AppDbContext --no-build` -> *"No changes have been made to the model since the last migration."*
- **Migrations List**: `dotnet tool run dotnet-ef migrations list --context AppDbContext --no-build` -> All 4 migrations in chronological order (`InitialCreate`, `FixReferenceRangesAndTexts`, `AddIdentityAndRefreshTokens`, `AddIsActiveToAstronaut`).
- **Authorization & Reverse Proxy Reflection Guard**: Comprehensive reflection sweep asserting authorization policies on every endpoint, reverse proxy forwarded headers configuration (`XForwardedFor`, `XForwardedProto`, cleared known networks/proxies), and OpenAPI document transformer relative server URL (`"/"`).

