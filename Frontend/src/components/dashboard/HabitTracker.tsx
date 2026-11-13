import { useCallback, useEffect, useRef, useState } from 'react';
import { Award, Check, Flame, LoaderCircle, Trophy } from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { api } from '../../services/api';
import { HabitDashboard, HabitDay } from '../../types/habits';
import { translateUiText } from '../../i18n/uiText';

export function HabitTracker({ astronautId, readOnly = false, refreshKey = '' }: { astronautId?: number; readOnly?: boolean; refreshKey?: string }) {
  const { language } = useLanguage();
  const ar = language === 'ar';
  const [data, setData] = useState<HabitDashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState<number | null>(null);
  const [saved, setSaved] = useState(false);
  const guard = useRef(false);
  const generation = useRef(0);
  const [day, setDay] = useState<HabitDay | null>(null);
  const reload = useCallback(async () => {
    const version = ++generation.current;
    setLoading(true); setError(''); setData(null); setDay(null);
    try { const result = await api.getHabits(astronautId); if (version === generation.current) setData(result); }
    catch { if (version === generation.current) setError(ar ? 'تعذّر تحميل العادات. تحقق من الاتصال وتطبيق تحديث قاعدة البيانات ثم أعد المحاولة.' : 'Could not load habits. Check the connection and apply the database migration, then retry.'); }
    finally { if (version === generation.current) setLoading(false); }
  }, [astronautId, ar]);
  useEffect(() => { void reload(); return () => { generation.current++; }; }, [reload, refreshKey]);
  useEffect(() => {
    const refresh = () => { if (!guard.current) void reload(); };
    window.addEventListener('focus', refresh);
    window.addEventListener('app:assignment-reminder',refresh);
    return () => { window.removeEventListener('focus', refresh);window.removeEventListener('app:assignment-reminder',refresh); };
  }, [reload]);
  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>;
    const schedule = () => {
      const now = new Date();
      const next = Date.UTC(now.getUTCFullYear(),now.getUTCMonth(),now.getUTCDate()+1);
      timer = setTimeout(() => { if (!guard.current) void reload(); schedule(); },next-Date.now()+1000);
    };
    schedule();
    return () => clearTimeout(timer);
  }, [reload]);
  const complete = async (id: number) => {
    if (guard.current) return;
    const version = generation.current;
    guard.current = true; setBusy(id); setError(''); setSaved(false);
    try { const result = await api.completeHabit(id); if (version === generation.current) { setData(result); setSaved(true); } }
    catch { if (version === generation.current) setError(ar ? 'لم يُحفظ الإنجاز. أعد المحاولة؛ لن تتكرر النقاط.' : 'Completion was not confirmed. Retry safely; points will not be duplicated.'); }
    finally { guard.current = false; setBusy(null); }
  };
  const badgeNames: Record<string,string> = ar
    ? { 'first-step':'الخطوة الأولى', 'week-streak':'أسبوع متواصل', 'month-streak':'شهر متواصل', 'century':'مئة نقطة' }
    : { 'first-step':'First step', 'week-streak':'Seven-day streak', 'month-streak':'Thirty-day streak', 'century':'100 points' };
  const number = (value: number) => value.toLocaleString(ar ? 'ar' : 'en');
  const dateLabel = (date: string) => new Date(date+'T00:00:00Z').toLocaleDateString(ar?'ar-EG':'en-GB',{timeZone:'UTC',day:'numeric',month:'short',year:'numeric'});
  return <section className="glass-panel rounded-[22px] p-5 space-y-4" aria-busy={loading}>
    <div className="flex justify-between gap-3 items-center"><div><h2 className="font-bold text-white flex gap-2 items-center"><Flame className="w-5 h-5 text-amber-400"/>{ar?'العادات اليومية والإنجازات':'Daily habits & achievements'}</h2><p className="text-xs text-slate-300 mt-1">{ar?'مهام يحددها الطبيب، وإنجازات تُحتسب مرة واحدة يوميًا بتوقيت غرينتش.':'Doctor-assigned activities. Each completion counts once per day in UTC.'}</p></div><button type="button" onClick={reload} disabled={loading || busy !== null} className="text-xs text-cyan-300 disabled:opacity-50">{ar?'تحديث':'Refresh'}</button></div>
    {loading && <p role="status" className="flex gap-2 text-sm"><LoaderCircle className="w-4 h-4 animate-spin"/>{ar?'جارٍ تحميل العادات…':'Loading habits…'}</p>}
    {error && <p role="alert" className="text-rose-300 text-sm">{error} <button type="button" onClick={reload} className="underline">{ar?'إعادة المحاولة':'Retry'}</button></p>}
    {saved && <p role="status" className="text-emerald-300 text-xs">{ar?'تم حفظ الإنجاز وتحديث النقاط.':'Completion saved. Your points are up to date.'}</p>}
    {data && <>
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {[ [ar?'النقاط':'Points',data.totalPoints], [ar?'المستوى':'Level',data.level], [ar?'السلسلة الحالية':'Current streak',data.currentStreak], [ar?'أطول سلسلة':'Best streak',data.longestStreak] ].map(([label,value])=><div key={label} className="rounded-xl bg-slate-900/70 border border-white/10 p-3"><p className="text-xs text-slate-300">{label}</p><p className="text-2xl font-extrabold text-cyan-300 mt-1">{number(Number(value))}</p></div>)}
      </div>
      <div className="space-y-2 text-xs text-slate-300">
        <p>{ar?'إنجازات اليوم: ':'Today: '}{number(data.assignments.filter(h=>h.completedToday).length)} / {number(data.assignments.length)}</p>
        <label className="flex gap-2 items-center">{ar?'التقدم نحو المستوى التالي':'Progress to next level'}<progress className="flex-1 h-2 accent-cyan-400" max={100} value={data.totalPoints%100} aria-label={ar?'التقدم نحو المستوى التالي':'Progress to next level'}/><span>{number(data.totalPoints%100)} / {number(100)}</span></label>
      </div>
      <div className="flex flex-wrap gap-2">{data.badges.map(b=><span key={b} className="flex items-center gap-1 rounded-full bg-amber-500/10 border border-amber-400/30 px-3 py-1 text-xs text-amber-200"><Award className="w-3 h-3"/>{badgeNames[b]}</span>)}</div>
      <div className="space-y-2">
        {!data.assignments.length && <p className="text-sm text-slate-300">{ar?'لا توجد عادات مسندة بعد. يستطيع الطبيب إضافة مهمة يومية من نموذج التوصيات.':'No habits assigned yet. A monitor can assign a daily activity from the recommendation form.'}</p>}
        {data.assignments.map(h=><article key={h.id} className="rounded-xl bg-slate-900/60 border border-white/10 p-3 flex items-center gap-3 justify-between"><div className="min-w-0"><h3 className="text-sm font-bold">{ar ? h.titleAr || translateUiText(h.title,'ar') : h.title}</h3><p className="text-xs text-slate-300 mt-1">{ar ? h.contentAr || translateUiText(h.content,'ar') : h.content}</p><p className="text-xs text-slate-400 mt-1">{ar?'السبب: ':'Reason: '}{ar ? h.reasonAr || translateUiText(h.reason,'ar') : h.reason}</p><span className="inline-flex text-xs text-amber-300 items-center gap-1 mt-2"><Trophy className="w-3 h-3"/>{number(h.points)} {ar?'نقطة':'points'}</span></div><button type="button" disabled={readOnly || h.completedToday || busy!==null} onClick={()=>complete(h.id)} className="shrink-0 text-xs font-bold rounded-xl border border-emerald-400/40 bg-emerald-500/15 p-3 disabled:opacity-60 flex items-center gap-1">{busy===h.id ? <LoaderCircle className="w-4 h-4 animate-spin"/> : h.completedToday ? <Check className="w-4 h-4"/> : null}{busy===h.id ? (ar?'جارٍ الحفظ…':'Saving…') : h.completedToday ? (ar?'مكتمل اليوم':'Done today') : readOnly ? (ar?'بانتظار الرائد':'Awaiting astronaut') : (ar?'أنجزت المهمة':'Mark complete')}</button></article>)}
      </div>
      <div className="rounded-xl border border-white/10 bg-slate-950/50 p-4">
        <h3 className="text-sm font-bold mb-3">{ar?'خريطة النشاط — آخر ١٢ أسبوعًا':'Activity heatmap — last 12 weeks'}</h3>
        <ol className="grid gap-1 w-fit" style={{gridTemplateRows:'repeat(7, 1fr)',gridAutoFlow:'column'}}>
          {data.heatmap.map(d=><li key={d.date}><button type="button" onClick={()=>setDay(d)} title={`${dateLabel(d.date)}: ${number(d.count)} ${ar?'إنجاز':'completed'}, ${number(d.points)} ${ar?'نقطة':'points'}`} aria-label={`${dateLabel(d.date)}: ${number(d.count)} ${ar?'إنجاز':'completed'}`} className={`block w-4 h-4 sm:w-5 sm:h-5 rounded-sm border border-white/10 focus:outline-cyan-300 ${d.count===0?'bg-slate-800':d.count===1?'bg-emerald-900':d.count===2?'bg-emerald-700':d.count===3?'bg-emerald-500':'bg-emerald-300'}`}/></li>)}
        </ol>
        <div className="text-xs text-slate-400 mt-3 flex gap-2 items-center">{ar?'أقل':'Less'}<span className="w-3 h-3 rounded-sm bg-slate-800"/><span className="w-3 h-3 rounded-sm bg-emerald-900"/><span className="w-3 h-3 rounded-sm bg-emerald-500"/><span className="w-3 h-3 rounded-sm bg-emerald-300"/>{ar?'أكثر':'More'}</div>
        {day && <p role="status" className="text-xs text-cyan-200 mt-3">{dateLabel(day.date)} · {number(day.count)} {ar?'إنجاز':'completed'} · {number(day.points)} {ar?'نقطة':'points'}</p>}
      </div>
    </>}
  </section>;
}
