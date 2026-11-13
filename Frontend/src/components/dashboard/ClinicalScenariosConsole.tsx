import { useUiText } from "../../i18n/useUiText";
import React, { useState } from 'react';
import {
  Radio,
  Play,
  Pause,
  AlertCircle,
  AlertTriangle,
  CheckCircle2,
  Stethoscope,
  Flame,
  Moon,
  Dumbbell,
  Compass,
  ArrowRight,
  ShieldCheck,
  Sparkles,
  ChevronDown,
  ChevronUp,
  MessageSquare,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { ActivePage, ScenarioId } from '../../types/telemetry';

interface ClinicalScenariosConsoleProps {
  onNavigate?: (page: ActivePage) => void;
}

export const ClinicalScenariosConsole: React.FC<ClinicalScenariosConsoleProps> = ({
  onNavigate,
}) => {
  const tx = useUiText();
  const { language } = useLanguage();
  const {
    activeScenario,
    allScenarios,
    activateScenario,
    isLivePulsing,
    toggleLivePulsing,
    currentUserName,
    currentUserCode,
  } = useTelemetry();
  const isAr = language === 'ar';

  // Collapsed by default to declutter the astronaut home dashboard
  const [isDetailsOpen, setIsDetailsOpen] = useState(false);

  const getScenarioIcon = (id: ScenarioId) => {
    switch (id) {
      case 'nominal':
        return <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />;
      case 'eva_sortie':
        return <Flame className="w-3.5 h-3.5 text-amber-400" />;
      case 'hypoxia':
        return <AlertCircle className="w-3.5 h-3.5 text-rose-400" />;
      case 'sas_sickness':
        return <Compass className="w-3.5 h-3.5 text-purple-400" />;
      case 'countermeasure_exercise':
        return <Dumbbell className="w-3.5 h-3.5 text-cyan-400" />;
      case 'deep_sleep':
        return <Moon className="w-3.5 h-3.5 text-blue-300" />;
    }
  };

  const getSeverityBadge = (sev: 'Routine' | 'MedicalAttention' | 'Emergency') => {
    if (sev === 'Emergency') {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-extrabold bg-rose-500/20 text-rose-300 border border-rose-400/50 shadow-[0_0_12px_rgba(244,63,94,0.3)] animate-pulse">
          <AlertCircle className="w-3 h-3 text-rose-400" />
          <span>{tx(isAr ? 'طوارئ حرجة' : 'Emergency')}</span>
        </span>
      );
    }
    if (sev === 'MedicalAttention') {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-extrabold bg-amber-500/20 text-amber-300 border border-amber-400/40">
          <AlertTriangle className="w-3 h-3 text-amber-400" />
          <span>{tx(isAr ? 'تحت الملاحظة' : 'Attention')}</span>
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-extrabold bg-emerald-500/20 text-emerald-300 border border-emerald-400/40">
        <CheckCircle2 className="w-3 h-3 text-emerald-400" />
        <span>{tx(isAr ? 'مستقر' : 'Nominal')}</span>
      </span>
    );
  };

  return (
    <div className="glass-panel rounded-2xl p-3 md:p-4 flex flex-col gap-3 relative overflow-hidden animate-fade-in border border-cyan-400/25 bg-slate-950/70 shadow-lg">
      {/* ── Slim Scenario Ribbon ── */}
      <div className="flex flex-col lg:flex-row items-stretch lg:items-center justify-between gap-2.5">
        {/* Scenario Selection Chips */}
        <div className="flex items-center gap-2 flex-1 min-w-0 overflow-x-auto no-scrollbar py-0.5">
          <div className="flex items-center gap-1.5 shrink-0 px-2.5 py-1 rounded-xl bg-cyan-950/70 border border-cyan-400/30 text-cyan-300">
            <Radio className="w-3.5 h-3.5 animate-pulse" />
            <span className="text-[11px] font-bold whitespace-nowrap">
              {tx(isAr ? 'المحاكاة السريرية:' : 'Simulations:')}
            </span>
          </div>

          <div className="flex items-center gap-1.5 flex-nowrap">
            {allScenarios.map((sc) => {
              const isSelected = sc.id === activeScenario.id;
              return (
                <button
                  key={sc.id}
                  type="button"
                  onClick={() => activateScenario(sc.id)}
                  title={tx(`${isAr ? sc.titleAr : sc.titleEn} (${sc.targetVitals.hr} bpm | SpO2 ${sc.targetVitals.spo2}%)`)}
                  className={`px-2.5 py-1.5 rounded-xl border text-xs font-bold transition-all flex items-center gap-1.5 shrink-0 cursor-pointer ${
                    isSelected
                      ? 'bg-gradient-to-r from-cyan-600/35 to-blue-600/40 border-cyan-400 text-white shadow-[0_0_15px_rgba(34,211,238,0.35)] ring-1 ring-cyan-400 scale-[1.02]'
                      : 'bg-slate-900/70 border-white/10 text-slate-300 hover:text-white hover:border-cyan-400/40 hover:bg-slate-800'
                  }`}
                >
                  <span className="shrink-0">{getScenarioIcon(sc.id)}</span>
                  <span className="whitespace-nowrap leading-none">
                    {tx(isAr ? sc.titleAr : sc.titleEn)}
                  </span>
                  {isSelected && (
                    <span className="w-1.5 h-1.5 rounded-full bg-cyan-400 animate-ping shrink-0" />
                  )}
                </button>
              );
            })}
          </div>
        </div>

        {/* Live Controls & Details Expand Toggle */}
        <div className="flex items-center justify-end gap-2 shrink-0 pt-1 lg:pt-0 border-t lg:border-t-0 border-white/10">
          {/* 1Hz Live Stream Toggle */}
          <button
            type="button"
            onClick={toggleLivePulsing}
            title={tx(isAr ? 'تشغيل أو إيقاف النبض الفسيولوجي المباشر' : 'Toggle 1Hz live telemetry stream')}
            className={`px-2.5 py-1.5 rounded-xl border text-[11px] font-bold flex items-center gap-1.5 transition-all cursor-pointer ${
              isLivePulsing
                ? 'bg-emerald-500/15 border-emerald-400/40 text-emerald-200'
                : 'bg-slate-800 border-white/10 text-slate-400 hover:text-white'
            }`}
          >
            {isLivePulsing ? (
              <>
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-ping" />
                <Pause className="w-3 h-3" />
                <span>{tx(isAr ? 'بث 1Hz نشط' : '1Hz Live')}</span>
              </>
            ) : (
              <>
                <Play className="w-3 h-3 text-cyan-400" />
                <span>{tx(isAr ? 'استئناف 1Hz' : 'Resume')}</span>
              </>
            )}
          </button>

          {/* Toggle Expandable Diagnosis & Protocols Drawer */}
          <button
            type="button"
            onClick={() => setIsDetailsOpen(!isDetailsOpen)}
            className={`px-2.5 py-1.5 rounded-xl border text-[11px] font-bold flex items-center gap-1 transition-all cursor-pointer ${
              isDetailsOpen
                ? 'bg-cyan-500/20 border-cyan-400/50 text-cyan-200'
                : 'bg-slate-900/80 border-white/10 text-slate-300 hover:text-white hover:border-cyan-400/30'
            }`}
          >
            <span>{tx(isAr ? (isDetailsOpen ? 'إخفاء التفاصيل' : 'تفاصيل السيناريو') : (isDetailsOpen ? 'Hide Details' : 'Details'))}</span>
            {isDetailsOpen ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
          </button>
        </div>
      </div>

      {/* ── Collapsible Clinical Details Drawer ── */}
      {isDetailsOpen && (
        <div className="pt-3 border-t border-white/10 flex flex-col gap-3 animate-fade-in">
          {/* Active Scenario Banner */}
          <div className="p-3 rounded-xl bg-slate-900/90 border border-cyan-400/30 flex flex-col md:flex-row items-start md:items-center justify-between gap-2.5">
            <div className="flex items-center gap-2.5 flex-wrap">
              <span className="text-xs font-bold text-cyan-300 flex items-center gap-1.5">
                <Sparkles className="w-3.5 h-3.5 text-cyan-400" />
                {tx(isAr ? 'السيناريو السريري المُطبّق حالياً:' : 'Active Simulation Scenario:')}
              </span>
              <span className="text-xs font-extrabold text-white">
                {tx(isAr ? activeScenario.titleAr : activeScenario.titleEn)}
              </span>
              <span className="text-xs text-slate-300 hidden sm:inline">
                — {tx(isAr ? activeScenario.subtitleAr : activeScenario.subtitleEn)}
              </span>
            </div>
            <div>{getSeverityBadge(activeScenario.severity)}</div>
          </div>

          {/* Diagnosis & Directive */}
          <div className="p-3 rounded-xl bg-cyan-950/40 border border-cyan-400/25 flex items-start gap-2.5">
            <div className="w-7 h-7 rounded-lg bg-cyan-500/20 border border-cyan-400/40 flex items-center justify-center text-cyan-300 shrink-0 mt-0.5">
              <Stethoscope className="w-3.5 h-3.5" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between gap-2">
                <span className="text-xs font-bold text-cyan-200">
                  {tx(isAr ? 'التشخيص الطبي وتوجيه طبيب الرحلة:' : 'Clinical Diagnosis & Directive:')}
                </span>
                <span className="text-[10px] font-mono text-cyan-400 bg-black/40 px-2 py-0.5 rounded">
                  {tx(" ICD-10 Aerospace ")}</span>
              </div>
              <p className="text-xs font-bold text-white mt-0.5">
                {tx(isAr ? activeScenario.diagnosisAr : activeScenario.diagnosisEn)}
              </p>
              <p className="text-xs text-cyan-100/90 mt-1 italic">
                «{tx(isAr ? activeScenario.flightSurgeonDirectiveAr : activeScenario.flightSurgeonDirectiveEn)}»
              </p>
            </div>
          </div>

          {/* Symptoms & Countermeasures Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2.5">
            {/* Symptoms */}
            <div className="p-3 rounded-xl bg-slate-950/80 border border-white/10 flex flex-col justify-between gap-2">
              <div>
                <div className="flex items-center justify-between gap-2 mb-1.5">
                  <span className="text-xs font-bold text-amber-200 flex items-center gap-1.5">
                    <AlertTriangle className="w-3.5 h-3.5 text-amber-400" />
                    <span>{tx(isAr ? 'الأعراض المرتبطة بالسيناريو:' : 'Associated Symptoms:')}</span>
                  </span>
                  <span className="text-[10px] text-slate-400">
                    {tx(activeScenario.symptomsAr.length)} {tx(isAr ? 'أعراض' : 'logged')}
                  </span>
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {(isAr ? activeScenario.symptomsAr : activeScenario.symptomsEn).map((sym, idx) => (
                    <span
                      key={idx}
                      className="text-[11px] px-2 py-0.5 rounded-lg bg-amber-500/10 border border-amber-400/30 text-amber-200 font-semibold"
                    >
                      • {tx(sym)}
                    </span>
                  ))}
                </div>
              </div>

              {onNavigate && (
                <button
                  type="button"
                  onClick={() => onNavigate('symptoms')}
                  className="self-start text-[11px] font-bold text-amber-300 hover:text-amber-200 flex items-center gap-1 transition-all mt-1 cursor-pointer"
                >
                  <span>{tx(isAr ? 'متابعة فرز الأعراض في صفحة الأعراض' : 'Open Symptom Triage Desk')}</span>
                  <ArrowRight className="w-3 h-3" />
                </button>
              )}
            </div>

            {/* Countermeasures */}
            <div className="p-3 rounded-xl bg-slate-950/80 border border-white/10 flex flex-col justify-between gap-2">
              <div>
                <div className="flex items-center justify-between gap-2 mb-1.5">
                  <span className="text-xs font-bold text-emerald-200 flex items-center gap-1.5">
                    <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />
                    <span>{tx(isAr ? 'البروتوكولات السريرية العاجلة:' : 'Clinical Countermeasures:')}</span>
                  </span>
                </div>
                <div className="space-y-1">
                  {(isAr ? activeScenario.clinicalActionsAr : activeScenario.clinicalActionsEn)
                    .slice(0, 2)
                    .map((act, idx) => (
                      <div
                        key={idx}
                        className="text-[11px] text-slate-200 flex items-start gap-1.5 leading-relaxed"
                      >
                        <span className="text-emerald-400 font-bold shrink-0">{tx(idx + 1)}.</span>
                        <span>{tx(act)}</span>
                      </div>
                    ))}
                </div>
              </div>

              {onNavigate && (
                <button
                  type="button"
                  onClick={() => onNavigate('recommendations')}
                  className="self-start text-[11px] font-bold text-emerald-300 hover:text-emerald-200 flex items-center gap-1 transition-all mt-1 cursor-pointer"
                >
                  <span>{tx(isAr ? 'عرض جدول التوصيات الطبية' : 'View Clinical Recommendations')}</span>
                  <ArrowRight className="w-3 h-3" />
                </button>
              )}
            </div>
          </div>

          {/* Quick Deep Link Shortcuts */}
          {onNavigate && (
            <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-white/10 text-xs">
              <span className="text-[11px] text-slate-300">
                {tx(isAr ? 'الرائد الحالي:' : 'Astronaut:')}{tx(' ')}
                <strong className="text-cyan-300">{tx(currentUserName)}</strong> ({tx(currentUserCode)})
              </span>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => onNavigate('alerts')}
                  className="px-2.5 py-1 rounded-lg bg-rose-500/15 hover:bg-rose-500/25 border border-rose-400/30 text-rose-200 text-[11px] font-bold flex items-center gap-1 transition-all cursor-pointer"
                >
                  <AlertCircle className="w-3 h-3 text-rose-400" />
                  <span>{tx(isAr ? 'الإنذارات' : 'Alerts')}</span>
                </button>
                <button
                  type="button"
                  onClick={() => onNavigate('chat')}
                  className="px-2.5 py-1 rounded-lg bg-cyan-500/15 hover:bg-cyan-500/25 border border-cyan-400/30 text-cyan-200 text-[11px] font-bold flex items-center gap-1 transition-all cursor-pointer"
                >
                  <MessageSquare className="w-3 h-3 text-cyan-400" />
                  <span>{tx(isAr ? 'مراسلة الطبيب' : 'Chat Surgeon')}</span>
                </button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
