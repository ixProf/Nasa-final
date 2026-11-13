# Anatomy viewer above symptom assessment

The existing body-map panel now supports an optional BioDigital Human Viewer API 3 integration. It stays in the same place above the symptom selector; the seeded accounts, existing scenario controls and other page layouts are preserved.

## Why this option

BioDigital documents a browser JavaScript SDK and iframe viewer, with selection events and programmatic object selection. It suits the existing React/Vercel frontend and does not require native physiology binaries on the MonsterASP backend. It requires a registered viewer developer key, an allowed frontend domain and an active subscription or trial that covers the chosen published model.

Pulse is a physiology simulation engine rather than an anatomy viewer. Integrating its native engine or a Unity WebGL build is a separate simulation project; the current deployment does not include either runtime. VOKA offers an embeddable bundle by agreement. Visible Body's standard educational access does not establish a custom integration entitlement. No third-party models or engine outputs have been copied, fabricated or added without a usable integration licence.

## Available immediately

- The local interactive 3D model and 2D view continue to work without credentials.
- Selecting a body region filters the symptom chips to relevant catalog codes. Existing selections are retained; no symptom is added automatically.
- All catalog red flags remain visible for every region.
- The selected region continues to show the existing health information panel.
- New controls and filtering explanations support Arabic and English.

## Enable BioDigital

Set these frontend environment variables locally and in the Vercel project, then rebuild:

```dotenv
VITE_BIODIGITAL_DEVELOPER_KEY=your_public_viewer_developer_key
VITE_BIODIGITAL_MODEL_ID=your_licensed_published_model_id
```

Register the actual local-development address and deployed Vercel domain with the BioDigital developer app. Follow the provider's HTTPS requirements; a development origin may need local HTTPS or a secure preview. Use the **public viewer developer key**, never the Content API developer secret, in frontend settings. Do not enter your backend JWT or database credentials here.

When both settings exist, the body panel offers **Interactive map** and **Detailed anatomy**. The external viewer and SDK load only after the user chooses Detailed anatomy. The SDK uses `human.ready`, `scene.info`, `scene.picked`, and `scene.selectObjects`. Selecting a recognized object updates the local region; selecting a local region highlights matching model objects. Unknown structures do not select a symptom or invent a region mapping.

For exact matching, configure object IDs from the actual published model:

```dotenv
VITE_BIODIGITAL_REGION_OBJECTS={"heart":["actual-heart-object-id"],"brain":["actual-brain-object-id"],"lungs":[],"eyes":[],"stomach":[]}
```

The JSON example is a shape only; obtain real IDs from the chosen model. Without an explicit mapping, recognized object IDs/names are matched to the five existing local regions. The app does not upload astronaut identities, vitals, assessment results or tokens to BioDigital; the viewer receives its model configuration and anatomy navigation commands. BioDigital's own model content and labels depend on the content/languages supplied by the licensed model and are not translated by this app.

## Failure handling and limits

SDK load failure, iframe failure or a 20-second readiness timeout returns to the local model with a short bilingual notice. The existing 2D switch remains available. No subscription screens, trial models or unlicensed iframe URLs are shown when credentials are absent.

No BioDigital key, published model ID or subscription was supplied in this session. The local viewer/filtering are usable, and the integration code is prepared; live licensed content and exact object mappings still require those settings for end-to-end verification. This viewer is educational/navigation UI and does not produce diagnosis probabilities or simulated vital readings.

Official references checked:

- https://developer.biodigital.com/docs/widget/getting-started/first-widget
- https://developer.biodigital.com/docs/widget/reference/namespaces/scene
- https://devsupport.biodigital.com/hc/en-us/articles/360051663974-Why-am-I-seeing-an-authentication-error-in-my-embedded-model
- https://pulse.kitware.com/_docs.html
- https://voka.io/embeddable-3d-model-viewer/
- https://www.visiblebody.com/legal/visible-body-user-agreement
