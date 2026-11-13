import { useUiText } from "../../i18n/useUiText";
import React, { useState } from 'react';
import { Activity } from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';

export const HeartRateChart: React.FC = () => {
  const tx = useUiText();
  const { language, t } = useLanguage();
  const { heartRateHistory } = useTelemetry();
  const isAr = language === 'ar';

  const [activePointIndex, setActivePointIndex] = useState<number>(3);

  const width = 600;
  const height = 165;
  const padX = 40;
  const padY = 26;

  const values = heartRateHistory.map((h) => h.value);
  const minVal = values.length > 0 ? Math.min(...values) : 60;
  const maxVal = values.length > 0 ? Math.max(...values) : 90;
  const minY = Math.max(30, Math.floor((minVal - 10) / 10) * 10);
  const maxY = Math.min(190, Math.ceil((maxVal + 10) / 10) * 10);

  const points = heartRateHistory.map((item, index) => {
    const x = padX + (index * (width - 2 * padX)) / Math.max(heartRateHistory.length - 1, 1);
    const clampedVal = Math.max(minY, Math.min(maxY, item.value));
    const y = height - padY - ((clampedVal - minY) / Math.max(1, maxY - minY)) * (height - 2 * padY);
    return { x, y, ...item };
  });

  const generateSmoothPath = (pts: { x: number; y: number }[]) => {
    if (pts.length === 0) return '';
    let d = `M ${pts[0].x} ${pts[0].y}`;

    for (let i = 0; i < pts.length - 1; i++) {
      const p0 = pts[i === 0 ? i : i - 1];
      const p1 = pts[i];
      const p2 = pts[i + 1];
      const p3 = pts[i + 2] || p2;

      const cp1x = p1.x + (p2.x - p0.x) / 6;
      const cp1y = p1.y + (p2.y - p0.y) / 6;
      const cp2x = p2.x - (p3.x - p1.x) / 6;
      const cp2y = p2.y - (p3.y - p1.y) / 6;

      d += ` C ${cp1x} ${cp1y}, ${cp2x} ${cp2y}, ${p2.x} ${p2.y}`;
    }
    return d;
  };

  const gridLevels = [
    maxY,
    Math.round(minY + (maxY - minY) * 0.66),
    Math.round(minY + (maxY - minY) * 0.33),
    minY,
  ];

  const linePath = generateSmoothPath(points);
  const areaPath =
    points.length > 0
      ? `${linePath} L ${points[points.length - 1].x} ${height - padY} L ${points[0].x} ${
          height - padY
        } Z`
      : '';

  const activePoint = points[activePointIndex] || points[points.length - 1];
  const latestVal = heartRateHistory[heartRateHistory.length - 1]?.value || 72;

  return (
    <div className="glass-panel rounded-3xl p-5 md:p-6 flex flex-col justify-between h-full transition-all duration-300 relative overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between gap-2.5 mb-2 z-10 flex-wrap">
        <div className="flex items-center gap-2.5">
          <Activity className="w-5 h-5 text-cyan-400" />
          <h3 className="text-sm md:text-base font-bold text-white">
            {tx(t.chart.title)}
          </h3>
        </div>
        <div className="flex items-center gap-2 text-xs font-mono bg-cyan-950/70 border border-cyan-400/30 px-2.5 py-0.5 rounded-full text-cyan-200">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-ping" />
          <span>{tx(isAr ? 'النبض الحي:' : 'Live Heart Rate:')}</span>
          <span className="font-extrabold text-white">{tx(latestVal)} {tx(" bpm")}</span>
        </div>
      </div>

      {/* SVG Chart Area */}
      <div className="w-full relative z-10 select-none" dir="ltr">
        <svg
          viewBox={`0 0 ${width} ${height}`}
          className="w-full h-auto overflow-visible cursor-pointer"
        >
          <defs>
            <linearGradient id="heartRateGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#00d2ff" stopOpacity="0.42" />
              <stop offset="75%" stopColor="#0066cc" stopOpacity="0.08" />
              <stop offset="100%" stopColor="#00d2ff" stopOpacity="0" />
            </linearGradient>

            <filter id="cyanGlow" x="-20%" y="-20%" width="140%" height="140%">
              <feDropShadow dx="0" dy="0" stdDeviation="3.5" floodColor="#00f2fe" floodOpacity="0.75" />
            </filter>
          </defs>

          {/* Horizontal Grid Lines & Y-Axis Labels */}
          {gridLevels.map((level) => {
            const y = height - padY - ((level - minY) / Math.max(1, maxY - minY)) * (height - 2 * padY);
            return (
              <g key={level} className="opacity-45">
                <line
                  x1={padX}
                  y1={y}
                  x2={width - padX}
                  y2={y}
                  stroke="#334155"
                  strokeWidth="1"
                  strokeDasharray="4 4"
                />
                <text
                  x={padX - 10}
                  y={y + 4}
                  textAnchor="end"
                  fill="#cbd5e1"
                  fontSize="11"
                  fontWeight="600"
                >
                  {tx(level)}
                </text>
              </g>
            );
          })}

          {/* Area Fill */}
          {areaPath && <path d={areaPath} fill="url(#heartRateGradient)" />}

          {/* Smooth Wave Line */}
          {linePath && (
            <path
              d={linePath}
              fill="none"
              stroke="#00d2ff"
              strokeWidth="3.2"
              strokeLinecap="round"
              strokeLinejoin="round"
              filter="url(#cyanGlow)"
            />
          )}

          {/* Active Tooltip & Beacon */}
          {activePoint && (
            <g>
              <line
                x1={activePoint.x}
                y1={padY}
                x2={activePoint.x}
                y2={height - padY}
                stroke="#00f2fe"
                strokeWidth="1.5"
                strokeDasharray="2 3"
                opacity="0.75"
              />

              <g transform={`translate(${activePoint.x}, ${Math.max(activePoint.y - 40, 8)})`}>
                <rect
                  x="-46"
                  y="-8"
                  width="92"
                  height="34"
                  rx="10"
                  fill="#0b1838"
                  stroke="#38bdf8"
                  strokeWidth="1.2"
                />
                <text
                  x="0"
                  y="7"
                  textAnchor="middle"
                  fill="#ffffff"
                  fontSize="12.5"
                  fontWeight="800"
                >
                  {tx(activePoint.value)}
                </text>
                <text
                  x="0"
                  y="19"
                  textAnchor="middle"
                  fill="#7dd3fc"
                  fontSize="9"
                  fontWeight="600"
                >
                  {tx(isAr ? 'نبضة / دقيقة' : 'bpm')}
                </text>
              </g>

              <circle
                cx={activePoint.x}
                cy={activePoint.y}
                r="5.5"
                fill="#ffffff"
                stroke="#00d2ff"
                strokeWidth="2.5"
              />
            </g>
          )}

          {/* Interactive Hit Targets */}
          {points.map((pt, idx) => (
            <circle
              key={idx}
              cx={pt.x}
              cy={pt.y}
              r="14"
              fill="transparent"
              className="cursor-pointer"
              onMouseEnter={() => setActivePointIndex(idx)}
              onClick={() => setActivePointIndex(idx)}
            />
          ))}
        </svg>

        {/* X-Axis Time Labels */}
        <div className="flex justify-between px-7 mt-1.5 text-xs font-semibold text-slate-300">
          {heartRateHistory.map((pt, index) => (
            <button
              key={index}
              onClick={() => setActivePointIndex(index)}
              className={`transition-colors ${
                activePointIndex === index
                  ? 'text-cyan-300 font-bold'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {tx(isAr ? pt.timeLabelAr : pt.timeLabelEn)}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
};
