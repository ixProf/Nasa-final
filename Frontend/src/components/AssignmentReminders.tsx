import { useEffect, useRef, useState } from 'react';
import { BellRing, Volume2, LoaderCircle } from 'lucide-react';
import { useLanguage } from '../context/LanguageContext';
import { useTelemetry } from '../context/TelemetryContext';
import { api } from '../services/api';
import { ReminderDelivery } from '../types/reminders';
import { mergeReminderInbox } from '../services/reminderInbox';
import { translateUiText } from '../i18n/uiText';

export function AssignmentReminders() {
  const { language } = useLanguage();
  const { currentAstronautId } = useTelemetry();
  const ar = language === 'ar';
  const locale = useRef(ar); locale.current = ar;
  const [queue,setQueue] = useState<ReminderDelivery[]>([]);
  const [soundReady,setSoundReady] = useState(false);
  const [notifications,setNotifications] = useState(false);
  const [permissionMessage,setPermissionMessage] = useState('');
  const [error,setError] = useState(false);
  const [busy,setBusy] = useState(false);
  const audio = useRef<AudioContext | null>(null);
  const lastChime = useRef(0);
  const soundOn = useRef(false);
  const preferSound = useRef(false);
  const browserOn = useRef(false);
  const chime = useRef<() => void>(()=>{});
  const retry = useRef<() => void>(()=>{});
  const ackGuard = useRef(false);
  const dismissed = useRef(new Set<string>());
  chime.current = () => {
    const context = audio.current;
    if (!context || context.state !== 'running' || !soundOn.current) return;
    if (Date.now()-lastChime.current<1000) return;
    lastChime.current=Date.now();
    // A brief, bounded two-note reminder, distinct from the emergency alarm.
    for (const [offset,frequency] of [[0,660],[0.22,880]]) {
      const oscillator=context.createOscillator(), gain=context.createGain();
      oscillator.frequency.value=frequency; gain.gain.setValueAtTime(0,context.currentTime+offset);
      gain.gain.linearRampToValueAtTime(0.12,context.currentTime+offset+0.02);
      gain.gain.exponentialRampToValueAtTime(0.001,context.currentTime+offset+0.2);
      oscillator.connect(gain);gain.connect(context.destination);
      oscillator.onended=()=>{oscillator.disconnect();gain.disconnect();};
      oscillator.start(context.currentTime+offset);oscillator.stop(context.currentTime+offset+0.22);
    }
  };

  const saveDevicePreference=(patch:{sound?:boolean;notifications?:boolean})=>{
    if(patch.sound!==undefined)preferSound.current=patch.sound;
    if(!currentAstronautId)return;
    const key='assignment-reminder-device:'+currentAstronautId;
    try {const previous=JSON.parse(localStorage.getItem(key)||'{}');localStorage.setItem(key,JSON.stringify({...previous,...patch}));}catch{/* Preferences remain usable for this session. */}
  };
  useEffect(()=>{setPermissionMessage('');},[language]);
  useEffect(()=>{
    if(!currentAstronautId)return;
    preferSound.current=false;
    try {
      const prefs=JSON.parse(localStorage.getItem('assignment-reminder-device:'+currentAstronautId)||'{}');
      preferSound.current=prefs.sound===true;
      browserOn.current=prefs.notifications===true&&'Notification' in window&&Notification.permission==='granted';
      setNotifications(browserOn.current);
    }catch{/* Use defaults when storage is unavailable. */}
    const arm=(event:PointerEvent)=>{
      if(!preferSound.current||(soundOn.current&&audio.current?.state==='running')||(event.target instanceof Element&&event.target.closest('[data-reminder-audio]')))return;
      try {
        audio.current ||= new AudioContext();const context=audio.current;
        void context.resume().then(()=>{soundOn.current=context.state==='running';setSoundReady(soundOn.current);}).catch(()=>{setSoundReady(false);});
      }catch{setSoundReady(false);}
    };
    window.addEventListener('pointerdown',arm);
    return()=>window.removeEventListener('pointerdown',arm);
  },[currentAstronautId]);

  useEffect(() => {
    if (!currentAstronautId) return;
    const owner=currentAstronautId;
    let mounted=true,loading=false,paused=false,eventVersion=0;
    const seen=new Set<string>(); dismissed.current.clear(); setQueue([]);
    const storageKey='assignment-reminders-seen:'+owner;
    let played: string[]=[];
    try { const parsed=JSON.parse(localStorage.getItem(storageKey)||'[]'); if(Array.isArray(parsed))played=parsed.filter(v=>typeof v==='string').slice(-200); } catch { /* A missing cache must not prevent delivery. */ }
    const receive=(rows:ReminderDelivery[])=>{
      if(!mounted)return;
      const incoming=rows.filter(r=>r.astronautId===owner&&!seen.has(r.token)&&!dismissed.current.has(r.token));
      if(!incoming.length)return;
      eventVersion++;
      let fresh:ReminderDelivery|undefined, shouldChime=false;
      for(const r of incoming){seen.add(r.token);if(!played.includes(r.token)){fresh ||= r;shouldChime ||= r.soundEnabled;played.push(r.token);}}
      played=played.slice(-200);
      try { localStorage.setItem(storageKey,JSON.stringify(played)); } catch { /* Delivery remains available without storage. */ }
      setQueue(prev=>mergeReminderInbox(prev,incoming,owner));
      if(fresh){
        if(shouldChime)chime.current();
        if(browserOn.current&&'Notification' in window&&Notification.permission==='granted'&&document.hidden){
          const title=locale.current?(fresh.titleAr||translateUiText(fresh.title,'ar')):fresh.title;
          try {
            const notification=new Notification(locale.current?'تذكير بالمهمة: '+title:'Assignment reminder: '+title,{body:locale.current?(fresh.contentAr||translateUiText(fresh.content,'ar')):fresh.content,tag:'assignment:'+fresh.recommendationId,silent:true});
            notification.onclick=()=>{window.focus();notification.close();window.dispatchEvent(new CustomEvent('app:navigate',{detail:'recommendations'}));};
          } catch { setPermissionMessage(locale.current?'إشعار المتصفح غير متاح. سيظل التذكير ظاهرًا داخل التطبيق.':'Browser notifications are unavailable. In-app reminders remain active.'); }
        }
      }
    };
    const load=async()=>{
      if(!mounted||loading||paused)return;
      loading=true;const version=eventVersion;
      try {
        const data=await api.getAssignmentReminders();
        if(!mounted)return;
        setError(false);
        if(version===eventVersion){
          const active=new Set(data.assignments.filter(a=>!a.completedToday).map(a=>a.recommendationId));
          const automatic=new Set(data.assignments.filter(a=>a.enabled).map(a=>a.recommendationId));
          setQueue(prev=>prev.filter(r=>active.has(r.recommendationId)&&(r.isManual||automatic.has(r.recommendationId))));
        }
        receive(data.pending);
      }catch { if(mounted){paused=true;setError(true);} }
      finally {loading=false;}
    };
    retry.current=()=>{paused=false;void load();};
    const incoming=(event:Event)=>receive([(event as CustomEvent<ReminderDelivery>).detail]);
    const focus=()=>{paused=false;void load();};
    const complete=(event:Event)=>{const id=(event as CustomEvent<number>).detail;setQueue(prev=>prev.filter(r=>r.recommendationId!==id));};
    window.addEventListener('app:assignment-reminder',incoming);
    window.addEventListener('app:reminders-refresh',focus);
    window.addEventListener('app:assignment-completed',complete);
    window.addEventListener('focus',focus);
    const timer=setInterval(()=>{void load();},30000);void load();
    return()=>{mounted=false;clearInterval(timer);window.removeEventListener('app:assignment-reminder',incoming);window.removeEventListener('app:reminders-refresh',focus);window.removeEventListener('app:assignment-completed',complete);window.removeEventListener('focus',focus);};
  },[currentAstronautId]);
  useEffect(()=>()=>{void audio.current?.close();audio.current=null;},[]);

  const enableSound=async()=>{
    setPermissionMessage('');
    try {
      if(soundOn.current&&audio.current?.state==='running'){soundOn.current=false;setSoundReady(false);saveDevicePreference({sound:false});return;}
      if(!audio.current)audio.current=new AudioContext();
      await audio.current.resume();
      soundOn.current=audio.current.state==='running';setSoundReady(soundOn.current);chime.current();
      saveDevicePreference({sound:soundOn.current});
      if(!soundOn.current)setPermissionMessage(ar?'اضغط مرة أخرى للسماح بالصوت من المتصفح.':'Click again to allow audio in your browser.');
    }catch {setPermissionMessage(ar?'تعذّر تشغيل الصوت. التذكير المنبثق لا يزال يعمل.':'Audio could not be enabled. Pop-up reminders still work.');}
  };
  const enableNotifications=async()=>{
    setPermissionMessage('');
    if(browserOn.current){browserOn.current=false;setNotifications(false);saveDevicePreference({notifications:false});return;}
    if(!('Notification' in window)){setPermissionMessage(ar?'المتصفح لا يدعم الإشعارات.':'This browser does not support notifications.');return;}
    try {
      const permission=await Notification.requestPermission();
      browserOn.current=permission==='granted';setNotifications(browserOn.current);
      saveDevicePreference({notifications:browserOn.current});
      if(permission!=='granted')setPermissionMessage(ar?'لم يُسمح بإشعارات المتصفح. يمكنك تفعيلها من إعدادات الموقع.':'Browser notifications are not allowed. You can enable them in site settings.');
    }catch {setPermissionMessage(ar?'تعذّر طلب الإذن. استخدم اتصالًا آمنًا أو العنوان المحلي.':'Permission could not be requested. Use HTTPS or localhost.');}
  };
  const active=queue[0];
  const dismiss=async(open=false)=>{
    if(!active||ackGuard.current)return;
    ackGuard.current=true;setBusy(true);
    try {
      await api.acknowledgeAssignmentReminder(active.recommendationId,active.token);
      dismissed.current.add(active.token);setQueue(prev=>prev.filter(r=>r.token!==active.token));
      if(open)window.dispatchEvent(new CustomEvent('app:navigate',{detail:'recommendations'}));
    }catch {setError(true);}finally{ackGuard.current=false;setBusy(false);}
  };
  return <>
    <div className="rounded-xl border border-cyan-400/20 bg-slate-950/60 p-3 text-xs flex flex-wrap items-center gap-3">
      <span className="flex items-center gap-1 text-cyan-200"><BellRing className="w-4 h-4"/>{ar?'تذكيرات المهام':'Assignment reminders'}</span>
      <button type="button" data-reminder-audio onClick={enableSound} aria-pressed={soundReady} className="rounded-lg border border-white/20 px-3 py-1.5 flex items-center gap-1"><Volume2 className="w-3.5 h-3.5"/>{soundReady?(ar?'كتم صوت التذكيرات':'Mute reminders'):(ar?'تفعيل صوت التذكيرات':'Enable reminder sound')}</button>
      <button type="button" onClick={enableNotifications} aria-pressed={notifications} className="rounded-lg border border-white/20 px-3 py-1.5">{notifications?(ar?'إيقاف إشعارات المتصفح':'Disable browser notifications'):(ar?'تفعيل إشعارات المتصفح':'Enable browser notifications')}</button>
      <button type="button" onClick={()=>window.dispatchEvent(new CustomEvent('app:navigate',{detail:'recommendations'}))} className="rounded-lg border border-cyan-400/30 px-3 py-1.5 text-cyan-200">{ar?'اختيار المهام وفاصل التذكير':'Choose tasks & reminder interval'}</button>
      <span className="text-slate-400">{ar?'أبقِ التطبيق مفتوحًا لاستقبال الصوت والإشعارات.':'Keep the app open to receive sound and notifications.'}</span>
      {permissionMessage&&<span role="status" className="text-amber-200">{permissionMessage}</span>}
      {error&&<span role="alert" className="text-rose-200">{ar?'تعذّر تحديث التذكيرات.':'Could not update reminders.'} <button type="button" className="underline" onClick={()=>retry.current()}>{ar?'إعادة المحاولة':'Retry'}</button></span>}
    </div>
    {active&&<section role="dialog" aria-modal="false" aria-labelledby="assignment-reminder-title" className="fixed bottom-5 end-5 z-[70] w-[min(94vw,420px)] rounded-2xl border border-amber-400/70 bg-slate-950 p-5 shadow-2xl text-white" dir={ar?'rtl':'ltr'}>
      <div className="flex items-center gap-2 text-amber-300 font-bold"><BellRing className="w-5 h-5 animate-pulse"/>{active.isManual?(ar?'تذكير من طبيب الرحلة':'Flight surgeon reminder'):(ar?'تذكير بالمهمة':'Assignment reminder')}<span className="ms-auto text-xs">{queue.length>1?queue.length:''}</span></div>
      <h2 id="assignment-reminder-title" className="mt-3 font-bold text-lg">{ar?(active.titleAr||translateUiText(active.title,'ar')):active.title}</h2>
      <p className="text-sm text-slate-200 mt-2">{ar?(active.contentAr||translateUiText(active.content,'ar')):active.content}</p>
      {!soundReady&&active.soundEnabled&&<button type="button" data-reminder-audio onClick={enableSound} className="text-xs text-cyan-300 underline mt-3">{ar?'اضغط لتفعيل صوت التذكيرات':'Click to enable reminder sound'}</button>}
      <div className="flex gap-2 mt-4"><button disabled={busy} type="button" onClick={()=>dismiss(true)} className="rounded-xl bg-cyan-600 px-4 py-2 text-sm font-bold disabled:opacity-50">{busy?<LoaderCircle className="w-4 h-4 animate-spin"/>:(ar?'فتح المهمة':'Open assignment')}</button><button disabled={busy} type="button" onClick={()=>dismiss()} className="rounded-xl border border-white/20 px-4 py-2 text-sm disabled:opacity-50">{ar?'إغلاق هذا التذكير':'Dismiss this reminder'}</button></div>
    </section>}
  </>;
}
