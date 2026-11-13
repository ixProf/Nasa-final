# Astronaut Health Monitoring System - Frontend API Documentation

> **Target Version**: v1.0.0 (MVP)  
> **API Base URL (HTTP)**: `http://localhost:5256`  
> **API Base URL (HTTPS)**: `https://localhost:7012`  
> **Interactive Swagger UI**: `http://localhost:5256/swagger` (Development environment only)  
> **OpenAPI JSON Spec**: `http://localhost:5256/openapi/v1.json`  

---

## 1. System Overview & Architecture

The **Astronaut Health Monitoring System** is a mission-critical physiological telemetry, health surveillance, and crew-support backend designed for human spaceflight operations. The platform coordinates between two primary user personas:

1. **Astronaut (`Astronaut` role)**:
   - Wears biosensors recording continuous physiological telemetry (heart rate, blood pressure, SpO2, core temperature, blood glucose, respiratory rate).
   - Submits daily subjective health metrics (sleep logs, mood/stress check-ins).
   - Consults non-diagnostic educational space adaptation guidance and self-care protocols.
   - Communicates with ground medical officers over a dedicated ground-orbit messaging thread.
   - Synchronizes offline sensor batches when telemetry caches reconnect to spacecraft telemetry buses.

2. **Flight Support Monitor / Flight Surgeon (`Monitor` role)**:
   - Maintains continuous oversight of all active crew members and expedition missions.
   - Receives instant push notifications for physiological anomalies and safety alerts (`Warning`, `Critical`, `MedicalAttention`, `Emergency`).
   - Acknowledges and resolves alerts, stamping clinical actions taken.
   - Issues operational recommendations (sleep adjustment, exercise prescription, CO2 mitigation).
   - Provisions new astronaut accounts and manages mission assignments.

### Running Locally
```bash
# Launch HTTP-only profile (recommended for headless containers or Linux environments)
dotnet run --launch-profile http

# Launch standard HTTPS profile
dotnet run --launch-profile https
```

### Environment Variables & Secrets (Names Only)
The frontend and backend interact using standard environment configuration:
- `ConnectionStrings__DefaultConnection`: PostgreSQL/Supabase session connection string.
- `Jwt__Key`: 256-bit symmetric signing secret (minimum 32 characters).
- `Jwt__Issuer`: `AstronautHealthMonitoring`
- `Jwt__Audience`: `AstronautHealthMonitoringClient`
- `SeedUsers__Astronauts__0__Password` through `SeedUsers__Astronauts__2__Password`: Demo crew passwords.
- `SeedUsers__Monitor__Password`: Demo flight surgeon password.
- `Cors__AllowedOrigins`: Array of trusted frontend origins (e.g. `http://localhost:3000`, `http://localhost:5173`).

---

## 2. Authentication & Session Lifecycle

Authentication is implemented via **ASP.NET Core Identity** and **JWT Bearer Tokens** paired with **Cryptographic Refresh Token Rotation**.

```
[ Frontend Client ]                                      [ Backend API ]
         │                                                      │
         │─── 1. POST /api/auth/login (username, password) ────►│
         │◄── 2. 200 OK (AccessToken 60m, RefreshToken 30d) ───│
         │                                                      │
         │─── 3. Authorized Requests: Authorization: Bearer ───►│
         │                                                      │
   (Token Expiring)                                             │
         │─── 4. POST /api/auth/refresh (RefreshToken) ────────►│
         │◄── 5. 200 OK (New AccessToken, New RefreshToken) ────│
         │                                                      │
   (Sign Out)                                                   │
         │─── 6. POST /api/auth/revoke (RefreshToken) ─────────►│
         │◄── 7. 204 No Content (Revoked) ──────────────────────│
```

### 1. Login (`POST /api/auth/login`)
- Rate-limited to **5 attempts per minute** per client IP. Exceeding this returns `429 Too Many Requests`.
- Failed attempts are tracked by Identity lockout rules. **5 consecutive invalid credentials** locks the account for **15 minutes** (`423 Locked`).
- Successful authentication returns a short-lived `accessToken` (60 minutes) and a single-use `refreshToken` (30 days).

### 2. Authorization Header
Every protected HTTP request must include the JWT in the standard header:
```http
Authorization: Bearer <access_token>
```

### 3. Refresh Token Rotation & 30-Second Grace Window
To withstand high packet-loss or flaky links in space communication:
- When refreshing a token (`POST /api/auth/refresh`), the backend issues a **brand new access token and a brand new refresh token**, while revoking the old refresh token.
- **Grace Window**: The old refresh token remains valid for **30 seconds** after its first exchange. If network jitter causes the client to retry the refresh request within 30 seconds, the server safely re-emits the active token pair rather than rejecting it.
- **Reuse Detection & Lockout**: If an already-rotated refresh token is presented **after** the 30-second window, the system flags token theft, revokes the entire descendant family of refresh tokens, and requires a full re-login.

### 4. SignalR Query-String Authentication
Browsers cannot attach custom HTTP headers during the initial WebSocket handshake. Therefore, for all SignalR hub connections under `/hubs/*`, the client must append the JWT as an `access_token` query parameter:
```
wss://localhost:7012/hubs/alerts?access_token=<jwt_token>
wss://localhost:7012/hubs/chat?access_token=<jwt_token>
```
*(The backend limits query-string token parsing exclusively to paths starting with `/hubs`.)*

---

## 3. Roles & Permissions Matrix

| Endpoint | HTTP Method | Route | Anonymous | Astronaut | Monitor |
|---|---|---|:---:|:---:|:---:|
| **Login** | `POST` | `/api/auth/login` | ✅ | ✅ | ✅ |
| **Refresh Token** | `POST` | `/api/auth/refresh` | ✅ | ✅ | ✅ |
| **Revoke Token** | `POST` | `/api/auth/revoke` | ❌ | ✅ | ✅ |
| **Current User Profile** | `GET` | `/api/auth/me` | ❌ | ✅ | ✅ |
| **Record Vital Signs** | `POST` | `/api/vitalsigns` | ❌ | ✅ | ❌ |
| **Latest Vitals** | `GET` | `/api/vitalsigns/latest` | ❌ | ✅ (Own) | ✅ (Any) |
| **Vital Signs History** | `GET` | `/api/vitalsigns/history` | ❌ | ✅ (Own) | ✅ (Any) |
| **Vital Sign by ID** | `GET` | `/api/vitalsigns/{id}` | ❌ | ✅ (Own) | ✅ (Any) |
| **Vital Sign Types Catalog** | `GET` | `/api/vitalsigns/types` | ❌ | ✅ | ✅ |
| **Record Sleep** | `POST` | `/api/sleep` | ❌ | ✅ | ❌ |
| **Latest Sleep** | `GET` | `/api/sleep/latest` | ❌ | ✅ (Own) | ✅ (Any) |
| **Sleep History** | `GET` | `/api/sleep/history` | ❌ | ✅ (Own) | ✅ (Any) |
| **Sleep Record by ID** | `GET` | `/api/sleep/{id}` | ❌ | ✅ (Own) | ✅ (Any) |
| **Record Mood** | `POST` | `/api/mood` | ❌ | ✅ | ❌ |
| **Latest Mood** | `GET` | `/api/mood/latest` | ❌ | ✅ (Own) | ✅ (Any) |
| **Mood History** | `GET` | `/api/mood/history` | ❌ | ✅ (Own) | ✅ (Any) |
| **Mood Entry by ID** | `GET` | `/api/mood/{id}` | ❌ | ✅ (Own) | ✅ (Any) |
| **List Alerts** | `GET` | `/api/alerts` | ❌ | ✅ (Own) | ✅ (All) |
| **Alert by ID** | `GET` | `/api/alerts/{id}` | ❌ | ✅ (Own) | ✅ (Any) |
| **Trigger Manual Alert** | `POST` | `/api/alerts/manual` | ❌ | ✅ (Self) | ✅ (Any) |
| **Acknowledge Alert** | `POST` | `/api/alerts/{id}/acknowledge` | ❌ | ❌ | ✅ |
| **Resolve Alert** | `POST` | `/api/alerts/{id}/resolve` | ❌ | ❌ | ✅ |
| **Provision Astronaut** | `POST` | `/api/astronauts` | ❌ | ❌ | ✅ |
| **List Astronauts** | `GET` | `/api/astronauts` | ❌ | ❌ | ✅ |
| **My Astronaut Profile** | `GET` | `/api/astronauts/me` | ❌ | ✅ | ❌ |
| **Astronaut by ID** | `GET` | `/api/astronauts/{id}` | ❌ | ✅ (Own) | ✅ (Any) |
| **Update Astronaut Profile** | `PUT` | `/api/astronauts/{id}` | ❌ | ❌ | ✅ |
| **Deactivate Astronaut** | `PATCH` | `/api/astronauts/{id}/deactivate` | ❌ | ❌ | ✅ |
| **List Missions** | `GET` | `/api/missions` | ❌ | ✅ | ✅ |
| **Mission by ID** | `GET` | `/api/missions/{id}` | ❌ | ✅ | ✅ |
| **Create Mission** | `POST` | `/api/missions` | ❌ | ❌ | ✅ |
| **Update Mission** | `PUT` | `/api/missions/{id}` | ❌ | ❌ | ✅ |
| **Delete Mission** | `DELETE` | `/api/missions/{id}` | ❌ | ❌ | ✅ |
| **Assign Astronaut to Mission** | `POST` | `/api/missions/{id}/astronauts` | ❌ | ❌ | ✅ |
| **Unassign Astronaut** | `DELETE` | `/api/missions/{id}/astronauts/{astronautId}` | ❌ | ❌ | ✅ |
| **Monitor Dashboard Overview** | `GET` | `/api/dashboard/monitor` | ❌ | ❌ | ✅ |
| **Astronaut Snapshot Dashboard** | `GET` | `/api/dashboard/astronauts/{id}` | ❌ | ❌ | ✅ |
| **Astronaut Personal Dashboard** | `GET` | `/api/dashboard/me` | ❌ | ✅ | ❌ |
| **List Recommendations** | `GET` | `/api/recommendations` | ❌ | ✅ (Own) | ✅ (All) |
| **Recommendation by ID** | `GET` | `/api/recommendations/{id}` | ❌ | ✅ (Own) | ✅ (Any) |
| **Create Recommendation** | `POST` | `/api/recommendations` | ❌ | ❌ | ✅ |
| **Update Recommendation Status** | `PATCH` | `/api/recommendations/{id}/status` | ❌ | ✅ (Own) | ✅ (Any) |
| **List Symptoms Catalog** | `GET` | `/api/symptoms` | ❌ | ✅ | ✅ |
| **Guidance Space Conditions** | `GET` | `/api/symptoms/guidance` | ❌ | ✅ | ✅ |
| **Assess Symptoms** | `POST` | `/api/symptoms/assess` | ❌ | ✅ (Self) | ✅ (Any) |
| **Get Chat Messages** | `GET` | `/api/chat/messages` | ❌ | ✅ (Own) | ✅ (Any) |
| **Send Chat Message** | `POST` | `/api/chat/messages` | ❌ | ✅ (Self) | ✅ (Any) |
| **Mark Chat Message Read** | `POST` | `/api/chat/messages/{id}/read` | ❌ | ✅ (Own) | ✅ (Any) |
| **Chat Unread Count** | `GET` | `/api/chat/unread-count` | ❌ | ✅ (Own) | ✅ (Any) |
| **Offline Telemetry Batch Sync**| `POST` | `/api/sync` | ❌ | ✅ | ❌ |
| **System Health Probe** | `GET` | `/health` | ✅ | ✅ | ✅ |

---

## 4. Response Envelopes & Conventions

### Date/Time Standards
All timestamps exchanged with the backend are **strict UTC ISO 8601 strings** with second/millisecond precision:
`YYYY-MM-DDTHH:mm:ss.sssZ` (Example: `"2026-11-09T18:30:00.000Z"`).

### RFC 7807 ProblemDetails Error Format
When an error occurs, the server responds with standard ProblemDetails JSON:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Value": [
      "'Value' must be greater than '0'."
    ],
    "RecordedAt": [
      "'Recorded At' must not be in the future beyond operational tolerance (5 min)."
    ]
  }
}
```

#### Common HTTP Status Codes
- `200 OK`: Successful read or update.
- `201 Created`: Successful creation; returns `Location` header pointing to new resource.
- `204 No Content`: Successful deletion or revocation with no return payload.
- `400 Bad Request`: Validation failure or business logic precondition violation (e.g., deactivated astronaut).
- `401 Unauthorized`: Missing, expired, or invalid JWT Bearer token.
- `403 Forbidden`: Insufficient role (e.g. astronaut attempting monitor actions, or monitor attempting offline sync).
- `404 Not Found`: Entity ID does not exist or belongs to another astronaut.
- `409 Conflict`: State conflict (e.g., deleting a mission with assigned crew, or illegal recommendation transition).
- `423 Locked`: User account locked due to repeated failed login attempts.
- `429 Too Many Requests`: Rate limit exceeded (login limit 5/min, manual alert limit 5/min).

### Generic Pagination Envelope (`PagedResult<T>`)
Used by all history and message list endpoints:
```json
{
  "items": [ /* array of records */ ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 85,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### Domain Enums
All enums are serialized as **strings**:

| Enum | Allowed String Values | Definition |
|---|---|---|
| `VitalSignStatus` | `Routine`, `MedicalAttention`, `Emergency` | Physiological status determined by clinical evaluator |
| `AlertSeverity` | `Warning`, `Critical`, `MedicalAttention`, `Emergency` | Clinical severity level of health alert |
| `AlertStatus` | `Active`, `Acknowledged`, `Resolved`, `Dismissed` | Operational lifecycle of an alert |
| `SleepQuality` | `Poor`, `Fair`, `Good`, `Excellent` | Calculated sleep quality tier |
| `RecommendationStatus` | `Pending`, `Acknowledged`, `Completed`, `Dismissed` | Crew task workflow state |
| `RecommendationPriority` | `Low`, `Medium`, `High`, `Critical` | Operational urgency |
| `RecommendationCategory` | `Sleep`, `PhysicalActivity`, `Nutrition`, `MentalHealth`, `Environmental`, `MedicalProcedure` | Clinical specialty |
| `MissionStatus` | `Planned`, `Active`, `Completed`, `Aborted` | Mission operational state |
| `SyncItemStatus` | `Accepted`, `Duplicate`, `Rejected` | Telemetry batch sync outcome per item |

---

## 5. Module Endpoint Reference

### 5.1 Authentication Module (`/api/auth`)

#### `POST /api/auth/login`
- **Policy**: Anonymous (Public) | **Rate Limit**: 5 req/min
- **Request**:
```json
{
  "username": "astro.vance",
  "password": "SamplePassword123!"
}
```
- **Response (`200 OK`)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
  "refreshToken": "7a8b9c0d1e2f3a4b...",
  "expiresAt": "2026-11-09T19:30:00.000Z",
  "user": {
    "id": "u-astro-001",
    "username": "astro.vance",
    "email": "morgan.vance@mission.space",
    "fullName": "Morgan Vance",
    "roles": ["Astronaut"],
    "astronautId": 1
  }
}
```
- **Errors**: `400 Bad Request` (empty fields), `401 Unauthorized` (bad credentials), `423 Locked` (account locked), `429 Too Many Requests`.

#### `POST /api/auth/refresh`
- **Policy**: Anonymous (Public)
- **Request**:
```json
{
  "refreshToken": "7a8b9c0d1e2f3a4b..."
}
```
- **Response (`200 OK`)**: Same shape as `login`. Returns brand-new access and refresh tokens.

#### `POST /api/auth/revoke`
- **Policy**: `Authenticated`
- **Request**:
```json
{
  "refreshToken": "7a8b9c0d1e2f3a4b..."
}
```
- **Response (`204 No Content`)**

#### `GET /api/auth/me`
- **Policy**: `Authenticated`
- **Response (`200 OK`)**: Returns `CurrentUserDto` matching the `user` property in `login`.

---

### 5.2 Astronaut & Mission Management

#### `POST /api/astronauts`
- **Policy**: `MonitorOnly`
- **Purpose**: Provisions a new astronaut record and creates their ASP.NET Core Identity user account atomically.
- **Request**:
```json
{
  "username": "astro.nova",
  "email": "alex.nova@mission.space",
  "firstName": "Alex",
  "lastName": "Nova",
  "astronautCode": "AST-004",
  "dateOfBirth": "1990-04-12T00:00:00.000Z",
  "gender": "Female",
  "bloodType": "A+",
  "heightCm": 172.5,
  "weightKg": 64.0,
  "missionId": 1,
  "initialPassword": "StrongPassword123!"
}
```
- **Response (`201 Created`)**: Full `AstronautDto`.
- **Errors**: `400 Bad Request` (duplicate code, username, or weak password), `404 Not Found` (mission does not exist).

#### `PATCH /api/astronauts/{id}/deactivate`
- **Policy**: `MonitorOnly`
- **Purpose**: Marks astronaut `IsActive = false`, locks Identity login, and revokes all active refresh tokens.
- **Response (`200 OK`)**: `{ "message": "Astronaut 1 successfully deactivated." }`

#### `DELETE /api/missions/{id}`
- **Policy**: `MonitorOnly`
- **Purpose**: Deletes an empty mission.
- **Errors**: `409 Conflict` if any active crew members are still assigned to the mission.

---

### 5.3 Vital Signs Telemetry (`/api/vitalsigns`)

#### `POST /api/vitalsigns`
- **Policy**: `AstronautOnly`
- **Purpose**: Ingests real-time biosensor telemetry reading, evaluates status against reference ranges, and auto-generates health alerts if non-routine.
- **Request**:
```json
{
  "clientGeneratedId": "550e8400-e29b-41d4-a716-446655440000",
  "vitalSignTypeId": 1,
  "value": 115.0,
  "secondaryValue": null,
  "measurementMethod": "Continuous ECG",
  "sensorLocation": "Chest Band",
  "recordedAt": "2026-11-09T18:25:00.000Z"
}
```
*(Validation: `clientGeneratedId` required UUID; `vitalSignTypeId` > 0; `value` > 0; `recordedAt` within 5-min future tolerance).*
- **Response (`201 Created`)**:
```json
{
  "id": 142,
  "clientGeneratedId": "550e8400-e29b-41d4-a716-446655440000",
  "astronautId": 1,
  "astronautName": "Morgan Vance",
  "vitalSignTypeId": 1,
  "vitalSignTypeName": "Heart Rate",
  "vitalSignTypeCode": "HR",
  "unit": "bpm",
  "value": 115.0,
  "secondaryValue": null,
  "status": "MedicalAttention",
  "measurementMethod": "Continuous ECG",
  "sensorLocation": "Chest Band",
  "recordedAt": "2026-11-09T18:25:00.000Z",
  "createdAt": "2026-11-09T18:25:02.120Z"
}
```

---

### 5.4 Sleep & Mood Telemetry

#### `POST /api/sleep`
- **Policy**: `AstronautOnly`
- **Request**:
```json
{
  "clientGeneratedId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "startTime": "2026-11-08T22:00:00.000Z",
  "endTime": "2026-11-09T06:00:00.000Z",
  "lightSleepMinutes": 240,
  "deepSleepMinutes": 120,
  "remSleepMinutes": 90,
  "awakeMinutes": 30,
  "recordedAt": "2026-11-09T06:15:00.000Z"
}
```
- **Response (`201 Created`)**: Calculates `durationHours = 8.0`, `efficiencyPercentage = 93.75%`, and evaluates `quality = "Good"`.

#### `POST /api/mood`
- **Policy**: `AstronautOnly`
- **Request**:
```json
{
  "clientGeneratedId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "moodScore": 8,
  "stressLevel": 3,
  "dominantEmotion": "Calm",
  "notes": "Good focus during EVA prep.",
  "recordedAt": "2026-11-09T12:00:00.000Z"
}
```
*(Validation: `moodScore` 1-10; `stressLevel` 1-10; `dominantEmotion` max 50 chars).*

---

### 5.5 Health Alerts & Emergency Procedures (`/api/alerts`)

#### `POST /api/alerts/manual`
- **Policy**: `Authenticated` | **Rate Limit**: 5 req/min
- **Purpose**: Raises a manual safety alert from the cockpit or flight control console.
- **Request**:
```json
{
  "astronautId": 1,
  "severity": "MedicalAttention",
  "title": "Severe Space Motion Sickness",
  "description": "Astronaut reporting persistent nausea and disorientation post-insertion.",
  "vitalSignReadingId": null
}
```
*(Note: Astronauts can only trigger manual alerts for themselves; monitors can specify any active astronaut. Allowed severity is strictly `MedicalAttention` or `Emergency`).*

#### `POST /api/alerts/{id}/acknowledge`
- **Policy**: `MonitorOnly`
- **Request**:
```json
{
  "actionTaken": "Instructed astronaut to drink 500ml electrolyte water and rest."
}
```

#### `POST /api/alerts/{id}/resolve`
- **Policy**: `MonitorOnly`
- **Request**:
```json
{
  "resolutionNotes": "Vitals normalized over 45 minutes. Routine status restored."
}
```

---

### 5.6 Recommendations Engine (`/api/recommendations`)

#### `POST /api/recommendations`
- **Policy**: `MonitorOnly`
- **Request**:
```json
{
  "astronautId": 1,
  "title": "Prescribed Ergometer Exercise Protocol",
  "description": "Perform 45-minute aerobic session on Cycle Ergometer at 75% max HR.",
  "category": "PhysicalActivity",
  "priority": "High"
}
```

#### `PATCH /api/recommendations/{id}/status`
- **Policy**: `Authenticated` (Astronaut can update own recommendations; Monitor can update any).
- **Request**:
```json
{
  "status": "Acknowledged"
}
```
- **Lifecycle Transition Rules**:
  - `Pending` ➔ `Acknowledged`
  - `Acknowledged` ➔ `Completed` or `Dismissed`
  - `Pending` ➔ `Dismissed` (Monitors only)
  - Attempting invalid transitions (e.g. `Completed` ➔ `Pending`) returns `409 Conflict`.

---

### 5.7 Space Adaptation & Symptom Guidance (`/api/symptoms`)

#### `POST /api/symptoms/assess`
- **Policy**: `Authenticated`
- **Purpose**: Evaluates reported symptom IDs against space condition catalogs and returns educational self-care guidance without diagnosing disease.
- **Request**:
```json
{
  "astronautId": 1,
  "symptomIds": [1, 3]
}
```
- **Response (`200 OK`)**:
```json
{
  "hasEmergencyFlag": false,
  "emergencyAlertId": null,
  "disclaimer": "EDUCATIONAL AND OPERATIONAL GUIDANCE ONLY. Not a medical diagnosis. Contact Flight Surgeon if symptoms persist or escalate.",
  "matchedConditions": [
    {
      "conditionName": "Space Motion Sickness (SMS)",
      "matchedSymptomCount": 2,
      "guidanceText": "Limit rapid head movements in pitch and yaw axes. Anchor gaze to cabin horizon.",
      "selfCareAdvice": "Hydrate with small sips; rest in sleep station."
    }
  ]
}
```
*(If symptoms contain red-flag indicators, `hasEmergencyFlag` is true, an automated emergency `HealthAlert` is generated, and its ID is returned in `emergencyAlertId`).*

---

### 5.8 Ground-Orbit Chat (`/api/chat`)

#### `GET /api/chat/messages`
- **Policy**: `Authenticated`
- **Query Params**: `astronautId` (required for monitors; ignored for astronauts who are automatically scoped), `pageNumber`, `pageSize`.
- **Response (`200 OK`)**: `PagedResult<ChatMessageDto>`.

#### `POST /api/chat/messages`
- **Policy**: `Authenticated`
- **Request**:
```json
{
  "clientGeneratedId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "astronautId": 1,
  "content": "Mission Control, telemetry sensor calibration complete.",
  "recordedAt": "2026-11-09T18:40:00.000Z"
}
```

---

### 5.9 Dashboard Aggregator (`/api/dashboard`)

#### `GET /api/dashboard/monitor`
- **Policy**: `MonitorOnly`
- **Purpose**: Bounded $O(1)$ query aggregation for ground flight controllers. Returns all crew status cards, active alerts, and mission summaries.

#### `GET /api/dashboard/astronauts/{id}`
- **Policy**: `MonitorOnly`
- **Purpose**: Detailed clinical snapshot for one astronaut (latest vitals per type, active alerts, recent sleep, mood, and open recommendations).

#### `GET /api/dashboard/me`
- **Policy**: `AstronautOnly`
- **Purpose**: Personal cockpit console for the logged-in astronaut.

---

### 5.10 Offline Telemetry Sync Engine (`POST /api/sync`)
- **Policy**: `AstronautOnly`
- **Purpose**: Atomic batch ingestion of vitals, sleep, mood, and chat messages generated while disconnected from ground communications.
- **Request**:
```json
{
  "deviceSyncTime": "2026-11-09T18:45:00.000Z",
  "vitalSignReadings": [ /* Array of CreateVitalSignReadingRequest */ ],
  "sleepRecords": [ /* Array of CreateSleepRecordRequest */ ],
  "moodEntries": [ /* Array of CreateMoodEntryRequest */ ],
  "chatMessages": [ /* Array of SendChatMessageRequest */ ]
}
```
- **Response (`200 OK`)**:
```json
{
  "success": true,
  "serverSyncedAt": "2026-11-09T18:45:02.340Z",
  "vitalSignReadingsProcessed": 1,
  "sleepRecordsProcessed": 1,
  "moodEntriesProcessed": 0,
  "chatMessagesProcessed": 1,
  "duplicatesIgnoredCount": 0,
  "itemResults": [
    {
      "clientGeneratedId": "550e8400-e29b-41d4-a716-446655440000",
      "entityType": "VitalSignReading",
      "status": "Accepted",
      "assignedId": 143,
      "message": null
    }
  ]
}
```
*(If a batch is replayed, duplicate items are safely reported with `status: "Duplicate"`, producing zero duplicated rows in the database).*

---

## 6. Real-Time SignalR Hubs

### 6.1 AlertsHub (`/hubs/alerts`)
Streams instantaneous notifications when safety alerts are triggered, acknowledged, or resolved.

- **Connection URL**: `wss://<host>/hubs/alerts?access_token=<jwt>`
- **Group Memberships**:
  - Monitors automatically join `"monitors"`.
  - Astronauts automatically join `"astronaut-{astronautId}"`.
- **Server-to-Client Events**:
  - `AlertCreated(HealthAlertDto alert)`: Dispatched to `"monitors"` and `"astronaut-{astronautId}"`.
  - `AlertAcknowledged(HealthAlertDto alert)`: Dispatched to `"monitors"` and `"astronaut-{astronautId}"`.
  - `AlertResolved(HealthAlertDto alert)`: Dispatched to `"monitors"` and `"astronaut-{astronautId}"`.

#### JavaScript Example
```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:7012/hubs/alerts", {
    accessTokenFactory: () => authState.accessToken
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .build();

connection.on("AlertCreated", (alert) => {
  console.warn(`[REAL-TIME ALERT] ${alert.severity}: ${alert.title}`);
  playAudioChime(alert.severity);
  showToast(alert);
});

await connection.start();
```

---

### 6.2 ChatHub (`/hubs/chat`)
Bi-directional real-time ground-orbit messaging.

- **Connection URL**: `wss://<host>/hubs/chat?access_token=<jwt>`
- **Client Methods (`connection.invoke(...)`)**:
  - `JoinAstronautThread(int astronautId)`: Monitors call this to subscribe to a specific astronaut's conversation thread.
  - `LeaveAstronautThread(int astronautId)`: Monitors call this when switching tabs.
  - `SendMessageToThread(SendChatMessageRequest request)`: Sends a message directly through the hub.
  - `MarkMessageRead(int messageId)`: Sends read receipt.
- **Server-to-Client Events**:
  - `MessageReceived(ChatMessageDto message)`
  - `MessageRead(ChatMessageDto message)`

---

## 7. Frontend Screen Mapping

| Screen / View | Target Role | REST Endpoints Required | Real-time Hubs Needed |
|---|:---:|---|---|
| **Login View** | All | `POST /api/auth/login`, `POST /api/auth/refresh` | None |
| **Flight Monitor Console** | Monitor | `GET /api/dashboard/monitor`, `GET /api/alerts` | `AlertsHub` (`AlertCreated`, `AlertAcknowledged`) |
| **Astronaut Cockpit HUD** | Astronaut | `GET /api/dashboard/me`, `POST /api/vitalsigns` | `AlertsHub`, `ChatHub` |
| **Telemetry History** | Both | `GET /api/vitalsigns/history`, `GET /api/vitalsigns/latest` | None (Periodic poll or cache) |
| **Sleep Tracker** | Both | `POST /api/sleep`, `GET /api/sleep/history` | None |
| **Mood & Stress Check-in** | Both | `POST /api/mood`, `GET /api/mood/history` | None |
| **Emergency Protocols** | Both | `POST /api/alerts/manual`, `GET /api/alerts/{id}` | `AlertsHub` |
| **Crew Recommendations** | Both | `GET /api/recommendations`, `PATCH /api/recommendations/{id}/status` | None |
| **Symptom Checker** | Both | `GET /api/symptoms/guidance`, `POST /api/symptoms/assess` | None |
| **Mission Control Chat** | Both | `GET /api/chat/messages`, `POST /api/chat/messages` | `ChatHub` (`MessageReceived`, `MessageRead`) |
| **Crew Provisioning** | Monitor | `POST /api/astronauts`, `PATCH /api/astronauts/{id}/deactivate` | None |
| **Mission Deployment** | Monitor | `GET /api/missions`, `POST /api/missions`, `POST /api/missions/{id}/astronauts` | None |
| **Telemetry Sync Center** | Astronaut | `POST /api/sync` | None (Sync completion feedback) |

---

## 8. Seeded Demo Accounts

The database seeder provisions standard demo profiles for local testing. **Passwords must be supplied via local configuration or user-secrets.**

| Username | Role | Full Name | Code / Assignment | Notes |
|---|:---:|---|---|---|
| `astro.vance` | `Astronaut` | Morgan Vance | `AST-001` (Ares V) | Demo Commander |
| `astro.lee` | `Astronaut` | Jordan Lee | `AST-002` (Ares V) | Demo Flight Engineer |
| `astro.quinn` | `Astronaut` | Taylor Quinn | `AST-003` (Ares V) | Demo Science Specialist |
| `monitor.rivera` | `Monitor` | Casey Rivera | Flight Support | Flight Surgeon console |

*(Passwords are set in user-secrets via `SeedUsers:Astronauts:0:Password` and `SeedUsers:Monitor:Password`).*

---

## 9. Known Limitations & Defensive Handling

1. **Disconnected Operations**: Spaceflight links experience delay and packet loss. All write requests accept `clientGeneratedId` UUIDs. Frontend clients should maintain an IndexedDB queue and replay via `POST /api/sync` on reconnection.
2. **Deactivated Crew Accounts**: Deactivating an astronaut blocks all write endpoints (`400 Bad Request` or `409 Conflict`). The frontend should inspect `isActive` on the astronaut profile and disable input fields accordingly.
3. **Clinical Range Review**: Reference ranges are marked with review notices in the database. UI screens should display the educational guidance disclaimer on symptom assessments and self-care recommendations.
4. **OpenAPI Document**: Available at `/openapi/v1.json` and visualizable via Swagger UI at `/swagger` in Development mode.

