import { useUiText } from "../../i18n/useUiText";
/**
 * BodyMapPanel.tsx
 *
 * The full interactive "Anatomical Body Map" panel.
 * Integrates:
 *   • 3D model (BodyMap3D) or SVG fallback
 *   • Region selection sidebar with live telemetry status
 *   • Language support (EN/AR)
 *
 * This component is added to SymptomGuidancePage as a collapsible top card.
 * It does NOT replace the existing symptom assessment or condition guide.
 */

import React, { useState, useCallback, useEffect, lazy, Suspense } from 'react';
import { Brain, Heart, Wind, Eye, Atom } from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { BODY_REGIONS, type BodyRegionId, type BodyRegion } from '../../data/bodyRegions';
import { AnatomyViewer } from './AnatomyViewer';
import { AnatomySymptomsContext, type AnatomySymptom } from './AnatomySymptomsContext';
import { REGION_SYMPTOMS } from '../../services/anatomy';

// Lazy load the 3D canvas (heavy bundle — only loaded when visible)
const BodyMap3D = lazy(() =>
  import('./BodyMap3D').then((m) => ({ default: m.BodyMap3D }))
);

// ─── Helper: pick icon for a region ──────────────────────────────────────────

const RegionIcon: React.FC<{ iconType: BodyRegion['iconType']; className?: string }> = ({
  iconType,
  className = 'w-5 h-5',
}) => {
  switch (iconType) {
    case 'heart':
      return <Heart className={className} />;
    case 'lungs':
      return <Wind className={className} />;
    case 'resp':
      return <Brain className={className} />;
    case 'glucose':
      return <Atom className={className} />;
    case 'blood':
      return <Eye className={className} />;
    default:
      return <Eye className={className} />;
  }
};

// ─── Status pill ──────────────────────────────────────────────────────────────

type VStatus = 'Routine' | 'MedicalAttention' | 'Emergency';

const StatusPill: React.FC<{ status: VStatus; isAr: boolean }> = ({ status, isAr }) => {
  const tx = useUiText();
  const cfg = {
    Routine: {
      bg: 'bg-emerald-500/15 border-emerald-400/35 text-emerald-300',
      dot: 'bg-emerald-400',
      label: isAr ? 'طبيعي' : 'Normal',
    },
    MedicalAttention: {
      bg: 'bg-amber-500/15 border-amber-400/35 text-amber-300',
      dot: 'bg-amber-400',
      label: isAr ? 'مراقبة' : 'Watch',
    },
    Emergency: {
      bg: 'bg-rose-500/15 border-rose-400/35 text-rose-300',
      dot: 'bg-rose-400 animate-pulse',
      label: isAr ? 'طوارئ' : 'Alert',
    },
  }[status];

  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full border text-[10px] font-bold ${cfg.bg}`}
    >
      <span className={`w-1.5 h-1.5 rounded-full ${cfg.dot}`} />
      {tx(cfg.label)}
    </span>
  );
};

// ─── Derive status from vitals for a given region ─────────────────────────────

function useRegionStatus(region: BodyRegion): VStatus {
  const { vitals } = useTelemetry();

  for (const code of region.relatedVitalCodes) {
    const vital = vitals[code as keyof typeof vitals];
    if (!vital) continue;
    if (vital.status === 'Emergency') return 'Emergency';
    if (vital.status === 'MedicalAttention') return 'MedicalAttention';
  }
  return 'Routine';
}

// ─── Sidebar region card ──────────────────────────────────────────────────────

interface RegionCardProps {
  selectedSymptoms?: AnatomySymptom[];
  region: BodyRegion;
  isSelected: boolean;
  onClick: () => void;
  isAr: boolean;
}

const RegionCard: React.FC<RegionCardProps> = ({ region, isSelected, onClick, isAr, selectedSymptoms=[] }) => {
  const tx = useUiText();
  const status = useRegionStatus(region);
  const linked = selectedSymptoms.filter(s=>REGION_SYMPTOMS[region.id].includes(s.code));

  return (
    <button
      type="button"
      onClick={onClick}
      className={`w-full text-start p-3 rounded-xl border transition-all duration-200 flex items-center gap-3 ${
        isSelected
          ? 'border-cyan-400/60 bg-cyan-500/10 shadow-[0_0_16px_rgba(56,189,248,0.15)]'
          : 'border-white/10 bg-slate-900/50 hover:border-white/25 hover:bg-slate-900/70'
      }`}
    >
      {/* Coloured region icon */}
      <div
        className="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 transition-colors"
        style={{
          background: isSelected
            ? `${region.selectedColor}25`
            : `${region.hoverColor}18`,
          border: `1px solid ${isSelected ? region.selectedColor + '50' : region.hoverColor + '30'}`,
          color: isSelected ? region.selectedColor : region.hoverColor,
        }}
      >
        <RegionIcon iconType={region.iconType} className="w-4 h-4" />
      </div>

      {/* Labels */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center justify-between gap-2">
          <span className="text-xs font-bold text-white truncate">
            {tx(isAr ? region.nameAr : region.nameEn)}
          </span>
          <StatusPill status={status} isAr={isAr} />
        </div>
        <p className="text-[11px] text-slate-400 mt-0.5 line-clamp-1">
          {tx(isAr ? region.descriptionAr : region.descriptionEn)}
        </p>
        {linked.length > 0 && <p className="mt-1 text-[11px] text-amber-200">{isAr?'أعراض مختارة مرتبطة: ':'Related selected symptoms: '}{linked.map(s=>s.label).join(isAr?'، ':', ')}</p>}
      </div>
    </button>
  );
};

// ─── Detail panel for selected region ────────────────────────────────────────

interface RegionDetailProps {
  region: BodyRegion;
  isAr: boolean;
}

const RegionDetail: React.FC<RegionDetailProps> = ({ region, isAr }) => {
  const tx = useUiText();
  const status = useRegionStatus(region);
  const { vitals } = useTelemetry();

  // Pull related vital values for display
  const relatedVitals = region.relatedVitalCodes
    .map((code) => vitals[code as keyof typeof vitals])
    .filter(Boolean);

  return (
    <div className="rounded-xl bg-slate-900/60 border border-white/10 p-4 space-y-3 animate-in fade-in duration-200">
      {/* Header */}
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-3">
          <div
            className="w-10 h-10 rounded-xl flex items-center justify-center shrink-0"
            style={{
              background: `${region.selectedColor}25`,
              border: `1px solid ${region.selectedColor}50`,
              color: region.selectedColor,
            }}
          >
            <RegionIcon iconType={region.iconType} className="w-5 h-5" />
          </div>
          <div>
            <h4 className="text-sm font-bold text-white">
              {tx(isAr ? region.nameAr : region.nameEn)}
            </h4>
            <StatusPill status={status} isAr={isAr} />
          </div>
        </div>
      </div>

      {/* Description */}
      <p className="text-xs text-slate-300 leading-relaxed">
        {tx(isAr ? region.descriptionAr : region.descriptionEn)}
      </p>

      {/* Spaceflight note */}
      <div className="p-2.5 rounded-lg bg-blue-500/10 border border-blue-400/20">
        <p className="text-[11px] text-blue-200 leading-relaxed">
          <strong className="text-blue-300 font-bold">
            {tx(isAr ? '🛸 ملاحظة فضائية: ' : '🛸 Spaceflight Note: ')}
          </strong>
          {tx(isAr ? region.spaceflightNoteAr : region.spaceflightNoteEn)}
        </p>
      </div>

      {/* Live vital readings */}
      {relatedVitals.length > 0 && (
        <div className="grid grid-cols-2 gap-2">
          {relatedVitals.map((v) => (
            <div
              key={v.code}
              className="p-2 rounded-lg bg-slate-800/60 border border-white/8 text-center"
            >
              <div className="text-[10px] text-slate-400 mb-0.5 uppercase tracking-wider">
                {tx(isAr ? v.titleAr : v.titleEn)}
              </div>
              <div className="text-sm font-extrabold text-white tabular-nums" dir="ltr">
                {tx(v.value)}
                {tx(v.secondaryValue ? ` / ${v.secondaryValue}` : '')}
              </div>
              <div className="text-[10px] text-slate-400">
                {tx(isAr ? v.unitAr : v.unitEn)}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

// ─── 3D Canvas loading fallback ───────────────────────────────────────────────

const CanvasLoadingFallback: React.FC<{ isAr: boolean }> = ({ isAr }) => (
  <div className="w-full h-full flex flex-col items-center justify-center gap-3 text-slate-400">
    <div className="w-10 h-10 rounded-full border-2 border-cyan-500/30 border-t-cyan-400 animate-spin" />
    <span className="text-xs font-semibold tracking-wider">
      {isAr ? 'جارٍ تحميل النموذج ثلاثي الأبعاد…' : 'Initialising 3D Engine…'}
    </span>
  </div>
);

// ─── Main component ───────────────────────────────────────────────────────────

export const BodyMapPanel: React.FC<{onRegionChange?:(id:BodyRegionId|null)=>void;activeRegionId?:BodyRegionId|null;selectedSymptoms?:AnatomySymptom[]}> = ({onRegionChange,activeRegionId,selectedSymptoms=[]}) => {
  const tx = useUiText();
  const { language, isRtl } = useLanguage();
  const isAr = language === 'ar';

  const [selectedRegionId, setSelectedRegionId] = useState<BodyRegionId | null>(null);
  useEffect(()=>{if(activeRegionId!==undefined)setSelectedRegionId(activeRegionId);},[activeRegionId]);
  const [is3DEnabled, setIs3DEnabled] = useState(true);
  const [isPanelExpanded, setIsPanelExpanded] = useState(true);

  const selectedRegion = selectedRegionId
    ? BODY_REGIONS.find((r) => r.id === selectedRegionId) ?? null
    : null;

  const handleRegionSelect = useCallback((id: BodyRegionId) => {
    const next=selectedRegionId===id?null:id;
    setSelectedRegionId(next);onRegionChange?.(next);
  }, [selectedRegionId,onRegionChange]);

  return (
    <div className="glass-panel rounded-[22px] overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between p-4 pb-0">
        <div className="flex items-center gap-2.5">
          <div className="w-9 h-9 rounded-xl bg-cyan-500/15 border border-cyan-400/25 flex items-center justify-center text-cyan-300">
            <Brain className="w-4 h-4" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-white">
              {tx(isAr ? 'خريطة الجسم التشريحية التفاعلية' : 'Interactive Anatomical Body Map')}
            </h3>
            <p className="text-[11px] text-slate-400">
              {tx(isAr
                ? 'انقر على منطقة لعرض الحالة الصحية والإرشادات الفضائية'
                : 'Click a region to view health status and spaceflight guidance')}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          {/* 3D / 2D toggle */}
          <button
            type="button"
            onClick={() => setIs3DEnabled((v) => !v)}
            className={`px-2.5 py-1 rounded-lg border text-[10px] font-bold transition-all ${
              is3DEnabled
                ? 'bg-cyan-500/15 border-cyan-400/35 text-cyan-300'
                : 'bg-slate-900/50 border-white/15 text-slate-400 hover:border-white/30'
            }`}
          >
            {tx(is3DEnabled ? '3D' : '2D')}
          </button>

          {/* Collapse toggle */}
          <button
            type="button"
            onClick={() => setIsPanelExpanded((v) => !v)}
            className="w-7 h-7 rounded-lg border border-white/15 bg-slate-900/50 flex items-center justify-center text-slate-400 hover:text-white hover:border-white/30 transition-all"
          >
            <svg
              className={`w-3.5 h-3.5 transition-transform ${isPanelExpanded ? '' : 'rotate-180'}`}
              viewBox="0 0 12 12"
              fill="none"
            >
              <path d="M2 4L6 8L10 4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </button>
        </div>
      </div>

      {/* Body */}
      {isPanelExpanded && (
        <div className="p-4 pt-3">
          <div className={`grid gap-4 ${selectedRegion ? 'grid-cols-1 lg:grid-cols-5' : 'grid-cols-1'}`}>
            {/* 3D Canvas area */}
            <div
              className={`${selectedRegion ? 'lg:col-span-3' : 'col-span-full'} relative rounded-xl overflow-hidden`}
              style={{
                minHeight: 320,
                height: 'clamp(400px, 58vw, 540px)',
                background:
                  'radial-gradient(ellipse at 50% 60%, rgba(14,30,68,0.6) 0%, rgba(2,6,20,0.85) 100%)',
                border: '1px solid rgba(56,189,248,0.12)',
              }}
            >
              {is3DEnabled ? (
                <Suspense fallback={<CanvasLoadingFallback isAr={isAr} />}>
                  <AnatomySymptomsContext.Provider value={selectedSymptoms}><AnatomyViewer
                    selectedRegionId={selectedRegionId}
                    onRegionSelect={handleRegionSelect}
                  /></AnatomySymptomsContext.Provider>
                </Suspense>
              ) : (
                <SVGBodyFallback
                  selectedRegionId={selectedRegionId}
                  onRegionSelect={handleRegionSelect}
                  isAr={isAr}
                />
              )}
            </div>

            {/* Right side: region list + detail */}
            <div className={`${selectedRegion ? 'lg:col-span-2' : 'col-span-full'} flex flex-col gap-2`}>
              {/* When no 3D, show horizontal pills instead of vertical list in full-width mode */}
              {!selectedRegion ? (
                <div className={`grid gap-2 ${is3DEnabled ? 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-5' : 'grid-cols-2 sm:grid-cols-5'}`}>
                  {BODY_REGIONS.map((region) => (
                    <RegionCard
                      key={region.id}
                      region={region}
                      selectedSymptoms={selectedSymptoms}
                      isSelected={false}
                      onClick={() => handleRegionSelect(region.id)}
                      isAr={isAr}
                    />
                  ))}
                </div>
              ) : (
                <>
                  {/* Compact list */}
                  <div className="space-y-1.5 max-h-44 overflow-y-auto pr-0.5">
                    {BODY_REGIONS.map((region) => (
                      <RegionCard
                        key={region.id}
                        region={region}
                        selectedSymptoms={selectedSymptoms}
                        isSelected={selectedRegionId === region.id}
                        onClick={() => handleRegionSelect(region.id)}
                        isAr={isAr}
                      />
                    ))}
                  </div>

                  {/* Detail card */}
                  <RegionDetail region={selectedRegion} isAr={isAr} />
                </>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

// ─── SVG 2D Fallback ──────────────────────────────────────────────────────────

/**
 * Clean SVG body silhouette — the original 2D implementation.
 * Preserved as the fallback when 3D is toggled off or unavailable.
 */

interface SVGBodyFallbackProps {
  selectedRegionId: BodyRegionId | null;
  onRegionSelect: (id: BodyRegionId) => void;
  isAr: boolean;
}

const SVG_HOTSPOT_POSITIONS: Record<BodyRegionId, { cx: number; cy: number; r: number }> = {
  brain:   { cx: 160, cy: 55,  r: 22 },
  eyes:    { cx: 160, cy: 82,  r: 14 },
  lungs:   { cx: 160, cy: 160, r: 30 },
  heart:   { cx: 148, cy: 148, r: 16 },
  stomach: { cx: 160, cy: 220, r: 20 },
};

const SVGBodyFallback: React.FC<SVGBodyFallbackProps> = ({
  selectedRegionId,
  onRegionSelect,
  isAr,
}) => {
  const tx = useUiText();
  const [hovered, setHovered] = useState<BodyRegionId | null>(null);

  return (
    <div className="w-full h-full flex items-center justify-center">
      <svg
        viewBox="0 0 320 460"
        className="w-full h-full"
        style={{ maxWidth: 240 }}
        xmlns="http://www.w3.org/2000/svg"
      >
        {/* Defs */}
        <defs>
          <radialGradient id="bodyGrad" cx="50%" cy="40%" r="60%">
            <stop offset="0%" stopColor="#0f2a52" stopOpacity="0.7" />
            <stop offset="100%" stopColor="#020611" stopOpacity="0.9" />
          </radialGradient>
          <filter id="glow">
            <feGaussianBlur stdDeviation="3" result="coloredBlur" />
            <feMerge>
              <feMergeNode in="coloredBlur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>

        {/* Body silhouette */}
        {/* Head */}
        <ellipse cx="160" cy="52" rx="34" ry="38" fill="url(#bodyGrad)" stroke="#1e3a5f" strokeWidth="1.5" />
        {/* Neck */}
        <rect x="150" y="84" width="20" height="20" rx="4" fill="url(#bodyGrad)" stroke="#1e3a5f" strokeWidth="1.5" />
        {/* Torso */}
        <path
          d="M110 100 C95 100 80 115 80 140 L80 300 C80 320 90 330 110 330 L210 330 C230 330 240 320 240 300 L240 140 C240 115 225 100 210 100 Z"
          fill="url(#bodyGrad)"
          stroke="#1e3a5f"
          strokeWidth="1.5"
        />
        {/* Arms */}
        <path d="M80 115 C65 120 55 150 60 200 L70 200 C68 155 78 128 95 118 Z" fill="url(#bodyGrad)" stroke="#1e3a5f" strokeWidth="1" />
        <path d="M240 115 C255 120 265 150 260 200 L250 200 C252 155 242 128 225 118 Z" fill="url(#bodyGrad)" stroke="#1e3a5f" strokeWidth="1" />
        {/* Legs */}
        <path d="M110 328 L100 440 C100 450 118 450 118 440 L128 328 Z" fill="url(#bodyGrad)" stroke="#1e3a5f" strokeWidth="1" />
        <path d="M210 328 L220 440 C220 450 202 450 202 440 L192 328 Z" fill="url(#bodyGrad)" stroke="#1e3a5f" strokeWidth="1" />

        {/* Anatomical region hotspots */}
        {BODY_REGIONS.map((region) => {
          const pos = SVG_HOTSPOT_POSITIONS[region.id];
          const isSelected = selectedRegionId === region.id;
          const isHov = hovered === region.id;

          return (
            <g key={region.id}>
              {/* Glow ring */}
              {(isSelected || isHov) && (
                <circle
                  cx={pos.cx}
                  cy={pos.cy}
                  r={pos.r + 8}
                  fill="none"
                  stroke={isSelected ? region.selectedColor : region.hoverColor}
                  strokeWidth="2"
                  opacity={0.4}
                  filter="url(#glow)"
                />
              )}
              {/* Main hotspot */}
              <circle
                cx={pos.cx}
                cy={pos.cy}
                r={pos.r}
                fill={
                  isSelected
                    ? `${region.selectedColor}40`
                    : isHov
                    ? `${region.hoverColor}30`
                    : 'rgba(56,189,248,0.08)'
                }
                stroke={isSelected ? region.selectedColor : isHov ? region.hoverColor : 'rgba(56,189,248,0.3)'}
                strokeWidth={isSelected ? 2.5 : 1.5}
                style={{ cursor: 'pointer', transition: 'all 0.2s' }}
                onClick={() => onRegionSelect(region.id)}
                onMouseEnter={() => setHovered(region.id)}
                onMouseLeave={() => setHovered(null)}
              />
              {/* Label */}
              <text
                x={pos.cx}
                y={pos.cy + pos.r + 14}
                textAnchor="middle"
                fontSize="9"
                fontWeight="bold"
                fill={isSelected ? region.selectedColor : '#94a3b8'}
                style={{ pointerEvents: 'none', userSelect: 'none' }}
              >
                {tx(isAr ? region.nameAr : region.nameEn)}
              </text>
            </g>
          );
        })}

        {/* Grid lines for sci-fi look */}
        <g opacity="0.08" stroke="#38bdf8" strokeWidth="0.5">
          {[80, 160, 240, 320].map((x) => (
            <line key={x} x1={x} y1="0" x2={x} y2="460" />
          ))}
          {[80, 160, 240, 320, 400].map((y) => (
            <line key={y} x1="0" y1={y} x2="320" y2={y} />
          ))}
        </g>
      </svg>
    </div>
  );
};
