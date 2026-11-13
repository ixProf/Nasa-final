# Symptom assessment v2 — Step 0 review and proposed contract

Status: review completed; proposal only. No application implementation or database migration is included in this step.

Sources: `symptom_classifier_prompt.md` and `تقرير ربط الأعراض بمخاطر الفضاء + مؤشر الارتباط (النسخة 2).md`, supplied on 13 November 2026. This follows Part E: “After EACH step, stop and show me what changed” and “Propose the interface/DTO changes BEFORE writing code.” Subsequent steps must generate migrations only, never apply them or start an app instance that automatically applies them.

## Existing code reviewed

| Area | Current behavior | Required change |
| --- | --- | --- |
| Runtime | ASP.NET Core/.NET 9, EF Core/Npgsql, Mapster, Supabase | Keep the actual .NET 9 target; the prompt's .NET 8 description is stale. |
| `ISymptomGuidanceService` | Catalog queries and `EvaluateSymptomsAsync(request, cancellationToken)` | Keep these signatures. Add optional classifier behind a separate interface, not inside the numerical rules. |
| `ConditionSymptom.Weight` and its DTO | Integer, seeded as 1 | Change both to fractional values; configure database precision and generate a migration. |
| `SpaceCondition` | Has `IsMatchable`, no hazard or cap | Add `HazardCode` and nullable `ConfidenceCap`; null for knowledge-only topics. |
| Seed | 18 symptoms, 12 conditions; no SMS, no near-vision symptom | Tier 1 adds SMS, three scored symptoms and five red flags. Existing red flags: fainting, chest pain, difficulty breathing. |
| Assessment | Severity/duration triage plus matches ordered by symptom count | Replace the association ranking with weighted coverage/evidence, caps, gates, hazard maxima and explainable breakdowns. Preserve triage as a separate output; an association strength must never itself become an emergency threshold. |
| Matches | Computed but excluded from HTTP JSON with `JsonIgnore` | Explicit public v2 association collection; do not assume adding index fields makes them visible. |
| Alerts | Existing `TriggerManualAlertAsync`, failure reported via `AlertDeliveryError` | Preserve this path and astronaut ownership. Evaluate/send known red flags before optional classifier calls. Never claim delivery without success. |
| Frontend | Passes signed-in or monitor-selected astronaut, visible errors, no fabricated assessment result | Preserve these fixes. Replace the result panel with red flags first, top hazards, breakdowns, knowledge cards and bilingual footer. Remove hardcoded symptom count. |
| Offline | Server assessment needs the backend/database; disabling the LLM alone does not make browser assessment offline | Distinguish LLM outage fallback from full browser offline assessment. Shared versioned rules/catalog and local templates are needed for the latter; queue alert delivery and visibly label it pending. |

## Proposed entity and DTO changes

Use consistent JSON camelCase matching the existing API. These are contract proposals, not code already added.

### Persistence and catalog

- `SpaceCondition.HazardCode`: bounded code (`GRAVITY`, `CLOSED_ENV`, `ISOLATION`; no scored radiation condition).
- `SpaceCondition.ConfidenceCap`: nullable integer; 85 / 70 / 60 / 50 per condition in the report. Knowledge-only topics have no index or cap.
- `ConditionSymptom.Weight` and `ConditionSymptomDto.Weight`: `decimal`, database `numeric(12,9)`. Calculate 1/N without rounding to two decimal places; display rounding happens only at the end.
- Expose hazard and cap on `SpaceConditionDto`; Mapster and the frontend catalog type must change with the entity.
- Retain existing catalog IDs and junction IDs where possible; add new rows deterministically. New explanatory content carries `SAMPLE CONTENT - pending staff review`.
- Tier 1 contains 26 symptoms (18 existing + 8 new). The full 32-symptom catalog belongs to the later tier; do not silently omit later symptoms from denominators when claiming the full report's exact scores.

### Request and trusted context

Keep `AstronautId`, `SymptomCodes`, `Severity`, `DurationHours`. Add `Locale` (`ar`/`en`, validated). Free text and confirmation of classified codes belong to the optional classifier step.

Create a server-resolved assessment context, separate from the public request:

| Field | Source and behavior |
| --- | --- |
| Mission day | Derive from the astronaut's assigned mission start; unknown remains null. |
| Hours since gravity transition and transition kind | Require a recorded launch/landing/arrival. A launch is not evidence of post-landing orthostatic timing. |
| Communication delay | Trusted mission settings; changes guidance order, never association numbers. |
| Radiation event/time and dose budget | Recorded mission event/settings, with expiry; never inferred from symptoms or accepted as an unchecked astronaut boolean. |
| CO2 event | Recorded environmental context; unknown does not trigger a boost. |
| Sleep/baseline and mood pattern | Derive from actual astronaut history. Missing nights/days are not zero-valued evidence. |
| Latest relevant vital flags | Load server-side for the target astronaut, with freshness rules and timestamps. |

Existing `Mission` does not store transition kind, communication delay, radiation events or CO2 events. Tier 1 must report these as unavailable rather than inventing them. Context additions can be implemented in the later tier without changing the assessment service signature.

### Response

Retain `Level` (1–3), `Reasons`, `RedFlags`, `RecommendedActions`, `ShouldEscalate`, `TriggeredAlertId`, `AlertDeliveryError` for compatibility. Add:

- `EngineVersion`: `2.0-demo`.
- `ConditionMatches`: public list of extended `ConditionMatchResultDto` with `ConditionCode`, `HazardCode`, `AssociationIndex`, `Strength` (`weak` / `moderate` / `strong`), matched symptom codes, bilingual explanations and `Breakdown`.
- `Breakdown`: matched weight, total weight, coverage, evidence, phase-gate factor/state, vital boost, context boost, cap, uncapped result and final index. Include reason codes so every change is explainable.
- `HazardScores`: hazard code, index, strength, explanation and contributing condition. Compute the maximum of its conditions, not a sum. Do not fabricate numeric distance/radiation hazards.
- `KnowledgeOnly`: separate cards with code, name, explanation and source; no numeric index properties.
- `GuidanceMode`: `REAL_TIME` or `DELAYED`; include trusted delay seconds when known.
- `Radiation`: status (`withheld`, `rule_triggered`, `not_applicable`), explanation, optional recorded-event time and optional mission-budget consumption. No symptom-based radiation index.
- `ContextWarnings`: missing phase, baseline or event data, rather than silently assuming an affirmative context.
- Bilingual fixed footer. Keep emergency delivery state separate from any future delayed support-message state.

Do not return “message queued” in delayed mode until a durable support queue actually exists. For now the template can state the recorded communication delay and instruct contact without claiming a message was sent.

### Optional classifier interface (Step 5)

Propose `ISymptomClassifierClient.ClassifyAsync(...)` and `ExplainAsync(...)`, with typed validated results. Disabled by default via `SymptomClassifier:Enabled=false`; no provider or credentials assumed. Maximum three-second budget, one retry only within that total budget, then deterministic templates. Known red flags bypass waiting. Minimize outbound data: no astronaut IDs, names, mission identifiers or raw records.

Classify output may add only canonical symptom codes; preserve explicit chips, negation and resolved-code distinctions. Final escalation is the maximum of server rules and classifier escalation. Treat psych-crisis screening as a separate emergency flag using the same alert path, not as a scored condition. Explain output cannot alter numbers, ordering or escalation. Enforce phrase/number validation and fall back to fixed templates if invalid; a prompt alone is insufficient enforcement.

## Specification conflicts found during review

1. **SMS window:** report section 5.2 says five days following launch or gravity transition, while implementation Step 2 says five days after launch / 72 hours after transition. Proposed precedence: use the report's explicit SMS five-day window; reserve 72 hours for post-landing/arrival orthostatic timing. Document that choice in tests.
2. **Weights versus printed arithmetic:** use exact 1/N weights, not the report's two-decimal table as calculation inputs. With the full catalog, fatigue/concentration/poor sleep/daytime sleepiness gives Sleep about 78.57, Stress 20.83 and Air 20.03. The printed integer results are display approximations.
3. **Radiation example SMS score:** nausea plus fatigue contributes only nausea to SMS under the supplied mappings. SMS's total weight is 29/12, so its raw index is 100/(29/12), about 41.38, not 48. The engine should follow the catalog/formula, not hardcode the inconsistent example.
4. **Tier 1 versus full-catalog acceptance:** the full Air denominator includes nasal congestion and eye irritation (Tier 2). Therefore the full report's exact Air score cannot be an acceptance target for the strict Tier 1 seed alone. Keep separate tier-specific expected values; add the full-catalog cases when those symptoms are added.
5. **Radiation activation:** a generic gravity-transition timestamp cannot establish whether the astronaut has landed, and a radiation event cannot remain active indefinitely. Record event kind/time, do not fake these from mission day or free text.
6. **Wording:** the fixed footer explicitly contains the words otherwise forbidden in generated explanations. Exempt only the exact fixed footer, not arbitrary disclaimers or body text.

These are consistency and implementation findings, not validation of the report's medical claims. All weights, boosts, caps, timing windows and triage choices remain demo rules pending medical review.

## Ordered execution after this checkpoint

1. Seed/model changes, fractional weights and a generated-only migration; show the seed diff and stop.
2. Pure deterministic engine, early red-flag path and supported phase gates; show formulas and stop.
3. Public response contract and serialization coverage; show sample JSON and stop.
4. Acceptance and regression tests, including caps, duplicated codes, radiation withholding, unknown context and astronaut 2 ownership; show results and stop.
5. Optional classifier, disabled by default; show fallback and reconciliation tests and stop.
6. Minimal bilingual frontend result panel and contract tests; show results and stop.

No migration application, database reset, production deployment, provider signup or credential changes are authorized by this staged prompt.
