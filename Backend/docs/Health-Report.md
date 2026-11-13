# Health report and sync display

The Reports page reads the authenticated astronaut's full available histories for vitals, sleep, mood, alerts, assignments, habits, and care communications. A monitor's report uses the selected astronaut ID on every request. Paged histories follow server pagination; failed sections are disclosed rather than replaced with sample records.

The report supports 7-day, 30-day and all-history views, local timestamps, measurement input context, sleep stages, mood scales, symptom intensity and duration, alerts and resolutions, recommendations and reminders, habit totals and available daily activity. Habit totals are lifetime statistics; displayed activity days reflect the backend's available heatmap. Mood averages are normalized to percentages to account for mixed score scales. Blood-pressure summary statistics use systolic readings, with both values in the detail table. Browser print supports saving the report as PDF.

The existing backend has no symptom-assessment history endpoint. Completed assessments are now kept in sessionStorage, scoped by the signed-in account and target astronaut, for inclusion in reports in that browser session. This does not recover older assessments, create backend persistence or transfer assessments to another device. Storage failures do not turn a successful assessment into a failed server request.

The former sync button is replaced with a visual simulation. A cycle starts every 5 seconds and displays progress for 900 ms. The timer is cleaned up on unmount. This display sends no network requests and does not modify pending/accepted/rejected queue records. Actual queue statuses remain visible separately; the existing synchronization service is unchanged.

Validation: frontend build, full frontend tests, and report-specific tests for pagination, incomplete histories and account/astronaut separation. Browser and print-layout verification remain necessary in the local app.
