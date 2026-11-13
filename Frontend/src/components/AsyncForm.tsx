import { useUiText } from "../i18n/useUiText";
import { FormEvent, FormHTMLAttributes, useRef, useState } from 'react';
import { LoaderCircle, CheckCircle2, AlertCircle } from 'lucide-react';
import { useLanguage } from '../context/LanguageContext';

export function AsyncForm({ onSubmit, children, ...props }: FormHTMLAttributes<HTMLFormElement>) {
  const tx = useUiText();
  const { language } = useLanguage();
  const busy = useRef(false);
  const [state, setState] = useState<'idle' | 'saving' | 'saved' | 'queued' | 'error'>('idle');
  const [error, setError] = useState('');
  const isAr = language === 'ar';
  const submit = async (event: Parameters<NonNullable<FormHTMLAttributes<HTMLFormElement>['onSubmit']>>[0]) => {
    event.preventDefault();
    if (busy.current || !onSubmit) return;
    busy.current = true;
    setState('saving'); setError('');
    try { await onSubmit(event); setState('saved'); }
    catch (failure) { setError(failure instanceof Error ? failure.message : String(failure)); setState((failure as {queued?:boolean})?.queued?'queued':'error'); }
    finally { busy.current = false; }
  };
  return <form {...props} onSubmit={submit} aria-busy={state === 'saving'} onChange={() => { if (!busy.current) setState('idle'); }}>
    <fieldset disabled={state === 'saving'} className="contents">{children}</fieldset>
    {state !== 'idle' && <div className="col-span-full flex items-center gap-2 text-xs" role={state === 'error' ? 'alert' : 'status'} aria-live="polite">
      {state === 'saving' ? <LoaderCircle className="w-4 h-4 animate-spin"/> : state === 'saved' ? <CheckCircle2 className="w-4 h-4 text-emerald-400"/> : <AlertCircle className="w-4 h-4 text-rose-400"/>}
      <span>{tx(state === 'queued' ? (isAr ? 'في انتظار المزامنة' : 'Pending synchronization') : state === 'saving' ? (isAr ? 'جارٍ الحفظ…' : 'Saving…') : state === 'saved' ? (isAr ? 'تم الحفظ' : 'Saved') : (isAr ? 'تعذّر الحفظ. حاول مرة أخرى.' : 'Could not save. Please retry.'))}</span>
      {(state === 'error' || state === 'queued') && <span>{tx(error)}</span>}
    </div>}
  </form>;
}
