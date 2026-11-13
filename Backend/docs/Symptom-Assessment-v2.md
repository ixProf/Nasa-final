# Symptom assessment v2 implementation

Implements the attached v2 report's deterministic MVP, including the full 32-symptom catalog so the full-report denominators and examples are reproducible. All numerical rules are demonstration values pending medical review.

## Implemented

- Fractional specificity weights (`decimal`, PostgreSQL `numeric(12,9)`), eight red flags, SMS, four knowledge-only topics, hazard codes and per-condition caps.
- Coverage × evidence × 100, followed by phase factor, relevant vital boost, context boost and cap. Rounding only for returned display indices. Hazard index is the maximum condition index. No condition can reach 100.
- SMS uses a five-day launch/transition window; orthostatic timing requires a recorded landing/arrival within 72 hours. Unknown timing applies a conservative half-factor and explains the missing context.
- Radiation is withheld without a recorded recent event. Event plus nausea/vomiting plus a qualifying companion symptom raises follow-up to medical attention. Light flashes alone do not raise escalation.
- Existing red-flag alert path runs before historical context queries. Target astronaut ownership and visible alert-save failures are retained. Association strength does not determine emergency escalation.
- Public `conditionMatches`, `hazardScores`, `breakdown`, `knowledgeOnly`, `radiation`, `guidanceMode`, version and bilingual footer. Legacy internal `Matches` remains excluded from JSON.
- Bilingual frontend results, red flags first, top three hazards, expandable calculation details, knowledge-only cards without scores, no percentages labelled as medical probabilities.
- Catalog caching and conservative offline assessment after the catalog has been loaded online. Offline results show unavailable mission context, withhold radiation, omit context/vital boosts, and explicitly state that no alert was sent. They do not claim to have queued a support message. Reassess after reconnecting.
- Guards against showing a previous astronaut's or previous language's late assessment response. Removed obsolete local symptom/condition fallback catalogs and the hardcoded symptom count.

## Recorded context

Context is resolved on the backend, never accepted as unchecked astronaut-supplied flags. Mission day uses the assigned mission's start date. Latest vitals are restricted to the previous 24 hours. Sleep and mood boosts require consecutive recorded days for that astronaut; absent days are not zero-valued observations. A configured sleep baseline is optional; the absolute less-than-five-hour rule still works without it.

Optional operator-owned configuration keys, supplied through normal backend configuration/user-secrets:

| Key | Meaning |
| --- | --- |
| `SymptomAssessment:Missions:<missionId>:GravityTransitionAtUtc` | Recorded UTC event timestamp |
| `SymptomAssessment:Missions:<missionId>:GravityTransitionKind` | `launch`, `landing`, or `arrival` |
| `SymptomAssessment:Missions:<missionId>:CommsDelaySeconds` | Nonnegative recorded delay; 60 seconds or more selects delayed guidance |
| `SymptomAssessment:Missions:<missionId>:RadiationEventAtUtc` | Radiation event active for at most 48 hours; future timestamps are ignored |
| `SymptomAssessment:Missions:<missionId>:Co2EventAtUtc` | CO2 event active for at most 24 hours; future timestamps are ignored |
| `SymptomAssessment:SleepBaselines:<astronautId>` | Recorded personal sleep baseline in hours |

These settings support recorded demo context; no new mission-event management UI or durable delayed-support queue was added. Missing recorded context never manufactures an event or a successful message delivery.

## Specification corrections

- SMS timing follows the report's five-day transition rule, rather than the conflicting 72-hour wording in Part E. The 72-hour window applies to orthostatic timing.
- Nausea plus fatigue produces SMS 41.38 within its phase window using the supplied full catalog, not the inconsistent example value 48.
- Full-catalog sleep example produces Sleep 78.57, Stress 20.83 and Air 20.03.
- Preserved original symptom IDs, labels, body areas and junction IDs. New labels and links have stable IDs. The follow-up label migration restores metadata changed during initial scaffolding.

## Database and testing

Generated migrations: `20261113093325_SymptomAssociationV2` and `20261113093734_PreserveSymptomLabels`. Neither was applied. Existing Supabase configuration was retained. Startup auto-migration is now disabled by default in all environments; explicit `Database:AutoMigrate=true` remains available when intentionally configured. The new backend requires the generated schema/catalog update before online assessment can be tested.

Validation covers the report examples, caps, fractional weights, duplicate selections, red/urgent signs, radiation withholding/expiry, knowledge-only serialization, phase timing, actual target-astronaut context, and frontend offline conformance with the backend seed. The backend and frontend builds pass. Backend Fact checks are invoked directly by the local reflection runner because the sandbox's testhost socket restriction prevents the standard testhost process; this is not a live Supabase/browser end-to-end test.

Final results: 98 backend Fact checks passed, 29 frontend tests passed, both builds passed, and EF reports no model changes missing from the generated migrations.

## Optional classifier

The optional LLM classify/explain integration was not added or configured. No symptom text is transmitted to an external model. The deterministic engine and fixed bilingual guidance work independently of an LLM. A future integration must implement the attached prompt's schema validation, privacy minimization, three-second total budget, escalation maximum, number/phrase checks and template fallback before being enabled.
