import { AssignmentReminderPanel } from '../dashboard/AssignmentReminderPanel';
import { intervalInMinutes } from '../../services/reminderInbox';
import { AsyncButton } from "../AsyncButton";
import { HabitTracker } from '../dashboard/HabitTracker';
import { useUiText } from "../../i18n/useUiText";
import { AsyncForm } from "../AsyncForm";
import React, { useState } from 'react';
import {
  ClipboardCheck,
  Plus,
  Check,
  Sparkles,
  XCircle,
  Clock,
  Calendar,
  Bell,
  Trash2,
  Stethoscope,
  Dumbbell,
  Moon,
  Heart,
  Wind,
  AlertTriangle,
  Play,
  RotateCcw,
  CheckCircle2,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import {
  RecommendationStatus,
  RecommendationCategory,
  RecommendationPriority,
  ScheduleType,
} from '../../types/telemetry';

export const RecommendationsPage: React.FC = () => {
  const tx = useUiText();
  const { language } = useLanguage();
  const {
    recommendations,
    updateRecommendationStatus,
    createRecommendation,
    deleteRecommendation,
    triggerRecommendationAlert,
    crewMembers,
    currentRole,
    currentUserName,
    activeScenario,
    currentAstronautId,
  } = useTelemetry();
  const isAr = language === 'ar';

  const [statusFilter, setStatusFilter] = useState<'ALL' | RecommendationStatus | 'DailyRecurring'>('ALL');
  const [showAddRec, setShowAddRec] = useState(false);

  // Doctor Form Fields
  const [targetAstroId, setTargetAstroId] = useState<number>(0); // 0 = All crew
  const [recTitle, setRecTitle] = useState('');
  const [recTitleAr,setRecTitleAr] = useState('');
  const [recContentAr,setRecContentAr] = useState('');
  const [recReasonAr,setRecReasonAr] = useState('');
  const [habitPoints,setHabitPoints] = useState(10);
  const [reminderEnabled,setReminderEnabled]=useState(true);
  const [reminderAmount,setReminderAmount]=useState(1);
  const [reminderUnit,setReminderUnit]=useState<'minutes'|'hours'>('hours');
  const [reminderSound,setReminderSound]=useState(true);
  const [recContent, setRecContent] = useState('');
  const [recReason, setRecReason] = useState('');
  const [recCategory, setRecCategory] = useState<RecommendationCategory>('Medical');
  const [recPriority, setRecPriority] = useState<RecommendationPriority>('High');
  const [recScheduleType, setRecScheduleType] = useState<ScheduleType>('DailyRecurring');
  const [recScheduledTime, setRecScheduledTime] = useState('08:00');
  const [recDoctorName, setRecDoctorName] = useState(currentUserName);
  const [simulatedAlertSuccessId, setSimulatedAlertSuccessId] = useState<number | null>(null);

  const handleCreateRec = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!recTitle.trim() || !recTitleAr.trim() || targetAstroId <= 0) throw new Error(isAr ? 'اختر رائد فضاء وأدخل العنوان باللغتين.' : 'Select an astronaut and enter both titles.');

    const selectedCrew = crewMembers.find((c) => c.id === targetAstroId);
    const targetName = targetAstroId === 0
      ? (isAr ? 'كافة أفراد الطاقم' : 'All Crew Members')
      : selectedCrew?.fullName || 'Morgan Vance';

    await createRecommendation({
      astronautId: targetAstroId,
      astronautFullName: targetName,
      title: recTitle.trim(),
      titleAr: recTitleAr.trim(),
      content: recContent.trim() || recTitle.trim(),
      contentAr: recContentAr.trim() || recTitleAr.trim(),
      reason: recReason.trim() || (isAr ? 'بروتوكول وقائي للحفاظ على التوازن الفسيولوجي المداري' : 'Preventive countermeasure protocol'),
      reasonAr: recReasonAr.trim() || 'مهمة يومية وفق تعليمات طبيب الرحلة',
      category: recCategory,
      priority: recPriority,
      isDailyHabit: recScheduleType === 'DailyRecurring',
      habitPoints,
      reminderEnabled,
      reminderIntervalMinutes: intervalInMinutes(reminderAmount,reminderUnit),
      reminderSoundEnabled: reminderSound,
      scheduleType: recScheduleType,
      scheduledTime: recScheduleType === 'DailyRecurring' ? recScheduledTime : undefined,
      prescribedBy: recDoctorName.trim() || (isAr ? 'د. سارة أوكونور (طبيب الرحلة)' : 'Dr. Sarah O\'Connor (Flight Surgeon)'),
    });

    setRecTitle(''); setRecTitleAr(''); setRecContentAr(''); setRecReasonAr('');
    setRecContent('');
    setRecReason('');
    setShowAddRec(false);
  };

  const handleSimulateAlert = (recId: number) => {
    triggerRecommendationAlert(recId);
    setSimulatedAlertSuccessId(recId);
    setTimeout(() => {
      setSimulatedAlertSuccessId(null);
    }, 4000);
  };

  // Color mapping by Priority Level
  const getPriorityTheme = (p: RecommendationPriority) => {
    switch (p) {
      case 'Urgent':
        return {
          label: isAr ? 'حرجة / طارئة' : 'Urgent',
          border: 'border-rose-500/50',
          glow: 'shadow-[0_0_20px_rgba(244,63,94,0.3)]',
          badge: 'bg-rose-500/20 text-rose-300 border-rose-500/40',
          indicator: 'bg-rose-500',
        };
      case 'High':
        return {
          label: isAr ? 'أولوية عالية' : 'High Priority',
          border: 'border-amber-400/40',
          glow: 'shadow-[0_0_18px_rgba(245,158,11,0.2)]',
          badge: 'bg-amber-500/20 text-amber-300 border-amber-400/40',
          indicator: 'bg-amber-400',
        };
      case 'Medium':
        return {
          label: isAr ? 'أولوية متوسطة' : 'Medium Priority',
          border: 'border-cyan-400/40',
          glow: 'shadow-[0_0_15px_rgba(34,211,238,0.15)]',
          badge: 'bg-cyan-500/20 text-cyan-300 border-cyan-400/30',
          indicator: 'bg-cyan-400',
        };
      case 'Low':
      default:
        return {
          label: isAr ? 'أولوية اعتيادية' : 'Routine',
          border: 'border-emerald-400/30',
          glow: 'shadow-[0_0_12px_rgba(16,185,129,0.12)]',
          badge: 'bg-emerald-500/15 text-emerald-300 border-emerald-400/30',
          indicator: 'bg-emerald-400',
        };
    }
  };

  const getCategoryIcon = (c: RecommendationCategory) => {
    switch (c) {
      case 'Physical':
        return <Dumbbell className="w-4 h-4 text-cyan-400" />;
      case 'Sleep':
        return <Moon className="w-4 h-4 text-blue-300" />;
      case 'Medical':
        return <Stethoscope className="w-4 h-4 text-emerald-400" />;
      case 'Psychological':
        return <Heart className="w-4 h-4 text-rose-300" />;
      case 'Environmental':
        return <Wind className="w-4 h-4 text-amber-300" />;
    }
  };

  const getStatusBadge = (st: RecommendationStatus) => {
    switch (st) {
      case 'Completed':
        return {
          label: isAr ? 'مكتملة بنجاح' : 'Completed',
          cls: 'bg-emerald-500/20 text-emerald-300 border-emerald-400/40',
        };
      case 'Acknowledged':
        return {
          label: isAr ? 'قيد التنفيذ' : 'In Progress',
          cls: 'bg-cyan-500/20 text-cyan-300 border-cyan-400/40',
        };
      case 'Dismissed':
        return {
          label: isAr ? 'مؤجلة' : 'Dismissed',
          cls: 'bg-slate-700/40 text-slate-300 border-slate-600/40',
        };
      case 'Pending':
      default:
        return {
          label: isAr ? 'في الانتظار' : 'Pending',
          cls: 'bg-amber-500/20 text-amber-300 border-amber-400/40',
        };
    }
  };

  // Filter recommendations: Astronaut sees only their own or all-crew recs
  const userFilteredRecs = recommendations.filter((r) => {
    if (currentRole === 'Astronaut') {
      return r.astronautId === 0 || r.astronautId === (currentAstronautId || 1);
    }
    return true;
  });

  const displayRecs = userFilteredRecs.filter((r) => {
    if (statusFilter === 'ALL') return true;
    if (statusFilter === 'DailyRecurring') return (r.isDailyHabit || r.scheduleType === 'DailyRecurring');
    return r.status === statusFilter;
  });

  // KPI calculations
  const totalCount = userFilteredRecs.length;
  const recurringCount = userFilteredRecs.filter((r) => r.scheduleType === 'DailyRecurring').length;
  const activeCount = userFilteredRecs.filter((r) => r.status === 'Pending' || r.status === 'Acknowledged').length;
  const completedCount = userFilteredRecs.filter((r) => r.status === 'Completed').length;

  return (
    <div className="glass-panel rounded-[24px] p-5 md:p-6 flex flex-col gap-5 animate-fade-in border border-cyan-400/20">
      {currentRole === 'Monitor' && <label className="text-sm text-slate-300">
        {isAr ? 'متابعة عادات رائد الفضاء' : 'View astronaut habits'}
        <select value={targetAstroId} onChange={e => setTargetAstroId(Number(e.target.value))} className="ms-3 bg-slate-900 rounded-lg p-2">
          <option value={0} disabled>{isAr ? 'اختر رائد فضاء' : 'Select an astronaut'}</option>
          {crewMembers.map(c => <option key={c.id} value={c.id}>{tx(isAr ? c.fullNameAr : c.fullName)}</option>)}
        </select>
      </label>}
      {(currentRole === 'Astronaut' || targetAstroId > 0) && <HabitTracker
        astronautId={currentRole === 'Monitor' ? targetAstroId : undefined}
        readOnly={currentRole === 'Monitor'}
        refreshKey={recommendations.map(r => r.id + ':' + r.status).join(',')}
      />}
      {(currentRole==='Astronaut'||targetAstroId>0)&&<AssignmentReminderPanel astronautId={currentRole==='Monitor'?targetAstroId:undefined} monitor={currentRole==='Monitor'} refreshKey={recommendations.map(r=>r.id+':'+r.status).join(',')}/>}
      {/* ── Page Header ── */}
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4 pb-4 border-b border-white/10">
        <div className="flex items-center gap-3">
          <div className="w-11 h-11 rounded-2xl bg-cyan-500/20 border border-cyan-400/40 flex items-center justify-center text-cyan-300 shadow-[0_0_15px_rgba(34,211,238,0.25)]">
            <ClipboardCheck className="w-6 h-6 stroke-[2.2]" />
          </div>
          <div>
            <div className="flex items-center gap-2 flex-wrap">
              <h2 className="text-lg md:text-xl font-extrabold text-white">
                {tx(isAr ? 'نظام التوصيات والبروتوكولات الطبية المدارية' : 'Orbital Medical Countermeasures & Directives')}
              </h2>
              <span className="text-[10px] font-mono px-2 py-0.5 rounded-full bg-cyan-950/90 text-cyan-300 border border-cyan-400/30">
                {tx(currentRole === 'Monitor' ? (isAr ? 'لوحة طبيب الرحلة' : 'Flight Surgeon Mode') : (isAr ? 'لوحة الرائد' : 'Crew Mode'))}
              </span>
            </div>
            <p className="text-xs text-slate-300 mt-0.5">
              {tx(isAr
                ? 'إدارة التوصيات الطبية اليومية المتكررة والمفردة مع جدولة إشعارات آلية فور حلول موعد الجرعة أو التمرين'
                : 'Direct medical prescriptions with daily recurring automated reminders & countermeasure tracking')}
            </p>
          </div>
        </div>

        {/* Doctor Action Button */}
        {currentRole === 'Monitor' && (
          <AsyncButton
            type="button"
            onClick={() => setShowAddRec(!showAddRec)}
            className="px-4 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white text-xs font-bold flex items-center gap-2 shadow-[0_0_20px_rgba(34,211,238,0.3)] transition-all cursor-pointer shrink-0"
          >
            <Plus className="w-4 h-4 stroke-[2.5]" />
            <span>{tx(isAr ? (showAddRec ? 'إغلاق نافذة التوصية' : 'إصدار توصية طبية جديدة') : (showAddRec ? 'Close Prescription' : 'New Directive'))}</span>
          </AsyncButton>
        )}
      </div>

      {/* ── KPI Summary Cards ── */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        <div className="p-3.5 rounded-2xl bg-slate-900/60 border border-white/10 flex items-center justify-between">
          <div>
            <p className="text-xs text-slate-400 font-semibold">{tx(isAr ? 'إجمالي التوصيات' : 'Total Directives')}</p>
            <p className="text-xl font-black text-white mt-0.5">{tx(totalCount)}</p>
          </div>
          <div className="w-9 h-9 rounded-xl bg-cyan-500/15 border border-cyan-400/30 flex items-center justify-center text-cyan-300">
            <ClipboardCheck className="w-4 h-4" />
          </div>
        </div>

        <div className="p-3.5 rounded-2xl bg-slate-900/60 border border-white/10 flex items-center justify-between">
          <div>
            <p className="text-xs text-slate-400 font-semibold">{tx(isAr ? 'تكرار يومي مجدول' : 'Daily Recurring')}</p>
            <p className="text-xl font-black text-amber-300 mt-0.5">{tx(recurringCount)}</p>
          </div>
          <div className="w-9 h-9 rounded-xl bg-amber-500/15 border border-amber-400/30 flex items-center justify-center text-amber-300">
            <Clock className="w-4 h-4" />
          </div>
        </div>

        <div className="p-3.5 rounded-2xl bg-slate-900/60 border border-white/10 flex items-center justify-between">
          <div>
            <p className="text-xs text-slate-400 font-semibold">{tx(isAr ? 'قيد المتابعة والتنفيذ' : 'Active / In Progress')}</p>
            <p className="text-xl font-black text-cyan-300 mt-0.5">{tx(activeCount)}</p>
          </div>
          <div className="w-9 h-9 rounded-xl bg-cyan-500/15 border border-cyan-400/30 flex items-center justify-center text-cyan-300">
            <RotateCcw className="w-4 h-4" />
          </div>
        </div>

        <div className="p-3.5 rounded-2xl bg-slate-900/60 border border-white/10 flex items-center justify-between">
          <div>
            <p className="text-xs text-slate-400 font-semibold">{tx(isAr ? 'مكتملة بنجاح' : 'Completed')}</p>
            <p className="text-xl font-black text-emerald-400 mt-0.5">{tx(completedCount)}</p>
          </div>
          <div className="w-9 h-9 rounded-xl bg-emerald-500/15 border border-emerald-400/30 flex items-center justify-center text-emerald-300">
            <CheckCircle2 className="w-4 h-4" />
          </div>
        </div>
      </div>

      {/* ── Doctor Prescription Desk (Monitor Only) ── */}
      {currentRole === 'Monitor' && showAddRec && (
        <AsyncForm
          onSubmit={handleCreateRec}
          className="p-5 rounded-3xl bg-slate-900/90 border-2 border-cyan-400/40 shadow-2xl flex flex-col gap-4 animate-fade-in relative overflow-hidden"
        >
          <div className="flex items-center justify-between pb-3 border-b border-white/10">
            <div className="flex items-center gap-2">
              <Stethoscope className="w-5 h-5 text-cyan-400" />
              <h3 className="text-sm font-extrabold text-white">
                {tx(isAr ? 'محرر الوصفات والتوجيهات السريرية (Flight Surgeon Prescription Desk)' : 'Clinical Prescription Authoring Desk')}
              </h3>
            </div>
            <span className="text-[11px] text-cyan-300 font-mono">
              {tx(isAr ? 'متصل بقاعدة البيانات وتنبيهات الطاقم' : 'Connected to Crew Telemetry')}
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            {/* Target Astronaut */}
            <div>
              <label className="text-[11px] font-bold text-slate-300 block mb-1">
                {tx(isAr ? 'الرائد المستهدف:' : 'Target Astronaut:')}
              </label>
              <select
                value={targetAstroId}
                onChange={(e) => setTargetAstroId(Number(e.target.value))}
                className="w-full rounded-xl bg-slate-950/90 border border-white/15 px-3 py-2 text-xs font-semibold text-white focus:border-cyan-400 focus:outline-none"
              >
                <option value={0} disabled>{isAr ? 'اختر رائد فضاء' : 'Select an astronaut'}</option>
                {crewMembers.map((c) => (
                  <option key={c.id} value={c.id}>
                    👤 {tx(isAr ? c.fullNameAr : c.fullName)} ({tx(c.astronautCode)})
                  </option>
                ))}
              </select>
            </div>

            {/* Category */}
            <div>
              <label className="text-[11px] font-bold text-slate-300 block mb-1">
                {tx(isAr ? 'التصنيف الطبي:' : 'Category:')}
              </label>
              <select
                value={recCategory}
                onChange={(e) => setRecCategory(e.target.value as RecommendationCategory)}
                className="w-full rounded-xl bg-slate-950/90 border border-white/15 px-3 py-2 text-xs font-semibold text-white focus:border-cyan-400 focus:outline-none"
              >
                <option value="Medical">{tx(isAr ? '🩺 طبي ودوائي (Medical)' : '🩺 Medical')}</option>
                <option value="Physical">{tx(isAr ? '🏋️ تمارين ومقاومة ضمور (Physical)' : '🏋️ Physical / Countermeasure')}</option>
                <option value="Sleep">{tx(isAr ? '🌙 نوم ودورة سيركادية (Sleep)' : '🌙 Sleep & Circadian')}</option>
                <option value="Psychological">{tx(isAr ? '❤️ صحة نفسية ومعنوية (Psychological)' : '❤️ Psychological')}</option>
                <option value="Environmental">{tx(isAr ? '💨 بيئي ومقصورة (Environmental)' : '💨 Environmental')}</option>
              </select>
            </div>

            {/* Priority Level */}
            <div>
              <label className="text-[11px] font-bold text-slate-300 block mb-1">
                {tx(isAr ? 'مستوى الأولوية والتوهج:' : 'Priority Level:')}
              </label>
              <select
                value={recPriority}
                onChange={(e) => setRecPriority(e.target.value as RecommendationPriority)}
                className="w-full rounded-xl bg-slate-950/90 border border-white/15 px-3 py-2 text-xs font-semibold text-white focus:border-cyan-400 focus:outline-none"
              >
                <option value="Low">🟢 {tx(isAr ? 'عادية (Low) - هادئة' : 'Low')}</option>
                <option value="Medium">🔵 {tx(isAr ? 'متوسطة (Medium) - سماوية' : 'Medium')}</option>
                <option value="High">🟠 {tx(isAr ? 'عالية (High) - كهرمانية ملفتة' : 'High')}</option>
                <option value="Urgent">🔴 {tx(isAr ? 'حرجة / طارئة (Urgent) - توهج أحمر نابض' : 'Urgent')}</option>
              </select>
            </div>
          </div>

          {/* Schedule Frequency Section */}
          <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-white/10 flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3">
            <div className="flex items-center gap-3">
              <Clock className="w-5 h-5 text-amber-400 shrink-0" />
              <div>
                <span className="text-xs font-bold text-white block">
                  {tx(isAr ? 'نوع التكرار والجدولة الزمنية:' : 'Schedule Frequency:')}
                </span>
                <span className="text-[11px] text-slate-400">
                  {tx(isAr ? 'حدد ما إذا كانت التوصية لمرة واحدة أم تتكرر يومياً في مواعيد محددة' : 'Specify one-time vs daily recurring alert')}
                </span>
              </div>
            </div>

            <div className="flex items-center gap-2 flex-wrap">
              <AsyncButton
                type="button"
                onClick={() => setRecScheduleType('Once')}
                className={`px-3 py-1.5 rounded-xl text-xs font-bold border transition-all cursor-pointer ${
                  recScheduleType === 'Once'
                    ? 'bg-blue-600 text-white border-blue-400 shadow-sm'
                    : 'bg-slate-900 border-white/10 text-slate-300 hover:text-white'
                }`}
              >
                {tx(isAr ? 'مرة واحدة (Once)' : 'Once')}
              </AsyncButton>
              <AsyncButton
                type="button"
                onClick={() => setRecScheduleType('DailyRecurring')}
                className={`px-3 py-1.5 rounded-xl text-xs font-bold border transition-all cursor-pointer ${
                  recScheduleType === 'DailyRecurring'
                    ? 'bg-amber-600 text-white border-amber-400 shadow-[0_0_15px_rgba(245,158,11,0.3)]'
                    : 'bg-slate-900 border-white/10 text-slate-300 hover:text-white'
                }`}
              >
                ⏰ {tx(isAr ? 'تكرار يومي في موعد محدد (Daily Recurring)' : 'Daily Recurring')}
              </AsyncButton>
            </div>

            {recScheduleType === 'DailyRecurring' && (
              <div className="flex items-center gap-2">
                <input
                  type="time"
                  value={recScheduledTime}
                  onChange={(e) => setRecScheduledTime(e.target.value)}
                  className="rounded-xl bg-slate-900 border border-amber-400/40 text-amber-300 font-mono font-bold px-3 py-1.5 text-xs focus:outline-none"
                />
                <div className="hidden lg:flex items-center gap-1 text-[10px]">
                  {['07:30', '13:00', '18:30', '21:30'].map((time) => (
                    <AsyncButton
                      key={time}
                      type="button"
                      onClick={() => setRecScheduledTime(time)}
                      className="px-1.5 py-1 rounded bg-slate-800 text-slate-300 hover:text-white border border-white/10 font-mono"
                    >
                      {tx(time)}
                    </AsyncButton>
                  ))}
                </div>
              </div>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <label className="text-xs text-slate-300">{isAr?'العنوان بالعربية':'Arabic title'}<input required dir="rtl" maxLength={200} value={recTitleAr} onChange={e=>setRecTitleAr(e.target.value)} className="mt-1 w-full rounded-xl bg-slate-950/90 border border-white/15 p-2 text-white"/></label>
            <label className="text-xs text-slate-300">{isAr?'التعليمات بالعربية':'Arabic instructions'}<textarea required dir="rtl" maxLength={2000} value={recContentAr} onChange={e=>setRecContentAr(e.target.value)} className="mt-1 w-full rounded-xl bg-slate-950/90 border border-white/15 p-2 text-white"/></label>
            <label className="text-xs text-slate-300">{isAr?'السبب بالعربية':'Arabic reason'}<textarea required dir="rtl" maxLength={1000} value={recReasonAr} onChange={e=>setRecReasonAr(e.target.value)} className="mt-1 w-full rounded-xl bg-slate-950/90 border border-white/15 p-2 text-white"/></label>
            {recScheduleType==='DailyRecurring' && <label className="text-xs text-slate-300">{isAr?'نقاط الإنجاز اليومي':'Points per daily completion'}<input type="number" min={1} max={100} required value={habitPoints} onChange={e=>setHabitPoints(Number(e.target.value))} className="mx-2 w-20 rounded-lg bg-slate-900 border border-white/15 p-2"/><span>{isAr?'تُضاف هذه المهمة إلى متتبع العادات.':'This assignment appears in the habit tracker.'}</span></label>}
          </div>
          <div className="rounded-xl border border-cyan-400/30 bg-cyan-500/5 p-3 flex flex-wrap gap-3 items-center text-xs">
            <label className="flex gap-2 items-center"><input type="checkbox" checked={reminderEnabled} onChange={e=>setReminderEnabled(e.target.checked)}/>{isAr?'تفعيل التذكير التلقائي عند إسناد المهمة':'Enable automatic reminders when assigned'}</label>
            <label>{isAr?'تذكير كل ':'Remind every '}<input type="number" min={1} max={reminderUnit==='hours'?168:10080} step={1} required value={reminderAmount} onChange={e=>setReminderAmount(Number(e.target.value))} className="w-20 bg-slate-950 border border-white/15 p-2 rounded-lg"/></label>
            <select value={reminderUnit} onChange={e=>setReminderUnit(e.target.value as 'minutes'|'hours')} aria-label={isAr?'وحدة التكرار':'Repeat unit'} className="bg-slate-950 rounded-lg border border-white/15 p-2"><option value="minutes">{isAr?'دقيقة':'minutes'}</option><option value="hours">{isAr?'ساعة':'hours'}</option></select>
            <label className="flex gap-2 items-center"><input type="checkbox" checked={reminderSound} onChange={e=>setReminderSound(e.target.checked)}/>{isAr?'صوت مع التذكير':'Reminder sound'}</label>
            <p className="w-full text-slate-400">{isAr?'عند التفعيل، يُرسل تذكير فور إسناد المهمة ثم يتكرر بالفاصل المحدد.':'When enabled, a reminder is sent when assigned, then repeats at this interval.'}</p>
          </div>
          {/* Title & Action */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="text-[11px] font-bold text-slate-300 block mb-1">
                {tx(isAr ? 'عنوان التوصية الطبية:' : 'Directive Title:')}
              </label>
              <input
                type="text"
                required
                value={recTitle}
                onChange={(e) => setRecTitle(e.target.value)}
                placeholder={tx(isAr ? 'مثال: جلسة مقاومة عضلية ARED لمدة 45 دقيقة' : 'e.g., ARED Countermeasure Protocol')}
                className="w-full rounded-xl bg-slate-950/90 border border-white/15 px-3 py-2 text-xs text-white focus:border-cyan-400 focus:outline-none"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-slate-300 block mb-1">
                {tx(isAr ? 'اسم الطبيب / الأخصائي المشرف:' : 'Prescribing Medical Officer:')}
              </label>
              <input
                type="text"
                value={recDoctorName}
                onChange={(e) => setRecDoctorName(e.target.value)}
                placeholder={tx(isAr ? 'د. سارة أوكونور (طبيب الرحلة الرئيسي)' : 'Dr. Sarah O\'Connor')}
                className="w-full rounded-xl bg-slate-950/90 border border-white/15 px-3 py-2 text-xs text-white focus:border-cyan-400 focus:outline-none"
              />
            </div>
          </div>

          {/* Details & Rationale */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="text-[11px] font-bold text-slate-300 block mb-1">
                {tx(isAr ? 'تفاصيل الإجراء المطلوب من الرائد بدقة:' : 'Detailed Action Protocol:')}
              </label>
              <textarea
                rows={2}
                value={recContent}
                onChange={(e) => setRecContent(e.target.value)}
                placeholder={tx(isAr ? 'اشرح بالتفصيل الخطوات، الجرعة، أو نمط التمرين...' : 'Detailed action instructions...')}
                className="w-full rounded-xl bg-slate-950/90 border border-white/15 px-3 py-2 text-xs text-white focus:border-cyan-400 focus:outline-none resize-none"
              />
            </div>

            <div>
              <label className="text-[11px] font-bold text-slate-300 block mb-1">
                {tx(isAr ? 'المبرر الفسيولوجي والسند الطبي (Physiological Rationale):' : 'Physiological Rationale:')}
              </label>
              <textarea
                rows={2}
                value={recReason}
                onChange={(e) => setRecReason(e.target.value)}
                placeholder={tx(isAr ? 'وضح للرائد سبب هذا التوجيه (مثلاً: للحد من انزياح السوائل أو فقدان الكالسيوم)...' : 'Medical physiological justification...')}
                className="w-full rounded-xl bg-slate-950/90 border border-white/15 px-3 py-2 text-xs text-white focus:border-cyan-400 focus:outline-none resize-none"
              />
            </div>
          </div>

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-white/10">
            <AsyncButton
              type="button"
              onClick={() => setShowAddRec(false)}
              className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-bold transition-all cursor-pointer"
            >
              {tx(isAr ? 'إلغاء' : 'Cancel')}
            </AsyncButton>
            <AsyncButton
              type="submit"
              className="px-5 py-2 rounded-xl bg-gradient-to-r from-emerald-600 to-teal-500 hover:from-emerald-500 hover:to-teal-400 text-white text-xs font-extrabold shadow-[0_0_15px_rgba(16,185,129,0.35)] transition-all cursor-pointer flex items-center gap-1.5"
            >
              <Check className="w-4 h-4 stroke-[3]" />
              <span>{tx(isAr ? 'اعتماد ونشر التوصية للرائد فوراً' : 'Issue Directive')}</span>
            </AsyncButton>
          </div>
        </AsyncForm>
      )}

      {/* ── Active Clinical Scenario Banner ── */}
      {activeScenario.id !== 'nominal' && (
        <div className="p-4 rounded-2xl bg-gradient-to-r from-cyan-950/80 via-slate-900/90 to-blue-950/80 border border-cyan-400/40 flex flex-col md:flex-row items-start md:items-center justify-between gap-3 shadow-[0_0_20px_rgba(34,211,238,0.15)] animate-fade-in">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-cyan-500/20 border border-cyan-400/40 flex items-center justify-center text-cyan-300 shrink-0">
              <Sparkles className="w-5 h-5 text-cyan-400 animate-pulse" />
            </div>
            <div>
              <span className="text-xs font-bold text-cyan-300 block">
                {tx(isAr ? 'بروتوكول استجابة فوري متزامن مع السيناريو السريري النشط:' : 'Synchronized Protocol for Active Scenario:')}{tx(' ')}
                <span className="text-white font-extrabold">{tx(isAr ? activeScenario.titleAr : activeScenario.titleEn)}</span>
              </span>
              <p className="text-xs text-slate-200 mt-0.5">
                {tx(isAr ? activeScenario.flightSurgeonDirectiveAr : activeScenario.flightSurgeonDirectiveEn)}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-[11px] font-bold px-3 py-1 rounded-xl bg-cyan-500/20 text-cyan-200 border border-cyan-400/30">
              {tx(isAr ? 'مُتصل بالتيليمتري 100%' : '100% Telemetry Linked')}
            </span>
          </div>
        </div>
      )}

      {/* ── Filter Tabs Bar ── */}
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <div className="flex items-center gap-1.5 bg-slate-900/80 p-1 rounded-xl border border-white/10 overflow-x-auto no-scrollbar">
          {(
            [
              { key: 'ALL', ar: 'كافة التوصيات', en: 'All' },
              { key: 'DailyRecurring', ar: '⏰ تكرار يومي', en: 'Daily' },
              { key: 'Pending', ar: 'في الانتظار', en: 'Pending' },
              { key: 'Acknowledged', ar: 'قيد التنفيذ', en: 'Active' },
              { key: 'Completed', ar: 'مكتملة', en: 'Completed' },
            ] as const
          ).map((tab) => (
            <AsyncButton
              key={tab.key}
              type="button"
              onClick={() => setStatusFilter(tab.key)}
              className={`px-3 py-1.5 rounded-lg text-xs font-bold transition-all cursor-pointer whitespace-nowrap ${
                statusFilter === tab.key
                  ? 'bg-cyan-600 text-white shadow'
                  : 'text-slate-300 hover:text-white hover:bg-white/5'
              }`}
            >
              {tx(isAr ? tab.ar : tab.en)}
            </AsyncButton>
          ))}
        </div>

        <span className="text-xs text-slate-400 font-mono">
          {tx(displayRecs.length)} {tx(isAr ? 'توصية معروضة' : 'items')}
        </span>
      </div>

      {/* ── Recommendations Grid ── */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {displayRecs.map((rec) => {
          const priorityTheme = getPriorityTheme(rec.priority);
          const statusBadge = getStatusBadge(rec.status);
          const isRecurring = rec.isDailyHabit || rec.scheduleType === 'DailyRecurring';
          const isSimulatedSuccess = simulatedAlertSuccessId === rec.id;

          return (
            <div
              key={rec.id}
              className={`p-4 md:p-5 rounded-2xl bg-slate-950/70 border ${priorityTheme.border} ${priorityTheme.glow} flex flex-col justify-between gap-3.5 transition-all duration-300 relative overflow-hidden`}
            >
              {/* Top Accent Strip by Priority */}
              <div className={`absolute top-0 inset-x-0 h-1 ${priorityTheme.indicator}`} />

              <div>
                {/* Meta Top: Priority Badge + Schedule Badge + Status */}
                <div className="flex items-center justify-between gap-2 flex-wrap mb-2">
                  <div className="flex items-center gap-1.5 flex-wrap">
                    {/* Priority Badge */}
                    <span className={`text-[10px] font-extrabold px-2.5 py-0.5 rounded-full border ${priorityTheme.badge}`}>
                      {tx(priorityTheme.label)}
                    </span>

                    {/* Schedule Badge */}
                    {isRecurring ? (
                      <span className="inline-flex items-center gap-1 text-[10px] font-mono font-bold px-2.5 py-0.5 rounded-full bg-amber-500/15 text-amber-300 border border-amber-400/30">
                        <Clock className="w-3 h-3" />
                        <span>{tx(isAr ? `تكرار يومي [${rec.scheduledTime || '08:00'}]` : `Daily [${rec.scheduledTime || '08:00'}]`)}</span>
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 text-[10px] font-mono font-bold px-2 py-0.5 rounded-full bg-slate-800 text-slate-300 border border-white/10">
                        <Calendar className="w-3 h-3" />
                        <span>{tx(isAr ? 'تُنفذ مرة واحدة' : 'Once')}</span>
                      </span>
                    )}

                    {/* Category */}
                    <span className="inline-flex items-center gap-1 text-[10px] text-cyan-300 bg-cyan-950/70 px-2 py-0.5 rounded-full border border-cyan-400/30">
                      {getCategoryIcon(rec.category)}
                      <span>{tx(rec.category)}</span>
                    </span>
                  </div>

                  {/* Status Badge */}
                  <span className={`text-[10px] font-extrabold px-2.5 py-0.5 rounded-full border shrink-0 ${statusBadge.cls}`}>
                    {tx(statusBadge.label)}
                  </span>
                </div>

                {/* Title */}
                <h4 className="text-sm md:text-base font-extrabold text-white leading-snug mb-1.5 flex items-start gap-1.5">
                  <Sparkles className="w-4 h-4 text-cyan-400 shrink-0 mt-0.5" />
                  <span>{tx(isAr ? rec.titleAr || rec.title : rec.title)}</span>
                </h4>

                {/* Content / Directive Instructions */}
                <p className="text-xs text-slate-200 leading-relaxed mb-2 bg-slate-900/50 p-2.5 rounded-xl border border-white/5">
                  {tx(isAr ? rec.contentAr || rec.content : rec.content)}
                </p>

                {/* Physiological Rationale Box */}
                {(rec.reasonAr || rec.reason) && (
                  <div className="p-2.5 rounded-xl bg-cyan-950/30 border border-cyan-400/20 text-xs">
                    <span className="text-cyan-300 font-bold block text-[11px] mb-0.5">
                      {tx(isAr ? '🩺 السند والمبرر الفسيولوجي المداري:' : 'Physiological Rationale:')}
                    </span>
                    <p className="text-slate-300 text-[11px] leading-relaxed">
                      {tx(isAr ? rec.reasonAr || rec.reason : rec.reason)}
                    </p>
                  </div>
                )}
              </div>

              {/* Bottom Meta & Action Controls */}
              <div className="pt-2.5 border-t border-white/10 flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-2.5">
                {/* Prescribed By + Target */}
                <div className="text-[11px] text-slate-400 flex flex-col">
                  <span>
                    {tx(isAr ? 'المشرف: ' : 'Surgeon: ')}
                    <strong className="text-slate-200">{tx(rec.prescribedBy || (isAr ? 'د. سارة أوكونور' : 'Dr. Sarah O\'Connor'))}</strong>
                  </span>
                  <span>
                    {tx(isAr ? 'الموجه له: ' : 'Target: ')}
                    <strong className="text-cyan-300">{tx(rec.astronautFullName)}</strong>
                  </span>
                </div>

                {/* Astronaut / Doctor Action Buttons */}
                <div className="flex items-center gap-1.5 flex-wrap self-end sm:self-center">
                  {/* Simulate Scheduled Alert Button (Works for any recurring recommendation) */}
                  {isRecurring && rec.status !== 'Completed' && (
                    <AsyncButton
                      type="button"
                      onClick={() => handleSimulateAlert(rec.id)}
                      title={tx(isAr ? 'تجربة إطلاق إشعار التوصية التلقائي الآن فوراً' : 'Simulate auto notification trigger now')}
                      className={`px-2.5 py-1 rounded-lg border text-[11px] font-bold flex items-center gap-1 transition-all cursor-pointer ${
                        isSimulatedSuccess
                          ? 'bg-emerald-500/30 border-emerald-400 text-emerald-200'
                          : 'bg-amber-500/15 hover:bg-amber-500/25 border-amber-400/30 text-amber-200'
                      }`}
                    >
                      <Bell className="w-3 h-3 text-amber-400 animate-pulse" />
                      <span>{tx(isAr ? (isSimulatedSuccess ? 'تم إرسال الإشعار ✓' : 'اختبار الإشعار التلقائي') : (isSimulatedSuccess ? 'Alert Fired ✓' : 'Test Alert'))}</span>
                    </AsyncButton>
                  )}

                  {/* Move to In Progress */}
                  {rec.status === 'Pending' && (
                    <AsyncButton
                      type="button"
                      onClick={() => updateRecommendationStatus(rec.id, 'Acknowledged')}
                      className="px-3 py-1 rounded-lg bg-cyan-500/20 hover:bg-cyan-500/30 border border-cyan-400/30 text-cyan-200 text-[11px] font-bold transition-all cursor-pointer"
                    >
                      {tx(isAr ? 'بدء التنفيذ' : 'Start')}
                    </AsyncButton>
                  )}

                  {/* Complete */}
                  {!rec.isDailyHabit && rec.status !== 'Completed' && rec.status !== 'Dismissed' && (
                    <AsyncButton
                      type="button"
                      onClick={() => updateRecommendationStatus(rec.id, 'Completed')}
                      className="px-3 py-1 rounded-lg bg-emerald-500/20 hover:bg-emerald-500/30 border border-emerald-400/30 text-emerald-200 text-[11px] font-bold flex items-center gap-1 transition-all cursor-pointer"
                    >
                      <Check className="w-3 h-3 stroke-[2.5]" />
                      <span>{tx(isAr ? 'تم الإنجاز' : 'Complete')}</span>
                    </AsyncButton>
                  )}

                  {/* Reset back to Pending if Completed */}
                  {rec.status === 'Completed' && (
                    <AsyncButton
                      type="button"
                      onClick={() => updateRecommendationStatus(rec.id, 'Pending')}
                      className="px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-[11px] font-semibold transition-all cursor-pointer"
                    >
                      {tx(isAr ? 'إعادة التفعيل' : 'Reactivate')}
                    </AsyncButton>
                  )}

                  {/* Doctor Can Delete/Revoke Recommendation */}
                  {currentRole === 'Monitor' && (
                    <AsyncButton
                      type="button"
                      onClick={() => deleteRecommendation(rec.id)}
                      title={tx(isAr ? 'حذف أو إلغاء هذه التوصية' : 'Delete directive')}
                      className="p-1 rounded-lg hover:bg-rose-500/20 text-slate-400 hover:text-rose-300 transition-all cursor-pointer"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </AsyncButton>
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
