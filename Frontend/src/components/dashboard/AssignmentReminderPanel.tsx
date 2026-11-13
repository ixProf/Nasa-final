import { useEffect, useRef, useState } from 'react';
import { Bell, LoaderCircle } from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { api } from '../../services/api';
import { AssignmentReminder } from '../../types/reminders';
import { intervalInMinutes } from '../../services/reminderInbox';
import { translateUiText } from '../../i18n/uiText';
import { AsyncForm } from '../AsyncForm';
import { AsyncButton } from '../AsyncButton';

function ReminderRow({ item, monitor, refresh }: {item:AssignmentReminder;monitor:boolean;refresh:()=>Promise<void>}) {
  const {language}=useLanguage();const ar=language==='ar';
  const [enabled,setEnabled]=useState(item.enabled);
  const [unit,setUnit]=useState<'minutes'|'hours'>(item.intervalMinutes%60===0?'hours':'minutes');
  const [amount,setAmount]=useState(item.intervalMinutes%60===0?item.intervalMinutes/60:item.intervalMinutes);
  const [sound,setSound]=useState(item.soundEnabled);
  const [sent,setSent]=useState(false);
  const save=async()=>{
    const intervalMinutes=intervalInMinutes(amount,unit);
    await api.updateAssignmentReminder(item.recommendationId,{enabled,intervalMinutes,soundEnabled:sound});
    window.dispatchEvent(new Event('app:reminders-refresh'));await refresh();
  };
  return <div className="rounded-xl border border-white/10 bg-slate-900/60 p-3 space-y-3">
    <div className="flex flex-wrap gap-3 justify-between items-center"><h3 className="text-sm font-bold">{ar?(item.titleAr||translateUiText(item.title,'ar')):item.title}</h3>{item.completedToday&&<span className="text-xs text-emerald-300">{ar?'مكتمل اليوم — التكرار متوقف حتى الغد':'Done today — repeats pause until tomorrow'}</span>}</div>
    <AsyncForm onSubmit={save} className="flex flex-wrap gap-3 items-center text-xs">
      <label className="flex gap-2 items-center"><input type="checkbox" checked={enabled} onChange={e=>setEnabled(e.target.checked)}/>{ar?'تذكير تلقائي لهذه المهمة':'Automatic reminder for this task'}</label>
      <label className="flex gap-2 items-center">{ar?'كل':'Every'}<input type="number" min={1} max={unit==='hours'?168:10080} step={1} required value={amount} onChange={e=>setAmount(Number(e.target.value))} aria-label={ar?'الفاصل بين التذكيرات':'Reminder interval'} className="w-20 rounded-lg bg-slate-950 border border-white/15 p-2"/></label>
      <select value={unit} onChange={e=>setUnit(e.target.value as 'minutes'|'hours')} aria-label={ar?'وحدة الوقت':'Time unit'} className="rounded-lg bg-slate-950 border border-white/15 p-2"><option value="minutes">{ar?'دقيقة':'minutes'}</option><option value="hours">{ar?'ساعة':'hours'}</option></select>
      <label className="flex gap-2 items-center"><input type="checkbox" checked={sound} onChange={e=>setSound(e.target.checked)}/>{ar?'صوت مع التذكير':'Play a sound'}</label>
      <button type="submit" className="rounded-lg bg-cyan-700 px-3 py-2 font-bold">{ar?'حفظ التذكير':'Save reminder'}</button>
    </AsyncForm>
    {item.enabled&&!item.completedToday&&item.nextDueAt&&<p className="text-xs text-slate-400">{ar?'التذكير التالي: ':'Next reminder: '}{new Date(item.nextDueAt).toLocaleString(ar?'ar-EG':'en-GB')}</p>}
    {monitor&&<div className="flex gap-3 items-center"><AsyncButton type="button" disabled={item.completedToday} onClick={async()=>{await api.sendAssignmentReminder(item.recommendationId);setSent(true);}} className="rounded-lg bg-amber-500/15 border border-amber-400/40 px-3 py-2 text-xs font-bold text-amber-200">{ar?'إرسال تذكير الآن للرائد':'Send reminder to astronaut now'}</AsyncButton>{sent&&<span role="status" className="text-xs text-emerald-300">{ar?'تم إرسال التذكير وحفظه في صندوق الرائد.':'Reminder sent and saved in the astronaut’s inbox.'}</span>}</div>}
  </div>;
}

export function AssignmentReminderPanel({astronautId,monitor=false,refreshKey=''}:{astronautId?:number;monitor?:boolean;refreshKey?:string}) {
  const {language}=useLanguage();const ar=language==='ar';
  const [items,setItems]=useState<AssignmentReminder[]>([]);
  const [loading,setLoading]=useState(false),[error,setError]=useState(false);
  const version=useRef(0);
  const refreshRef=useRef<()=>Promise<void>>(async()=>{});
  useEffect(()=>{
    let mounted=true;const owner=++version.current;
    const load=async()=>{
      setLoading(true);setError(false);
      try {const data=await api.getAssignmentReminders(astronautId);if(mounted&&version.current===owner)setItems(data.assignments);}
      catch {if(mounted)setError(true);}
      finally {if(mounted)setLoading(false);}
    };
    setItems([]);refreshRef.current=load;void load();
    const incoming=()=>{void load();};window.addEventListener('app:assignment-reminder',incoming);
    return()=>{mounted=false;version.current++;window.removeEventListener('app:assignment-reminder',incoming);};
  },[astronautId,refreshKey]);
  return <section className="glass-panel rounded-2xl p-4 space-y-3">
    <h2 className="flex items-center gap-2 font-bold text-cyan-200"><Bell className="w-4 h-4"/>{ar?'تذكيرات المهام':'Assignment reminders'}</h2>
    <p className="text-xs text-slate-300">{monitor?(ar?'حدّد فاصل التكرار أو أرسل تذكيرًا فوريًا للرائد.':'Set repeat intervals or send an immediate reminder to the astronaut.'):(ar?'اختر المهام التي تريد تذكيرًا بها وحدّد كل كم دقيقة أو ساعة.':'Choose which tasks remind you and how often in minutes or hours.')}</p>
    {!monitor&&<p className="text-xs text-slate-400">{ar?'يمكن أن يصلك تذكير مباشر من طبيب الرحلة حتى عند إيقاف التذكير التلقائي.':'Direct flight surgeon reminders can still arrive when automatic reminders are off.'}</p>}
    {loading&&<p role="status" className="text-xs flex gap-2"><LoaderCircle className="w-4 h-4 animate-spin"/>{ar?'جارٍ تحميل التذكيرات…':'Loading reminders…'}</p>}
    {error&&<p role="alert" className="text-xs text-rose-200">{ar?'تعذّر تحميل التذكيرات. تحقق من الاتصال وتحديث قاعدة البيانات.':'Could not load reminders. Check the connection and database migrations.'} <button type="button" className="underline" onClick={()=>refreshRef.current()}>{ar?'إعادة المحاولة':'Retry'}</button></p>}
    {!loading&&!error&&!items.length&&<p className="text-sm text-slate-400">{ar?'لا توجد مهام نشطة.':'No active assignments.'}</p>}
    {items.map(item=><ReminderRow key={`${item.recommendationId}:${item.enabled}:${item.intervalMinutes}:${item.soundEnabled}`} item={item} monitor={monitor} refresh={()=>refreshRef.current()}/>)}
  </section>;
}
