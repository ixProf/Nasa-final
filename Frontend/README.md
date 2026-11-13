# NASA Astronaut Health Monitoring System - Modern Glassmorphism UI 🚀

A futuristic, ultra-responsive, and lightweight mission telemetry dashboard for astronauts and flight surgeons. Built with **React 19**, **Vite**, **TypeScript**, **Tailwind CSS**, and official **SignalR Hub integration**.

---

## ✨ Features & Visual Highlights

- **Aesthetic Glassmorphism UI**: High-fidelity frosted glass cards (`backdrop-filter: blur(24px)`), cosmic gradient glow, neon highlights, and seamless alignment with the NASA operational design specification.
- **Astronaut & Earth Cosmic Canvas**: Incorporates the official background visual (`Astronaut Gazing Over Earth`) with ambient dark gradient overlays for maximum contrast and readability.
- **Dynamic 3-Tier Clinical Status Banner**: Real-time evaluation (Routine / Medical Attention / Emergency) with an animated, fluid ECG rhythm wave.
- **Biometric Telemetry Cards**: 
  - Heart Rate (نبض القلب): animated 3D pulse & range indicators.
  - Oxygen Saturation (مستوى الأكسجين): pulmonary dynamics.
  - Blood Pressure (ضغط الدم): dual systolic/diastolic tracking.
  - Body Temperature (درجة الحرارة): thermal regulation monitor.
- **24-Hour Heart Dynamics Wave**: Ultra-lightweight custom SVG bezier spline wave with active point marker tooltip, zero external heavy chart library dependencies.
- **Quick Action Triggers**:
  - **Record New Reading**: interactive modal to input and evaluate biometrics in real time.
  - **Mission Medical Dossier**: comprehensive printable health status report.
- **Bilingual (Arabic / English)**: Instant one-click toggle with complete RTL/LTR directional reflow and tailored typography (`Tajawal`, `Cairo`, `Inter`).
- **Real-Time SignalR & Offline-First Ready**: Integrated with backend SignalR hubs (`/hubs/alerts`), local cache, and idempotent sync endpoints.

---

## 🚀 Getting Started

### 1. Install Dependencies
```bash
cd Frontend
npm install
```

### 2. Run the Development Server
```bash
npm run dev
```
Open [http://localhost:3000](http://localhost:3000) in your browser.

### 3. Build for Production
```bash
npm run build
```
Creates an optimized, tree-shaken static production bundle in `Frontend/dist`.

---

## 📁 File Structure

```text
Frontend/
├── public/
│   └── images/
│       └── astronaut-bg.png       # High-res mission background
├── src/
│   ├── components/
│   │   ├── dashboard/
│   │   │   ├── HeartRateChart.tsx # 24h interactive SVG spline wave
│   │   │   ├── QuickActions.tsx   # Action trigger cards
│   │   │   ├── StatusBanner.tsx   # Health state banner & ECG wave
│   │   │   ├── VitalCard.tsx      # Individual metric card
│   │   │   └── VitalsGrid.tsx     # 4-card telemetry grid
│   │   ├── layout/
│   │   │   ├── Header.tsx         # User profile & alerts badge
│   │   │   └── Sidebar.tsx        # Vertical glass dock navigation
│   │   └── modals/
│   │       ├── AlertsModal.tsx    # Medical protocol alerts
│   │       ├── RecordVitalModal.tsx # Data entry & evaluation
│   │       └── ReportModal.tsx    # Medical dossier & print view
│   ├── context/
│   │   ├── LanguageContext.tsx    # i18n & RTL/LTR engine
│   │   └── TelemetryContext.tsx   # Reactive state & evaluator
│   ├── i18n/
│   │   ├── ar.ts                  # Arabic translations
│   │   ├── en.ts                  # English translations
│   │   └── index.ts
│   ├── services/
│   │   ├── api.ts                 # Backend REST client & JWT rotation
│   │   └── signalrService.ts      # Real-time WebSocket connection
│   ├── styles/
│   │   └── index.css              # Custom glassmorphism & neon glow
│   ├── types/
│   │   └── telemetry.ts           # Type definitions
│   ├── App.tsx                    # Main dashboard layout
│   └── main.tsx                   # App root entry
├── index.html                     # HTML shell with NASA metadata & fonts
├── package.json
├── tailwind.config.js             # Space theme & animations
├── tsconfig.json
└── vite.config.ts                 # Proxy to Backend API (:5256)
```

