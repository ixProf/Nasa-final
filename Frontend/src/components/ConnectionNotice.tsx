import { useEffect, useState } from 'react';
import { CheckCircle2, LoaderCircle } from 'lucide-react';
import { useLanguage } from '../context/LanguageContext';
import { useUiText } from '../i18n/useUiText';

export function ConnectionNotice() {
  const tx = useUiText();
  const { language } = useLanguage();
  const [message,setMessage] = useState('');
  const [pending,setPending] = useState(0);
  const [saved,setSaved] = useState(false);
  useEffect(() => {
    const writes = new Set<number>();
    let timer: ReturnType<typeof setTimeout>;
    const onError = (event: Event) => { setSaved(false); setMessage((event as CustomEvent<string>).detail); };
    const onWrite = (event: Event) => {
      const {id,phase} = (event as CustomEvent<{id:number;phase:string}>).detail;
      if (phase==='saving') { writes.add(id); setSaved(false); setMessage(''); }
      else { writes.delete(id); if (phase==='saved' && writes.size===0) { setSaved(true); clearTimeout(timer); timer=setTimeout(()=>setSaved(false),3500); } }
      setPending(writes.size);
    };
    window.addEventListener('app:error',onError); window.addEventListener('app:write',onWrite);
    return () => { clearTimeout(timer); window.removeEventListener('app:error',onError); window.removeEventListener('app:write',onWrite); };
  },[]);
  if (!message && !pending && !saved) return null;
  const ar = language==='ar';
  return <div role={message?'alert':'status'} aria-live="polite" className={`fixed bottom-5 end-5 max-w-md z-50 rounded-xl border bg-slate-950 px-4 py-3 shadow-xl text-sm ${message?'border-rose-400 text-rose-200':'border-cyan-400/40 text-cyan-100'}`}>
    <div className="flex items-center gap-2">{pending>0?<LoaderCircle className="w-4 h-4 animate-spin"/>:!message?<CheckCircle2 className="w-4 h-4 text-emerald-300"/>:null}<span>{message?tx(message):pending?(ar?'جارٍ الحفظ…':'Saving…'):(ar?'تم الحفظ':'Saved')}</span>
      <button type="button" className="ms-3" onClick={()=>{setMessage('');setSaved(false);}} aria-label={ar?'إغلاق':'Dismiss'}>×</button>
    </div>
  </div>;
}
