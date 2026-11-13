# Astronaut Health Monitoring

React 19 / TypeScript / Vite frontend and ASP.NET Core 9 / PostgreSQL / SignalR backend.

## Local testing with Supabase

Run both apps locally and keep the existing Supabase database connection in backend user-secrets.

- Backend: run dotnet run --project Backend/AstronautHealthMonitoring.csproj --launch-profile http. The API is http://localhost:5256 and Swagger is http://localhost:5256/swagger.
- Frontend: run npm ci and npm run dev inside Frontend, then open http://localhost:3000.
- Vite proxies /api and /hubs to http://localhost:5256, including WebSockets. The local backend accesses Supabase using the existing server-side connection string.
- Automatic migrations and demo seeding stay disabled unless explicitly enabled. Saved test records use the configured Supabase database.

## Production deployment (after local testing)
- Static production builds use https://astrohealth.runasp.net/api and https://astrohealth.runasp.net/hubs by default.
- Override with VITE_API_BASE_URL and VITE_HUB_BASE_URL at build time. VITE_BACKEND_URL sets only the development proxy target. See Frontend/.env.example.
- The hosted backend must allow the exact deployed frontend origin under Cors:AllowedOrigins. Include scheme and port; do not include paths. Credentialed SignalR connections require explicit origins.
- A static host must serve index.html for SPA routes. Never route API calls to index.html.

Sign in using an existing account and its password. Demo usernames are listed on the sign-in page; passwords are entered by the user and are never bundled. There is no automatic demo login. Browser build variables are public: never put passwords, JWT signing keys, or database credentials in VITE_ variables.

## Apply backend changes on MonsterASP

Local source edits do not change the hosted service. Publish Backend/AstronautHealthMonitoring.csproj for the hosting platform and upload that output through the existing deployment process. Configure ConnectionStrings:DefaultConnection, Jwt:Key, Jwt:Issuer, Jwt:Audience, and Cors:AllowedOrigins using host settings. Keep the existing database and JWT identity settings.

REST and SignalR now use string enums. The frontend also accepts the numeric enum format used by older deployments and sends compatible numeric enum fields. The new severity/duration symptom assessment requires this updated backend; the UI reports an older deployment instead of manufacturing a result.

CORS middleware follows routing and precedes authorization. HTTPS redirection is opt-in with HttpsRedirection:Enabled; enable only with working TLS and trusted forwarded-header configuration. Do not redirect a browser preflight to an unavailable HTTPS port.

Reference: https://learn.microsoft.com/aspnet/core/security/cors

## Run the backend locally (optional)

Install the .NET 9 SDK and configure PostgreSQL. Inside Backend, use dotnet user-secrets or environment variables to set ConnectionStrings:DefaultConnection and Jwt:Key (at least 32 bytes). The root .env file is consumed by Docker Compose, not dotnet run.

Run dotnet restore followed by dotnet run. The HTTP launch profile listens on http://localhost:5256. Swagger is at /swagger in Development. To use it from the frontend, set VITE_BACKEND_URL=http://localhost:5256 in Frontend/.env.development.local.

Database:AutoMigrate and SeedData:Enabled are opt-in. Before enabling demo seeding, set SeedUsers:Astronaut:Password and SeedUsers:Monitor:Password on the server. Existing exposed demo passwords should be rotated by the account owner; this change does not rewrite Git history or rotate hosted credentials.

## Optional isolated Docker stack

Copy .env.example to .env and replace its placeholders. Run docker compose up -d --build. This creates a separate PostgreSQL database, local API, and nginx frontend. Frontend is http://localhost:3050; API is http://localhost:4050. The frontend container uses same-origin nginx proxies to the local backend. Auto-migration and demo seeding apply only to this explicitly started stack.

## Verification

- Frontend: npm test and npm run build inside Frontend.
- Backend: dotnet test Backend/tests/AstronautHealthMonitoring.Tests/AstronautHealthMonitoring.Tests.csproj.
- Hosted connectivity: check /health, /api/auth/me, and authenticated /hubs/alerts and /hubs/chat negotiation. A 401 without a token is expected for protected routes.

See Backend/docs/Final-Edits-Progress.md for implemented changes and remaining work from Final Edits.md.

Educational model, not a medical device, and not a NASA-approved protocol. Demo rules require review by a mission medical team.

## Arabic, saving feedback and habit tracking

See Backend/docs/Arabic-Saving-Habits.md for the migration command and the local test flow. Apply pending migrations before running the updated backend: the recommendations schema now includes Arabic text, habit points and schedule metadata, and daily completions use a new table.

## Automatic assignment reminders

See Backend/docs/Assignment-Reminders.md. Monitor assignments can send an initial reminder and repeat every X minutes/hours. Astronauts can select their tasks and sound preferences; monitors can send an immediate reminder. Apply the new migration, restart the backend, then enable reminder sound in the astronaut browser. The app must remain open for live sound and browser notifications.
