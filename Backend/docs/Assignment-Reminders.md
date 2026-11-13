# Assignment reminders

## Run locally with Supabase

Stop the existing backend with Ctrl+C. Inside Backend run:

    dotnet ef database update
    dotnet run

The new AddAutomaticAssignmentReminders migration adds persistent settings and a latest-delivery inbox. Existing habit/bilingual migrations must also be applied if pending. An idempotent SQL alternative for this reminder migration is assignment-reminders.sql; it assumes the previous migrations are already applied. Do not use both routes as separate changes.

Refresh the frontend. Use separate browser profiles or a private window to keep Monitor and Astronaut logins independent.

## Monitor workflow

- Create an assignment under Recommendations.
- Automatic reminders are on by default. Choose Every X minutes or hours and whether to request a reminder sound. Supported interval: 1 minute through 7 days.
- When reminders are enabled, saving sends an initial reminder immediately and schedules subsequent repeats.
- Choose an astronaut in the habit-view selector. Assignment reminders lists that astronaut's active tasks and saved reminder preferences.
- Send reminder to astronaut now sends an immediate targeted reminder, even if automatic reminders are off. Repeated manual clicks within five seconds share the same delivery.

## Astronaut workflow

- Click Enable reminder sound once to unlock and test audio. After permission was previously given, a normal page interaction can re-arm sound after a reload. Muting remains an explicit device preference.
- Enable browser notifications if wanted. Permission is requested only after this button is clicked. Denying permission does not disable in-app pop-ups.
- Open Recommendations, then Assignment reminders. Enable only the tasks you want, set their individual interval and sound, then Save reminder. Preferences persist on the backend.
- Each delivery opens a bilingual in-app pop-up. Background tabs can also show an operating-system notification when browser permission is granted. Sound is a brief two-note chime and respects both task and device mute controls.
- Open assignment navigates to the tasks page; Dismiss this reminder closes only the current delivery. Neither action cancels the repeat schedule. To stop repeats, disable Automatic reminder for this task or finish the task.
- Completed daily habits pause reminders for the rest of the UTC day. Completed one-off or dismissed assignments stop reminding. Direct monitor reminders for already completed tasks are rejected.

## Delivery behavior

- Keep the backend running and the web app open for live pop-ups and sound. This implementation does not install Web Push/service-worker delivery for a closed browser.
- The backend checks due schedules every 10 seconds and sends to the owning astronaut's authenticated SignalR group. A 30-second inbox poll and window-focus refresh provide reconnection fallback.
- Only the latest missed reminder for each task is retained. Returning astronauts see current pending reminders rather than a backlog of every missed repeat.
- Delivery tokens prevent polling and live events from duplicating the same pop-up. An old pop-up cannot acknowledge a newer delivery. Optimistic concurrency prevents multiple backend workers from claiming the same due reminder.
- Scheduling resumes from the current time after downtime. It does not burst-send missed intervals. Hosting that idles or suspends the backend also pauses worker execution until the backend is running again.
- Sound, notifications and browser background behavior depend on browser/device settings. Existing notification permissions can be revoked from the browser's site settings.

## Quick test

1. As Astronaut, enable sound and leave the dashboard open.
2. As Monitor in a different browser session, assign that astronaut a task with automatic reminders every 1 minute.
3. Verify an initial pop-up and chime. Dismiss it and wait for the next scheduled reminder.
4. Use Send reminder now to verify immediate delivery. Try changing a different astronaut's reminders while signed in as an astronaut: the backend must reject it.
5. Change the task to every 2 hours and save. Reload and verify the preference persists.
6. Disable automatic reminders; scheduled repeats stop. An explicit Monitor reminder still arrives.
7. Complete a daily habit; it should not remind again that UTC day.

## Browser documentation

https://developer.mozilla.org/en-US/docs/Web/Media/Guides/Autoplay
https://developer.mozilla.org/en-US/docs/Web/API/Notification/requestPermission_static

## Validation

Tests cover interval conversion, inbox deduplication and owner isolation, due scheduling, manual-send deduplication, persisted preferences, offline catch-up, stale acknowledgement, daily completion pauses, initial reminders and concurrent updates. Browser audio and real Supabase/SignalR delivery still need testing in the user's running environment.

Latest checks: both builds pass, 21 frontend tests pass, and 75 focused backend test methods pass through the direct test runner. EF reports no pending model changes. Applying the database migration from this runtime failed because the Supabase hostname could not be resolved; no migration application was confirmed.
