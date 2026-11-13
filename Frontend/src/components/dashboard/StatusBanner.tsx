import { useUiText } from "../../i18n/useUiText";
import React from 'react';
import { Check, AlertTriangle, AlertCircle, Radio, Sparkles } from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';

export const StatusBanner: React.FC = () => {
  const tx = useUiText();
  const { language, t } = useLanguage();
  const { overallStatus, activeScenario, vitals, isLivePulsing, alerts } = useTelemetry();
  const isAr = language === 'ar';

  const isAttention = overallStatus === 'MedicalAttention';
  const isEmergency = overallStatus === 'Emergency';

  const glowClass = isEmergency
    ? 'glass-glow-danger'
    : isAttention
    ? 'glass-glow-amber'
    : 'glass-glow-emerald';

  const badgeBg = isEmergency
    ? 'bg-red-500/20 text-red-400 border-red-400/50 shadow-[0_0_24px_rgba(239,68,68,0.35)]'
    : isAttention
    ? 'bg-amber-500/20 text-amber-300 border-amber-400/50 shadow-[0_0_24px_rgba(245,158,11,0.35)]'
    : 'bg-emerald-500/20 text-emerald-300 border-emerald-400/50 shadow-[0_0_24px_rgba(16,185,129,0.35)]';

  const hasData = Object.values(vitals).some(v=>v.value !== 'No data');
  const openAlerts = alerts.filter(a=>a.status!=='Resolved');
  const statusText = !hasData && openAlerts.length===0 ? 'No data' : isEmergency ? t.statusBanner.emergency : isAttention ? t.statusBanner.attention : t.statusBanner.stable;
  const statusSubtext = openAlerts.length
    ? openAlerts.map(a=>a.reason || a.message).join(' ')
    : !hasData ? 'No measurements have been loaded from the backend.'
    : 'Status is based on the latest backend readings. Demo thresholds require mission medical review.';

  const textColor = isEmergency
    ? 'text-red-400 drop-shadow-[0_0_14px_rgba(239,68,68,0.5)]'
    : isAttention
    ? 'text-amber-300 drop-shadow-[0_0_14px_rgba(245,158,11,0.5)]'
    : 'text-emerald-400 drop-shadow-[0_0_14px_rgba(16,185,129,0.5)]';

  const ecgStroke = isEmergency ? '#ef4444' : isAttention ? '#f59e0b' : '#10b981';

  const currentHr = Number(vitals.HR.value);
  // Dynamic ECG animation speed based on heart rate
  const animDuration = Number.isFinite(currentHr) && currentHr > 0 ? Math.max(0.7, Math.min(3.0, (60 / currentHr) * 2.2)) : 0;

  return (
    <div
      className={`w-full rounded-3xl px-6 py-5 md:px-8 md:py-5 flex flex-col md:flex-row items-center justify-between gap-5 transition-all duration-500 ${glowClass} relative overflow-hidden border border-white/10`}
    >
      {/* Status Info Group */}
      <div className="flex items-center gap-5 z-10 w-full md:w-auto">
        {/* Circular Glowing Status Checkmark Badge */}
        <div
          className={`w-14 h-14 md:w-16 md:h-16 rounded-full border-2 flex items-center justify-center shrink-0 transition-transform duration-300 ${badgeBg}`}
        >
          {isEmergency ? (
            <AlertCircle className="w-8 h-8 stroke-[2.4] animate-pulse" />
          ) : isAttention ? (
            <AlertTriangle className="w-8 h-8 stroke-[2.4]" />
          ) : (
            <Check className="w-8 h-8 stroke-[2.8]" />
          )}
        </div>

        {/* Status Text & Scenario Tag */}
        <div className="flex flex-col">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="text-xs md:text-sm font-bold text-slate-200">
              {tx(t.statusBanner.statusLabel)}
            </span>
            {/* Active Scenario Tag */}
            <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11px] font-extrabold bg-cyan-950/80 text-cyan-300 border border-cyan-400/40 shadow-sm">
              <span className="w-1.5 h-1.5 rounded-full bg-cyan-400 animate-pulse" />
              <span>{tx(isAr ? 'السيناريو النشط:' : 'Active Scenario:')}</span>
              <span className="text-white">{tx(isAr ? activeScenario.titleAr : activeScenario.titleEn)}</span>
            </span>
          </div>

          <div className={`text-2xl md:text-3xl font-extrabold leading-tight mt-0.5 ${textColor}`}>
            {tx(statusText)}
          </div>
          <p className="text-xs md:text-sm text-slate-300 font-medium mt-1">
            {tx(statusSubtext)}
          </p>
        </div>
      </div>

      {/* Dynamic Animated ECG Waveform with Live Beat rate */}
      <div className="w-full md:w-72 h-16 flex flex-col items-center md:items-end justify-center z-10">
        <div className="flex items-center gap-2 mb-1 text-[11px] font-mono text-cyan-300">
          <span className="flex items-center gap-1">
            <Radio className="w-3 h-3 text-cyan-400 animate-pulse" />
            <span>{tx(isLivePulsing ? (isAr ? 'بث حي 1Hz' : 'Live 1Hz') : (isAr ? 'متوقف' : 'Paused'))}</span>
          </span>
          <span className="text-slate-400">|</span>
          <span className="font-bold text-white">{tx(vitals.HR.value)} {tx(" bpm")}</span>
        </div>

        <svg
          viewBox="0 0 300 70"
          className="w-full max-w-[270px] h-full overflow-visible drop-shadow-[0_0_10px_rgba(16,185,129,0.45)]"
        >
          <path
            d="M0 35 L55 35 L68 35 L78 10 L88 60 L98 18 L108 46 L118 35 L165 35 L178 35 L188 10 L198 60 L208 18 L218 46 L228 35 L300 35"
            fill="none"
            stroke={ecgStroke}
            strokeWidth="3"
            strokeLinecap="round"
            strokeLinejoin="round"
            style={{
              strokeDasharray: 600,
              animation: animDuration ? `ecg-sweep ${animDuration}s linear infinite` : 'none',
            }}
          />
        </svg>
      </div>
    </div>
  );
};