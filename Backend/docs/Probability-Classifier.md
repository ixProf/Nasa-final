# Individual symptom classification and optional probability prediction

The assessment now accepts `symptomDetails`: one `{code, severity, durationHours}` object per selected symptom. Each symptom is evaluated separately. Each condition receives a follow-up priority only from its own matching symptoms and relevant recent vital readings. Critical findings are shown first. The response's overall level is the highest current priority, not a severity assigned to every symptom.

Critical red flags continue through the existing emergency alert path. A matched condition with a related recent emergency vital reading also triggers an emergency alert to the medical monitor. Probability predictions never lower urgency and are requested only after alert handling. Alert saving is distinguished from confirmed receipt by a doctor.

The UI highlights each pattern's percentage, lists other potential patterns and gives independent severity/duration controls. Without a prediction model, this percentage is explicitly labelled **symptom match**, and `probabilityPercent` is null. Offline assessments also leave probability null and state that no alert was sent.

## Actual probability model requirement

No trained model or labelled patient-outcome dataset was supplied. The report's weights are not a calibrated disease-probability estimator. This implementation therefore supplies an integration adapter rather than manufacturing medical probabilities from match scores.

To enable real model predictions, supply a model API trained and validated for the input features and intended population. Operator-owned configuration:

| Key | Value |
| --- | --- |
| `RiskProbabilityModel:Enabled` | `true` after a suitable model is supplied |
| `RiskProbabilityModel:Endpoint` | HTTPS inference endpoint |
| `RiskProbabilityModel:ModelVersion` | Expected exact model version |
| `RiskProbabilityModel:ValidationReference` | Reference to the model's validation documentation |
| `RiskProbabilityModel:ApiKey` | Optional bearer credential, configured via user-secrets/environment |

The adapter sends only the engine version, canonical symptom codes with their individual severity and duration, and candidate condition codes. It does not send astronaut identity, names or free text. The validation-reference configuration is an operator prerequisite; entering it does not itself establish clinical validity.

Expected JSON response:

```json
{
  "modelVersion": "your-validated-version",
  "predictions": [
    { "conditionCode": "SLEEP_DEFICIENCY", "probabilityPercent": 62.5 }
  ]
}
```

The value above illustrates the response schema, not a real prediction. All fields are required. Out-of-range percentages, duplicate predictions, malformed JSON, unexpected versions and failures fall back to rule results. No probability is shown for missing predictions. Requests have a three-second timeout. A model failure cannot suppress an emergency alert.

Validation: 103 backend Fact checks and 29 frontend tests pass; both builds pass. This includes independent symptom intensity, unrelated pattern isolation, critical-vital doctor alerts, disabled-provider behavior, valid probability mapping and rejection of missing/invalid probability values. Live model accuracy/calibration and end-to-end doctor receipt cannot be tested without the real model and a live authenticated session.
