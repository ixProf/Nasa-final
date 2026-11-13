import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import React, { useState, useEffect } from 'react';
import {
  BookOpen,
  SlidersHorizontal,
  Activity,
  HelpCircle,
  CheckCircle2,
  AlertTriangle,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { api } from '../../services/api';
import { VitalCode } from '../../types/telemetry';

interface MetricGuideData {
  code: VitalCode;
  nameAr: string;
  nameEn: string;
  unit: string;
  normalRange: string;
  emergencyRange: string;
  whatIsItAr: string;
  whatIsItEn: string;
  whyItMattersAr: string;
  whyItMattersEn: string;
  howToMeasureAr: string;
  howToMeasureEn: string;
  interpretationAr: string;
  interpretationEn: string;
  actionAr: string;
  actionEn: string;
  icon: React.ReactNode;
}

export const VitalGuidesRangesPage: React.FC = () => {
  const tx = useUiText();
  const { language } = useLanguage();
  const isAr = language === 'ar';
  const [selectedCode, setSelectedCode] = useState<VitalCode>('HR');

  const [guidesCatalog, setGuides] = useState<MetricGuideData[]>([]);
  const [loadError, setLoadError] = useState('');
  const [reload, setReload] = useState(0);
  useEffect(() => { let active=true;
    api.getGuides().then(data => {if(active){setGuides(data.map(g=>({...g,icon:<Activity className="w-4 h-4"/>})));setLoadError('');}}).catch(e=>{if(active)setLoadError(e.message);});
    return()=>{active=false;};
  }, [reload]);
  const activeGuide = guidesCatalog.find((g) => g.code === selectedCode) || guidesCatalog[0];

  if (!activeGuide) return <div className="glass-panel p-5 rounded-2xl" role="status">{loadError || (isAr ? 'جارٍ تحميل الأدلة من الخادم…' : 'Loading guides from the server…')}{loadError && <AsyncButton onClick={()=>setReload(n=>n+1)}>{isAr ? 'إعادة المحاولة' : 'Retry'}</AsyncButton>}</div>;
  const fiveQuestions = [
    {
      titleAr: '1. ما هو هذا المؤشر؟',
      titleEn: '1. What Is It?',
      body: isAr ? activeGuide.whatIsItAr : activeGuide.whatIsItEn,
    },
    {
      titleAr: '2. لماذا يهم في الفضاء؟',
      titleEn: '2. Why It Matters in Space',
      body: isAr ? activeGuide.whyItMattersAr : activeGuide.whyItMattersEn,
    },
    {
      titleAr: '3. كيف يتم قياسه بدقة؟',
      titleEn: '3. How to Measure Accurately',
      body: isAr ? activeGuide.howToMeasureAr : activeGuide.howToMeasureEn,
    },
    {
      titleAr: '4. كيف تفسر القراءة؟',
      titleEn: '4. How to Interpret Readings',
      body: isAr ? activeGuide.interpretationAr : activeGuide.interpretationEn,
    },
    {
      titleAr: '5. الإجراء الموصى به عند التغير',
      titleEn: '5. Recommended Action Guidance',
      body: isAr ? activeGuide.actionAr : activeGuide.actionEn,
    },
  ];

  return (
    <div className="flex flex-col gap-3.5 animate-fade-in">
      <p className="text-xs text-amber-200 rounded-xl border border-amber-400/20 p-3">{isAr ? 'نطاقات توضيحية للتعليم، وليست حدودًا طبية معتمدة لرحلة فضائية. راجع المصدر وخطة المهمة مع المراقب الطبي.' : 'Educational demonstration ranges, not approved spaceflight medical limits. Review the source and mission plan with your medical monitor.'}</p>
      {api.cachedGuideDate && <p role="status" className="text-amber-200 text-xs">{isAr ? 'نسخة محفوظة دون اتصال — آخر تحديث: ' : 'Offline cached copy — last updated: '}{new Date(api.cachedGuideDate).toLocaleString(language)}</p>}
      {/* Metric Selector Strip */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-2.5">
        {guidesCatalog.map((item) => {
          const active = item.code === selectedCode;
          return (
            <AsyncButton
              key={item.code}
              type="button"
              onClick={() => setSelectedCode(item.code)}
              className={`glass-panel rounded-2xl p-3 text-start border transition-all ${
                active
                  ? 'border-cyan-400/60 bg-cyan-500/15 shadow-[0_0_20px_rgba(34,211,238,0.18)]'
                  : 'border-white/10 hover:border-white/25'
              }`}
            >
              <div className="flex items-center justify-between mb-1.5">
                <div className="w-7 h-7 rounded-lg bg-white/5 border border-white/10 flex items-center justify-center">
                  {item.icon}
                </div>
                <span className="text-[11px] font-bold text-cyan-300">{tx(item.code)}</span>
              </div>
              <p className="text-xs font-bold text-white truncate">
                {tx(isAr ? item.nameAr : item.nameEn)}
              </p>
              <p className="text-[10px] text-emerald-300 mt-0.5" dir="ltr">
                {tx(item.normalRange)}
              </p>
            </AsyncButton>
          );
        })}
      </div>

      {/* 5 Core Questions Guide Panel (HealthMetricGuides) */}
      <div className="glass-panel rounded-[22px] p-5">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-4 pb-3 border-b border-white/10">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-cyan-500/20 border border-cyan-400/30 flex items-center justify-center text-cyan-300">
              <BookOpen className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                {tx(isAr
                  ? `الدليل الإرشادي للمؤشر: ${activeGuide.nameAr}`
                  : `Clinical Metric Guide: ${activeGuide.nameEn}`)}
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? 'القواعد الخمس لفهم وقياس وتفسير المؤشر الحيوي أثناء المهمة'
                  : 'Five essential principles for measuring and interpreting this vital sign')}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2 text-xs" dir="ltr">
            <span className="px-3 py-1 rounded-full bg-emerald-500/15 border border-emerald-400/30 text-emerald-300 font-bold">
              {tx(" Normal: ")}{tx(activeGuide.normalRange)}
            </span>
            <span className="px-3 py-1 rounded-full bg-rose-500/15 border border-rose-400/30 text-rose-300 font-bold">
              {tx(" Critical: ")}{tx(activeGuide.emergencyRange)}
            </span>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          {fiveQuestions.map((q, idx) => (
            <div
              key={idx}
              className={`p-4 rounded-xl bg-slate-900/55 border border-white/10 flex flex-col gap-2 ${
                idx === 4 ? 'md:col-span-2 lg:col-span-2 border-cyan-400/30 bg-cyan-950/20' : ''
              }`}
            >
              <h4 className="text-xs font-bold text-cyan-300 flex items-center gap-1.5">
                <HelpCircle className="w-3.5 h-3.5 shrink-0" />
                <span>{tx(isAr ? q.titleAr : q.titleEn)}</span>
              </h4>
              <p className="text-xs text-slate-200 leading-relaxed">{tx(q.body)}</p>
            </div>
          ))}
        </div>
      </div>

      {/* Complete Reference Ranges Table (ReferenceRanges) */}
      <div className="glass-panel rounded-[22px] p-5">
        <div className="flex items-center gap-2.5 mb-4">
          <div className="w-9 h-9 rounded-xl bg-blue-500/20 border border-blue-400/30 flex items-center justify-center text-blue-300">
            <SlidersHorizontal className="w-4 h-4" />
          </div>
          <div>
            <h3 className="text-base font-bold text-white">
              {tx(isAr
                ? 'جدول النطاقات المرجعية وحدود الطوارئ المعتمدة'
                : 'Demonstration Reference Ranges & Thresholds')}
            </h3>
            <p className="text-xs text-slate-300">
              {tx(isAr
                ? 'المعايير المرجعية المستخدمة لتقييم القراءات الحيوية الستة تلقائياً'
                : 'Reference standards used to evaluate all 6 vital sign metrics')}
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {guidesCatalog.map((g) => (
            <div
              key={g.code}
              className="p-3.5 rounded-xl bg-slate-900/55 border border-white/10 flex flex-col gap-2"
            >
              <div className="flex items-center justify-between">
                <span className="text-sm font-bold text-white flex items-center gap-2">
                  {g.icon}
                  {tx(isAr ? g.nameAr : g.nameEn)}
                </span>
                <span className="text-xs font-bold text-slate-400" dir="ltr">
                  {tx(g.unit)}
                </span>
              </div>
              <div className="flex items-center justify-between text-xs pt-1 border-t border-white/5">
                <span className="text-slate-300 flex items-center gap-1">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />
                  {tx(isAr ? 'النطاق الطبيعي:' : 'Normal:')}
                </span>
                <span className="font-bold text-emerald-300" dir="ltr">
                  {tx(g.normalRange)}
                </span>
              </div>
              <div className="flex items-center justify-between text-xs">
                <span className="text-slate-300 flex items-center gap-1">
                  <AlertTriangle className="w-3.5 h-3.5 text-rose-400" />
                  {tx(isAr ? 'حدود الطوارئ:' : 'Emergency:')}
                </span>
                <span className="font-bold text-rose-300" dir="ltr">
                  {tx(g.emergencyRange)}
                </span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

