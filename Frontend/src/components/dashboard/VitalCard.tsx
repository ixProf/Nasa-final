import { useUiText } from "../../i18n/useUiText";
import React from 'react';
import { Check, AlertCircle } from 'lucide-react';
import { VitalSignCardData } from '../../types/telemetry';
import { useLanguage } from '../../context/LanguageContext';

interface VitalCardProps {
  data: VitalSignCardData;
  onSelect?: () => void;
}

export const VitalCard: React.FC<VitalCardProps> = ({ data, onSelect }) => {
  const tx = useUiText();
  const { language, isRtl } = useLanguage();
  const isAr = language === 'ar';

  const title = isAr ? data.titleAr : data.titleEn;
  const unit = isAr ? data.unitAr : data.unitEn;

  const isRoutine = data.status === 'Routine';
  const isEmergency = data.status === 'Emergency';

  const renderHologram = () => {
    switch (data.iconType) {
      case 'heart':
        return (
          <div className="relative w-16 h-16 md:w-20 md:h-20 flex items-center justify-center shrink-0">
            <div className="absolute inset-1 bg-rose-500/25 rounded-full blur-xl" />
            <svg
              viewBox="0 0 80 80"
              className="w-16 h-16 drop-shadow-[0_0_14px_rgba(244,63,94,0.75)] animate-heartbeat"
            >
              <defs>
                <radialGradient id="heartHolo" cx="50%" cy="40%" r="60%">
                  <stop offset="0%" stopColor="#fda4af" />
                  <stop offset="45%" stopColor="#f43f5e" />
                  <stop offset="100%" stopColor="#881337" />
                </radialGradient>
              </defs>
              <path
                d="M34 18 V10 C34 8 38 8 38 10 V17 M42 19 V12 C42 10 46 10 46 12 V20"
                stroke="#fb7185"
                strokeWidth="3.5"
                strokeLinecap="round"
                fill="none"
              />
              <path
                d="M40 66 C40 66 16 50 16 31 C16 21 24 15 32 17 C36 18 39 21 40 24 C41 21 44 18 48 17 C56 15 64 21 64 31 C64 50 40 66 40 66 Z"
                fill="url(#heartHolo)"
                stroke="#fecdd3"
                strokeWidth="1.2"
              />
              <path
                d="M36 26 Q42 38 35 52 M42 32 Q48 40 50 46 M35 38 Q28 43 26 48"
                stroke="#ffe4e6"
                strokeWidth="1.3"
                strokeLinecap="round"
                fill="none"
                opacity="0.75"
              />
            </svg>
          </div>
        );

      case 'lungs':
        return (
          <div className="relative w-16 h-16 md:w-20 md:h-20 flex items-center justify-center shrink-0">
            <div className="absolute inset-1 bg-cyan-400/25 rounded-full blur-xl" />
            <svg
              viewBox="0 0 80 80"
              className="w-16 h-16 drop-shadow-[0_0_14px_rgba(34,211,238,0.75)]"
            >
              <defs>
                <linearGradient id="lungHolo" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0%" stopColor="#a5f3fc" stopOpacity="0.95" />
                  <stop offset="50%" stopColor="#06b6d4" stopOpacity="0.8" />
                  <stop offset="100%" stopColor="#1e3a8a" stopOpacity="0.9" />
                </linearGradient>
              </defs>
              <path
                d="M40 10 V28 M40 26 L32 34 M40 26 L48 34"
                stroke="#67e8f9"
                strokeWidth="3"
                strokeLinecap="round"
              />
              <path
                d="M35 22 C22 22 14 34 14 50 C14 60 20 64 28 62 C33 60 36 55 36 46 V24 C36 22 35 22 35 22 Z"
                fill="url(#lungHolo)"
                stroke="#67e8f9"
                strokeWidth="1.3"
              />
              <path
                d="M45 22 C58 22 66 34 66 50 C66 60 60 64 52 62 C47 60 44 55 44 46 V24 C44 22 45 22 45 22 Z"
                fill="url(#lungHolo)"
                stroke="#67e8f9"
                strokeWidth="1.3"
              />
              <path
                d="M32 34 Q25 40 22 48 M28 38 Q26 46 29 53 M48 34 Q55 40 58 48 M52 38 Q54 46 51 53"
                stroke="#cffafe"
                strokeWidth="1.1"
                fill="none"
                opacity="0.75"
              />
            </svg>
          </div>
        );

      case 'blood':
        return (
          <div className="relative w-16 h-16 md:w-20 md:h-20 flex items-center justify-center shrink-0">
            <div className="absolute inset-1 bg-purple-500/25 rounded-full blur-xl" />
            <svg
              viewBox="0 0 80 80"
              className="w-16 h-16 drop-shadow-[0_0_14px_rgba(192,132,252,0.7)]"
            >
              <defs>
                <linearGradient id="vesselHolo" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0%" stopColor="#f43f5e" stopOpacity="0.9" />
                  <stop offset="50%" stopColor="#9333ea" stopOpacity="0.8" />
                  <stop offset="100%" stopColor="#3b0764" stopOpacity="0.95" />
                </linearGradient>
              </defs>
              <path
                d="M16 54 C26 36 44 24 64 16 L70 28 C52 36 36 48 26 66 Z"
                fill="url(#vesselHolo)"
                stroke="#f0abfc"
                strokeWidth="1.3"
              />
              <ellipse
                cx="34"
                cy="48"
                rx="5.5"
                ry="3.5"
                transform="rotate(-35 34 48)"
                fill="#fda4af"
              />
              <ellipse
                cx="46"
                cy="37"
                rx="5"
                ry="3.2"
                transform="rotate(-30 46 37)"
                fill="#fb7185"
              />
              <ellipse
                cx="57"
                cy="28"
                rx="4.5"
                ry="2.8"
                transform="rotate(-25 57 28)"
                fill="#fecdd3"
              />
            </svg>
          </div>
        );

      case 'glucose':
        return (
          <div className="relative w-16 h-16 md:w-20 md:h-20 flex items-center justify-center shrink-0">
            <div className="absolute inset-1 bg-emerald-500/25 rounded-full blur-xl" />
            <svg
              viewBox="0 0 80 80"
              className="w-16 h-16 drop-shadow-[0_0_14px_rgba(52,211,153,0.7)]"
            >
              <polygon
                points="40,14 62,27 62,53 40,66 18,53 18,27"
                fill="rgba(16, 185, 129, 0.25)"
                stroke="#6ee7b7"
                strokeWidth="2"
              />
              <circle cx="40" cy="14" r="3.5" fill="#a7f3d0" />
              <circle cx="62" cy="27" r="3.5" fill="#a7f3d0" />
              <circle cx="62" cy="53" r="3.5" fill="#a7f3d0" />
              <circle cx="40" cy="66" r="3.5" fill="#a7f3d0" />
              <circle cx="18" cy="53" r="3.5" fill="#a7f3d0" />
              <circle cx="18" cy="27" r="3.5" fill="#a7f3d0" />
            </svg>
          </div>
        );

      default:
        return (
          <div className="relative w-16 h-16 md:w-20 md:h-20 flex items-center justify-center shrink-0">
            <div className="absolute inset-1 bg-amber-500/25 rounded-full blur-xl" />
            <svg
              viewBox="0 0 80 80"
              className="w-16 h-16 drop-shadow-[0_0_14px_rgba(251,191,36,0.75)]"
            >
              <defs>
                <linearGradient id="thermHolo" x1="0" y1="1" x2="0" y2="0">
                  <stop offset="0%" stopColor="#f97316" />
                  <stop offset="60%" stopColor="#fbbf24" />
                  <stop offset="100%" stopColor="#fef08a" />
                </linearGradient>
              </defs>
              <path
                d="M34 16 C34 12.5 46 12.5 46 16 V45 C50 48 52 53 50 58 C48 64 41 67 35 65 C29 63 27 56 30 50 C31 48 32 46 34 45 Z"
                fill="rgba(15, 23, 42, 0.7)"
                stroke="#fde68a"
                strokeWidth="2"
              />
              <rect x="38" y="24" width="4" height="26" rx="2" fill="url(#thermHolo)" />
              <circle cx="40" cy="55" r="7" fill="url(#thermHolo)" />
              <path
                d="M49 22 H53 M49 29 H52 M49 36 H53"
                stroke="#fcd34d"
                strokeWidth="1.5"
                strokeLinecap="round"
              />
            </svg>
          </div>
        );
    }
  };

  const badgeBg = isEmergency
    ? 'bg-red-500/25 text-red-200 border-red-400/40'
    : isRoutine
    ? 'bg-emerald-500/20 text-emerald-300 border-emerald-400/35'
    : 'bg-amber-500/25 text-amber-200 border-amber-400/40';

  return (
    <div
      onClick={onSelect}
      dir="ltr"
      className="glass-panel rounded-3xl p-4 md:p-5 flex items-center justify-between gap-3 transition-all duration-300 hover:-translate-y-1 cursor-pointer group relative overflow-hidden"
    >
      {/* Left Side: 3D Hologram Illustration */}
      {renderHologram()}

      {/* Right Side: Metric Title, Value, Unit & Normal Range Pill (Matching Reference Artwork) */}
      <div
        className="flex flex-col items-end text-right justify-between min-w-0"
        dir={isRtl ? 'rtl' : 'ltr'}
      >
        <span className="text-xs md:text-sm font-bold text-slate-200 truncate">
          {tx(title)}
        </span>

        <div className="mt-1 flex items-baseline gap-1">
          <span
            dir="ltr"
            className="text-2xl md:text-3xl font-extrabold text-white tracking-tight tabular-nums"
          >
            {tx(data.value)}
            {tx(data.secondaryValue ? ` / ${data.secondaryValue}` : '')}
          </span>
        </div>

        <span className="text-[11px] text-slate-300/85 font-semibold mt-0.5 min-h-[16px]">
          {tx(unit)}
        </span>

        {/* Normal Range Capsule Pill */}
        <div
          className={`mt-2.5 inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11px] font-bold border tabular-nums ${badgeBg}`}
        >
          {isRoutine ? (
            <Check className="w-3 h-3 stroke-[3]" />
          ) : (
            <AlertCircle className="w-3 h-3 stroke-[2.5]" />
          )}
          <span dir="ltr">{tx(data.rangeText)}</span>
        </div>
      </div>
    </div>
  );
};
