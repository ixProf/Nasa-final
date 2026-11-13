import { useEffect, useState } from 'react';
import { RefreshCw, Wifi, WifiOff, UserCheck } from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { useUiText } from '../../i18n/useUiText';
import { DetailedMedicalReport } from '../reports/DetailedMedicalReport';

function SimulatedSyncStatus() {
  const {language}=useLanguage();const ar=language==='ar';
  const [syncing,setSyncing]=useState(false);
  useEffect(()=>{
    let finish:ReturnType<typeof setTimeout>|undefined;
    const timer=setInterval(()=>{
      setSyncing(true);
      finish=setTimeout(()=>{setSyncing(false);},900);
    },5000);
    return()=>{clearInterval(timer);clearTimeout(finish);};
  },[]);
  return <div className="rounded-xl border border-cyan-400/20 bg-cyan-500/5 p-4 text-sm">
    <div className="flex items-center gap-2 text-cyan-200">
      <RefreshCw className={`h-4 w-4 ${syncing ? 'animate-spin motion-reduce:animate-none' : ''}`}/>
      <span>{syncing ? (ar?'جارٍ المزامنة…':'Syncing…') : (ar?'المزامنة':'Sync')}</span>
    </div>
  </div>;
}

export const MedicalDossierSyncPage=()=>{
  const {language}=useLanguage();const ar=language==='ar';const tx=useUiText();
  const {offlineQueue,isOnline,toggleOnlineStatus,currentUserName,activeAccount}=useTelemetry();
  return <div className="grid grid-cols-1 lg:grid-cols-12 gap-3.5 animate-fade-in items-start">
    <div className="lg:col-span-8"><DetailedMedicalReport/></div>
    <aside className="lg:col-span-4 glass-panel rounded-[22px] p-5 space-y-5 report-sync-panel">
      <div className="flex items-center justify-between gap-2">
        <h3 className="text-sm font-bold flex items-center gap-2">{isOnline?<Wifi className="h-4 w-4 text-emerald-300"/>:<WifiOff className="h-4 w-4 text-amber-300"/>}{ar?'الحساب والمزامنة':'Account and sync'}</h3>
        <button type="button" onClick={toggleOnlineStatus} className="rounded-lg border border-white/15 px-3 py-2 text-xs">{isOnline?(ar?'متصل':'Online'):(ar?'دون اتصال':'Offline')}</button>
      </div>
      <div className="rounded-xl border border-white/10 bg-slate-900/50 p-3 text-xs"><p className="flex gap-2 items-center"><UserCheck className="w-4 h-4 text-cyan-300"/>{currentUserName}</p><p className="text-slate-400 mt-2">{activeAccount}</p></div>
      <SimulatedSyncStatus/>
      <section><h4 className="text-sm font-semibold mb-3">{ar?'حالة سجلات الجهاز الفعلية':'Actual device record status'}</h4>
        <p className="text-xs text-slate-400 mb-3">{ar?'حالة سجلات الجهاز موضحة أدناه.':'Device record statuses are shown below.'}</p>
        <div className="space-y-2 max-h-80 overflow-y-auto">
          {!offlineQueue.length&&<p className="text-xs text-slate-400">{ar?'لا توجد سجلات في قائمة الجهاز.':'No records in the device queue.'}</p>}
          {offlineQueue.map(item=><div key={item.clientGeneratedId} className="rounded-xl border border-white/10 p-3 text-xs space-y-1"><p>{tx(item.summary)}</p><p className="text-slate-400">{new Date(item.recordedAt).toLocaleString(ar?'ar-EG':'en-US')}</p><p className={item.syncStatus==='Rejected'?'text-rose-300':item.syncStatus==='Pending'?'text-amber-300':'text-emerald-300'}>{item.syncStatus==='Rejected'?(ar?'مرفوض':'Rejected'):item.syncStatus==='Pending'?(ar?'معلق':'Pending'):(ar?'تمت المزامنة':'Synced')}</p></div>)}
        </div>
      </section>
    </aside>
  </div>;
};
