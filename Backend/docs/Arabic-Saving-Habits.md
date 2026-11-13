# Arabic, saving performance and habits — local test guide

## Setup

Frontend and backend run locally; the existing Supabase database remains configured in backend user-secrets.

Stop the backend with Ctrl+C. From the Backend directory, run:

    dotnet ef database update
    dotnet run

This applies the additive habit-completion and bilingual recommendation migrations. The update could not be applied from the agent runtime because the Supabase hostname could not be resolved. No database update was confirmed. If using the Supabase SQL editor instead, review and run habit-tracking.sql, generated from the last existing migration through both new migrations. Use one migration route, then restart the backend.

Keep the frontend running with npm run dev inside Frontend. Open http://localhost:3000 and refresh after the backend restart.

## Arabic / English

- Switch language on the sign-in page or in the header.
- Symptoms uses Arabic labels for all 18 real backend symptom codes.
- Medical reference cards translate all 12 real condition names, descriptions, guidance and escalation instructions. The mismatched invented fallback catalog is no longer displayed.
- Assessment reasons/actions, severity controls, units, statuses, built-in labels, save feedback and common errors are localized. Machine identifiers, email addresses and URLs retain their values.
- New assignments capture Arabic title, instructions and reason alongside the existing primary text. User-authored prose must be entered in both languages; the application does not invent translations of arbitrary medical instructions.

## Saving and responsiveness

- Vitals, sleep, mood, crew, assignments, missions and other forms disable during saving and show saving/saved/failure feedback. Async action buttons also show a spinner and lock while a returned operation is pending.
- A synchronous guard prevents fast double-clicks before React renders the disabled state.
- Identical concurrent measurement writes share one request. Retrying an uncertain result preserves the client record ID and capture timestamp; the backend's existing uniqueness checks prevent duplicate records.
- Sleep retry retains the original sleep interval. Failed chat sends retain their input and logical message identity.
- Dashboard hydration no longer waits for SignalR negotiation. Changing chat recipients no longer re-fetches every dashboard resource.
- Identical in-flight GETs are shared, and full chart history loads after the summary. Pages load in separate JavaScript bundles; the 3D body stays in its own deferred bundle.

Test: click Save repeatedly while a vital reading is pending. There should be one new saved record. Reload and verify persistence. Stop the backend, attempt a save, and verify failure feedback with input retained. Restart and retry once.

## Assignments / habit tracker

1. As Monitor, open Recommendations and choose an astronaut in the habit-view selector.
2. Create a recommendation, select Daily Recurring, enter both languages, choose points (1–100) and save.
3. As that astronaut, open Dashboard or Recommendations. The activity appears in Daily habits & achievements.
4. Mark it complete. Today's task locks, points increase, the first-step badge appears and today's heatmap cell changes.
5. Refresh: completion and points persist. Repeating a request cannot award the same day's points twice.

The tracker includes total points, levels (100 points each), current/best streak, achievement badges, daily progress and an 84-day heatmap. Click a heatmap cell for its date/count/points. Days follow UTC, displayed in both languages; the tracker refreshes on window focus and at UTC midnight. Monitors can view a selected astronaut but cannot complete tasks for them. Completion history starts empty and fills from actual saved completions.

Existing recommendations are not silently converted to daily habits. Scheduled time remains assignment metadata. Interval-based automatic reminders and manual Monitor reminders are now implemented; see Assignment-Reminders.md.

## Verification and limits

17 frontend regression tests pass and cover translations, numeric API compatibility, session refresh, request sharing, uncertain retries, and duplicate submissions. 62 focused backend checks pass through direct test-method invocation (the normal test host socket is unavailable here) and cover habit ownership, points, day boundaries, badges, streak gaps, bilingual assignment creation, vitals/sleep/mood idempotency and existing mappings.

Builds and local tests can run without Supabase access. Browser verification with the real database still requires the migration and a working connection from the user's terminal. The runtime blocks local socket probes and cannot resolve the Supabase hostname, so end-to-end browser/database performance has not been measured here.
