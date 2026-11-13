import { ButtonHTMLAttributes, useRef, useState } from 'react';
import { LoaderCircle } from 'lucide-react';
import { reportError } from '../services/errors';
import { useLanguage } from '../context/LanguageContext';

export function AsyncButton({ onClick, children, disabled, ...props }: ButtonHTMLAttributes<HTMLButtonElement>) {
  const guard = useRef(false);
  const [pending,setPending] = useState(false);
  const { language } = useLanguage();
  return <button {...props} disabled={disabled || pending} aria-busy={pending} onClick={async event => {
    if (guard.current) return;
    guard.current = true;
    try {
      const operation = onClick?.(event) as unknown;
      if (operation && typeof (operation as Promise<unknown>).then === 'function') {
        setPending(true); await operation;
      }
    } catch (error) { reportError(error); }
    finally { guard.current = false; setPending(false); }
  }}>
    {pending && <LoaderCircle className="w-3.5 h-3.5 animate-spin inline-block me-1" aria-label={language==='ar'?'جارٍ الحفظ':'Saving'}/>}{children}
  </button>;
}
