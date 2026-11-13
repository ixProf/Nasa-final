import { AsyncButton } from "../AsyncButton";
import { symptomArabic, conditionArabic } from '../../i18n/clinical';
import { AssociationResults, AssociationAssessment } from '../dashboard/AssociationResults';
import { useUiText } from "../../i18n/useUiText";
import { reportError } from '../../services/errors';
import React, { useState, useEffect, useRef } from 'react';
import {
  Stethoscope,
  CheckCircle2,
  Sparkles,
  BookOpen,
  Search,
  AlertTriangle,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { api } from '../../services/api';
import { BodyMapPanel } from '../body3d/BodyMapPanel';
import { BODY_REGIONS, type BodyRegionId } from '../../data/bodyRegions';
import { REGION_SYMPTOMS } from '../../services/anatomy';
import { saveReportAssessment } from '../../services/medicalReport';

interface SymptomItem {
  id: number;
  code: string;
  name: string;
  nameAr: string;
  category: string;
  isRedFlag?: boolean;
}

interface ConditionGuideItem {
  id: number;
  code: string;
  name: string;
  nameAr: string;
  description: string;
  descriptionAr: string;
  selfCareGuidance: string;
  selfCareGuidanceAr: string;
  whenToContactMonitor: string;
  whenToContactMonitorAr: string;
}

export const SymptomGuidancePage: React.FC = () => {
  const tx = useUiText();
  const { language } = useLanguage();
  const { activeScenario, activeAccount, currentAstronautId, currentRole, selectedChatAstronautId } = useTelemetry();
  const isAr = language === 'ar';

  const [symptoms, setSymptoms] = useState<SymptomItem[]>([]);
  const [bodyRegion,setBodyRegion]=useState<BodyRegionId|null>(null);
  const [conditions, setConditions] = useState<ConditionGuideItem[]>([]);
  const [conditionSearch, setConditionSearch] = useState('');
  const [selectedCodes, setSelectedCodes] = useState<string[]>(['HEADACHE']);
  const [assessing, setAssessing] = useState(false);
  const [assessmentError, setAssessmentError] = useState('');
  const [details,setDetails]=useState<Record<string,{severity:string;durationHours:number;durationMinutes:number}>>({});
  const [assessmentResult, setAssessmentResult] = useState<AssociationAssessment | null>(null);
  const assessmentGeneration=useRef(0);
  useEffect(()=>{assessmentGeneration.current++;setAssessing(false);setAssessmentResult(null);setAssessmentError('');},[currentAstronautId,currentRole,selectedChatAstronautId,language]);

  useEffect(() => {
    api
      .getSymptoms()
      .then((res) => {
        if (Array.isArray(res) && res.length > 0) {
          setSymptoms(
            res.map((s: any, idx: number) => {

              return {
                id: s.id || idx + 1,
                code: s.code,
                name: s.name,
                nameAr: symptomArabic[s.code] || s.name,
                category: s.bodyArea || 'General',
                isRedFlag: s.isRedFlag ?? false,
              };
            })
          );
        }
      })
      .catch(reportError);

    api
      .getSpaceConditions()
      .then((res) => {
        if (Array.isArray(res) && res.length > 0) {
          setConditions(
            res.map((c: any, idx: number) => {
              const fb = conditionArabic[c.code];
              return {
                id: c.id || idx + 1,
                code: c.code,
                name: c.name,
                nameAr: fb?.nameAr || c.name,
                description: c.description,
                descriptionAr: fb?.descriptionAr || c.description,
                selfCareGuidance: c.selfCareGuidance,
                selfCareGuidanceAr: fb?.selfCareGuidanceAr || c.selfCareGuidance,
                whenToContactMonitor: c.whenToContactMonitor,
                whenToContactMonitorAr: fb?.whenToContactMonitorAr || c.whenToContactMonitor,
              };
            })
          );
        }
      })
      .catch(reportError);
  }, []);

  const toggleSymptom = (code: string) => {
    assessmentGeneration.current++;setAssessing(false);setAssessmentResult(null);
    setSelectedCodes((prev) =>
      prev.includes(code) ? prev.filter((c) => c !== code) : [...prev, code]
    );
  };

  const handleAssess = async () => {
    const target = currentRole === 'Monitor' ? selectedChatAstronautId : currentAstronautId;
    if (!target || selectedCodes.length === 0) { setAssessmentError('Select an authenticated astronaut and at least one symptom.'); return; }
    setAssessing(true); setAssessmentError(''); setAssessmentResult(null);
    const generation=++assessmentGeneration.current;
    try {
      const symptomDetails = selectedCodes.map(code => {
        const value = details[code] || { severity: 'mild', durationHours: 0, durationMinutes: 0 };
        return { code, severity: value.severity, durationHours: value.durationHours + value.durationMinutes / 60 };
      });
      const res = await api.assessSymptoms(target, selectedCodes, 'mild', 0, language, symptomDetails);
      if(generation!==assessmentGeneration.current)return;
      if(res.engineVersion!=='2.0-demo' || !Array.isArray(res.hazardScores))throw new Error(isAr?"التقييم غير متاح مؤقتًا. حاول لاحقًا.":"Assessment is temporarily unavailable. Please try again later.");
      setAssessmentResult(res);
      saveReportAssessment(activeAccount,target,{id:crypto.randomUUID(),recordedAt:new Date().toISOString(),
        symptoms:symptomDetails.map(detail=>({...detail,name:symptoms.find(s=>s.code===detail.code)?.name || detail.code,nameAr:symptomArabic[detail.code] || detail.code})),result:res});
    } catch (error) { if(generation===assessmentGeneration.current)setAssessmentError(error instanceof Error ? error.message : 'Assessment failed. Please retry.'); }
    finally { if(generation===assessmentGeneration.current)setAssessing(false); }
  };

  const filteredConditions = conditions.filter((c) => {
    if (!conditionSearch.trim()) return true;
    const q = conditionSearch.toLowerCase();
    return (
      c.name.toLowerCase().includes(q) ||
      c.nameAr.includes(q) ||
      c.description.toLowerCase().includes(q) ||
      c.descriptionAr.includes(q)
    );
  });

  return (
    <div className="flex flex-col gap-3.5 animate-fade-in">
      {/* ── Interactive 3D Anatomical Body Map ── */}
      <BodyMapPanel onRegionChange={setBodyRegion} activeRegionId={bodyRegion} selectedSymptoms={symptoms.filter(s=>selectedCodes.includes(s.code)).map(s=>({code:s.code,label:isAr?s.nameAr:s.name,severity:details[s.code]?.severity || 'mild'}))}/>

      {/* Top Card: Interactive 18-Symptom Assessment Engine (/api/symptoms & /api/symptoms/assess) */}
      <div className="glass-panel rounded-[22px] p-5">
        {/* Active Scenario Live Interconnection Banner */}
        {activeScenario.id !== 'nominal' && (
          <div className="mb-4 p-3.5 rounded-2xl bg-gradient-to-r from-amber-500/15 via-slate-900/90 to-cyan-500/10 border border-amber-400/40 text-xs text-amber-200 flex flex-col md:flex-row items-start md:items-center justify-between gap-3 shadow-md animate-fade-in">
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-xl bg-amber-500/20 border border-amber-400/40 flex items-center justify-center text-amber-300 shrink-0">
                <AlertTriangle className="w-5 h-5 animate-pulse" />
              </div>
              <div>
                <span className="font-extrabold text-white text-xs block">
                  {tx(isAr ? 'أعراض حية مقترنة بالسيناريو النشط:' : 'Active Scenario Associated Symptoms:')}{tx(' ')}
                  <span className="text-cyan-300 font-bold">{tx(isAr ? activeScenario.titleAr : activeScenario.titleEn)}</span>
                </span>
                <span className="text-slate-300 text-[11px] block mt-0.5">
                  {tx(isAr ? activeScenario.diagnosisAr : activeScenario.diagnosisEn)}
                </span>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-1.5">
              {(isAr ? activeScenario.symptomsAr : activeScenario.symptomsEn).map((sym, i) => (
                <span
                  key={i}
                  className="px-2.5 py-1 rounded-lg bg-black/50 border border-amber-400/30 text-[11px] font-semibold text-amber-200"
                >
                  • {tx(sym)}
                </span>
              ))}
            </div>
          </div>
        )}
        

        <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-cyan-500/20 border border-cyan-400/30 flex items-center justify-center text-cyan-300">
              <Stethoscope className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                {tx(isAr
                  ? 'تقييم ارتباط الأعراض'
                  : 'Symptom Association Assessment')}
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? 'اختر الأعراض الحالية لتحليل الحالة الطبية وتلقي إرشادات الرعاية الفورية'
                  : 'Select current symptoms to match spaceflight conditions and receive clinical guidance')}
              </p>
            </div>
          </div>

          <AsyncButton
            type="button"
            onClick={handleAssess}
            disabled={assessing || selectedCodes.length === 0}
            className="px-4 py-2 rounded-xl bg-gradient-to-r from-blue-600 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white font-bold text-xs shadow-lg transition-all flex items-center gap-2 disabled:opacity-50"
          >
            <Sparkles className="w-4 h-4" />
            <span>
              {tx(assessing
                ? isAr
                  ? 'جاري تحليل الأعراض...'
                  : 'Analyzing...'
                : isAr
                ? `تحليل الأعراض المختارة (${selectedCodes.length})`
                : `Analyze Selected (${selectedCodes.length})`)}
            </span>
          </AsyncButton>
        </div>

        {assessmentError && <p role="alert" className="mb-3 text-rose-300">{tx(assessmentError)}</p>}
        <div className="grid md:grid-cols-2 gap-3 mb-4">
          {selectedCodes.map(code=>{const value=details[code] || {severity:'mild',durationHours:0,durationMinutes:0};const update=(change:Partial<typeof value>)=>{assessmentGeneration.current++;setAssessmentResult(null);setAssessing(false);setDetails(old=>({...old,[code]:{...value,...change}}));};
          return <div key={code} className="rounded-xl border border-cyan-400/20 p-3 space-y-2"><strong>{isAr?symptomArabic[code] || code:symptoms.find(s=>s.code===code)?.name || code}</strong><div className="flex gap-3 flex-wrap">
            <label>{isAr?'شدة هذا العرض':'This symptom’s intensity'}<select aria-label={(isAr?'شدة ':'Intensity ')+code} className="bg-slate-900 p-2 ms-2" value={value.severity} onChange={e=>update({severity:e.target.value})}><option value="mild">{isAr?'خفيفة':'Mild'}</option><option value="moderate">{isAr?'متوسطة':'Moderate'}</option><option value="severe">{isAr?'شديدة':'Severe'}</option></select></label>
            <label>{isAr?'المدة بالساعات':'Duration in hours'}<input aria-label={(isAr?'المدة بالساعات ':'Duration in hours ')+code} className="bg-slate-900 p-2 w-24 ms-2" type="number" min="0" max="8760" step="1" value={value.durationHours} onChange={e=>{const hours=Math.min(8760, Math.max(0, Math.trunc(Number(e.target.value) || 0))); update({durationHours:hours, ...(hours === 8760 ? {durationMinutes:0} : {})});}}/></label>
            <label>{isAr?'الدقائق':'Minutes'}<input aria-label={(isAr?'الدقائق ':'Minutes ')+code} className="bg-slate-900 p-2 w-24 ms-2" type="number" min="0" max={value.durationHours === 8760 ? 0 : 59} step="1" value={value.durationMinutes} onChange={e=>update({durationMinutes:Math.min(value.durationHours === 8760 ? 0 : 59, Math.max(0, Math.trunc(Number(e.target.value) || 0)))})}/></label>
          </div></div>;})}
        </div>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-2">
          {bodyRegion&&<div className="col-span-full flex flex-wrap items-center justify-between gap-2 rounded-xl border border-cyan-400/20 bg-cyan-500/5 p-3 text-xs"><span>{isAr?'الأعراض المرتبطة بـ ':'Symptoms related to '}{isAr?BODY_REGIONS.find(r=>r.id===bodyRegion)?.nameAr:BODY_REGIONS.find(r=>r.id===bodyRegion)?.nameEn}</span><button type="button" onClick={()=>setBodyRegion(null)} className="rounded-lg px-3 py-2 text-cyan-200 hover:bg-white/5">{isAr?'عرض جميع الأعراض':'Show all symptoms'}</button><p className="w-full text-slate-400">{isAr?'اختيار المنطقة يرشّح القائمة فقط؛ اختر الأعراض التي تشعر بها. الأعراض المحدّدة سابقًا تبقى ضمن التقييم.':'Selecting a region filters the list. Choose the symptoms you are experiencing; existing selections stay in your assessment.'}</p></div>}
          {(bodyRegion?symptoms.filter(s=>REGION_SYMPTOMS[bodyRegion].includes(s.code) || s.isRedFlag):symptoms).map((sym) => {
            const active = selectedCodes.includes(sym.code);
            return (
              <AsyncButton
                key={sym.code}
                type="button"
                onClick={() => toggleSymptom(sym.code)}
                className={`p-2.5 rounded-xl text-start border transition-all flex items-center justify-between gap-1.5 ${
                  active
                    ? sym.isRedFlag
                      ? 'bg-rose-500/25 border-rose-400 text-white'
                      : 'bg-cyan-500/20 border-cyan-400/60 text-white'
                    : sym.isRedFlag
                    ? 'bg-rose-950/30 border-rose-500/30 text-rose-200 hover:border-rose-400/50'
                    : 'bg-slate-900/50 border-white/10 text-slate-300 hover:border-white/25'
                }`}
              >
                <div className="min-w-0">
                  <span className="text-xs font-bold block truncate">
                    {tx(isAr ? sym.nameAr : sym.name)}
                  </span>
                  {sym.isRedFlag && (
                    <span className="text-[10px] text-rose-300 font-semibold flex items-center gap-1 mt-0.5">
                      <AlertTriangle className="w-3 h-3" />
                      {tx(isAr ? 'عرض حرج' : 'Red Flag')}
                    </span>
                  )}
                </div>
                <span
                  className={`w-4 h-4 rounded-full border shrink-0 flex items-center justify-center ${
                    active ? 'border-cyan-400 bg-cyan-400 text-slate-950' : 'border-slate-500'
                  }`}
                >
                  {active && <CheckCircle2 className="w-3 h-3" />}
                </span>
              </AsyncButton>
            );
          })}
        </div>

        {assessmentResult && <AssociationResults result={assessmentResult}/> }
      </div>

      {/* Bottom Card: All 12 Spaceflight Medical Conditions Catalog (/api/symptoms/guidance) */}
      <div className="glass-panel rounded-[22px] p-5">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-blue-500/20 border border-blue-400/30 flex items-center justify-center text-blue-300">
              <BookOpen className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                {tx(isAr
                  ? 'الدليل المرجعي للحالات الطبية الفضائية (12 متلازمة وحالة)'
                  : 'Spaceflight Medical Conditions Reference (12 Syndromes)')}
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? 'بروتوكولات الرعاية الذاتية المعتمدة للتعامل مع تأثيرات الفضاء'
                  : 'Clinical self-care protocols and escalation thresholds for spaceflight conditions')}
              </p>
            </div>
          </div>

          <div className="relative w-full sm:w-64">
            <Search className="w-4 h-4 text-slate-400 absolute top-2.5 start-3" />
            <input
              type="text"
              value={conditionSearch}
              onChange={(e) => setConditionSearch(e.target.value)}
              placeholder={tx(isAr ? 'بحث في الدليل الطبي...' : 'Search medical guide...')}
              className="w-full rounded-xl bg-slate-900/75 border border-white/15 ps-9 pe-3 py-2 text-xs text-white placeholder-slate-400 focus:outline-none focus:border-cyan-400"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 max-h-[340px] overflow-y-auto pr-1">
          {filteredConditions.map((cond) => (
            <div
              key={cond.code}
              className="p-3.5 rounded-xl bg-slate-900/55 border border-white/10 flex flex-col justify-between gap-2"
            >
              <div>
                <h4 className="text-xs font-bold text-white mb-1">
                  {tx(isAr ? cond.nameAr : cond.name)}
                </h4>
                <p className="text-[11px] text-slate-300 leading-relaxed mb-2">
                  {tx(isAr ? cond.descriptionAr : cond.description)}
                </p>
              </div>
              <div className="pt-2 border-t border-white/10 space-y-1 text-[11px]">
                <p className="text-cyan-200 leading-relaxed">
                  <strong className="text-cyan-400">
                    {tx(isAr ? 'الرعاية الذاتية: ' : 'Self-Care: ')}
                  </strong>
                  {tx(isAr ? cond.selfCareGuidanceAr : cond.selfCareGuidance)}
                </p>
                <p className="text-amber-200/90 leading-relaxed">
                  <strong className="text-amber-400">
                    {tx(isAr ? 'استشارة الطبيب: ' : 'Contact Doctor: ')}
                  </strong>
                  {tx(isAr ? cond.whenToContactMonitorAr : cond.whenToContactMonitor)}
                </p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

