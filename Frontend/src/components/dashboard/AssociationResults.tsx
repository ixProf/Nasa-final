import { conditionArabic, symptomArabic } from '../../i18n/clinical';
import { useLanguage } from '../../context/LanguageContext';

export interface AssociationAssessment {
  offline?:boolean;
  symptomFindings?:Array<{code:string;name:string;severity:string;durationHours:number;level:number}>;
  engineVersion:string; level:number; redFlags:string[]; triggeredAlertId?:number; alertDeliveryError?:string;
  hazardScores:Array<{hazard:string;index:number;strength:string;explanation:string;explanationAr:string;conditionCode:string}>;
  conditionMatches:Array<{conditionCode:string;conditionName:string;associationIndex:number;probabilityPercent?:number|null;probabilityStatus?:string;followUpLevel?:number;matchedSymptomCodes?:string[];followUpReasons?:string[];strength:string;breakdown:Record<string,number|string>}>;
  knowledgeOnly:Array<{code:string;name:string;explanation:string;explanationAr:string}>;
  radiation:{status:string;explanation:string;explanationAr:string}; guidanceMode:string; commsDelaySeconds:number;
  recommendedActions:string[];footer:string;footerAr:string;contextWarnings:string[];
}
export function AssociationResults({result}:{result:AssociationAssessment}) {
  const {language}=useLanguage();const ar=language==='ar';
  const hazards:Record<string,string>=ar?{GRAVITY:'الجاذبية',CLOSED_ENV:'البيئة المغلقة',ISOLATION:'العزلة والحبس'}:{GRAVITY:'Gravity',CLOSED_ENV:'Closed environment',ISOLATION:'Isolation and confinement'};
  const strength:Record<string,string>=ar?{weak:'ضعيف',moderate:'متوسط',strong:'قوي'}:{weak:'Weak',moderate:'Moderate',strong:'Strong'};
  const englishLabels:Record<string,string>={matchedWeight:"Matched weight",totalWeight:"Total weight",coverage:"Coverage",evidence:"Evidence",phaseGate:"Phase adjustment",vitalBoost:"Reading adjustment",contextBoost:"Context adjustment",cap:"Score limit",uncappedIndex:"Score before limit",phaseState:"Mission phase"};
  const name=(code:string,fallback:string)=>ar?(conditionArabic[code]?.nameAr || fallback):fallback;
  const labels:Record<string,string>={matchedWeight:'الوزن المتطابق',totalWeight:'الوزن الكلي',coverage:'التغطية',evidence:'قوة الدليل',phaseGate:'معامل المرحلة',vitalBoost:'تعزيز المؤشرات',contextBoost:'تعزيز السياق',cap:'السقف',uncappedIndex:'المؤشر قبل السقف',phaseState:'حالة المرحلة'};
  const phases:Record<string,string>={inside_window:'داخل النافذة',outside_window:'خارج النافذة',unknown:'غير معروف',not_applicable:'لا ينطبق'};
  return <section className="mt-4 space-y-3" aria-live="polite">
    {result.offline && <p role="status" className="text-amber-200">{ar?'تقييم محلي دون اتصال من دليل محفوظ. سياق المهمة والمؤشرات الحالية غير متاحين؛ لم يُرسل أي تنبيه. أعد التقييم عند الاتصال.':'Local offline assessment from a cached catalog. Current mission context and vitals are unavailable; no alert was sent. Reassess when connected.'}</p>}
    {result.level===3 && <div role="alert" className="p-4 rounded-xl border border-rose-400 bg-rose-950/50"><strong>{ar?'طوارئ — نبّه الطاقم والمسؤول الطبي الآن':'Emergency — alert crew and the medical officer now'}</strong><p>{result.triggeredAlertId?(ar?'تم حفظ التنبيه. تحقّق من وصوله مع المسؤول الطبي.':'Alert saved. Confirm receipt with the medical officer.'):(ar?'لم يُؤكَّد تسليم التنبيه. استخدم إجراء اتصالات الطوارئ فورًا.':'Alert delivery is not confirmed. Use the emergency communication procedure immediately.')}</p></div>}
    <p className="text-amber-200 text-xs">{ar?"دعم لاتخاذ القرار. اتبع خطة الرعاية الطبية للمهمة.":"Decision support. Follow your mission medical plan."}</p>
    <p className="text-sm text-slate-300">{ar?'تُقيّم شدة كل عرض والمتابعة لكل نمط على حدة. أعلى أولوية حالية: ':'Each symptom and pattern has its own follow-up priority. Highest current priority: '}{result.level===3?(ar?'حرجة':'Critical'):result.level===2?(ar?'مراجعة طبية':'Medical review'):(ar?'اعتيادية':'Routine')}</p>
    <div className="grid md:grid-cols-3 gap-2">{result.symptomFindings?.map(f=><div key={f.code} className={'p-3 rounded-xl border '+(f.level===3?'border-rose-400 bg-rose-950/40':f.level===2?'border-amber-400/50':'border-white/10')}><strong>{ar?symptomArabic[f.code] || f.name:f.name}</strong><p className="text-xs">{ar?({mild:'خفيفة',moderate:'متوسطة',severe:'شديدة'}[f.severity] || f.severity):f.severity} · {f.durationHours} {ar?'ساعة':'h'} · {f.level===3?(ar?'حرج':'Critical'):f.level===2?(ar?'مراجعة طبية':'Medical review'):(ar?'اعتيادي':'Routine')}</p></div>)}</div>
    <ol className="list-decimal ps-5 text-sm">{result.recommendedActions.map((action,i)=><li key={i}>{action}</li>)}</ol>
    {result.guidanceMode==='DELAYED' && <p className="text-amber-200 text-xs">{ar?'الاتصال متأخر؛ التأخر المسجّل بالثواني: ':'Delayed communication; recorded delay in seconds: '}{result.commsDelaySeconds}</p>}
    <p className="text-xs text-slate-300">{ar?"تعبّر النسب عن مدى تطابق الأعراض مع كل نمط، ولا تؤكّد وجود حالة طبية.":"Percentages describe how closely your symptoms match each pattern; they do not confirm a condition."}</p>
    <h3 className="font-bold">{ar?'الأنماط المحتملة ومتابعتها':'Potential patterns and their follow-up'}</h3>
    <div className="grid md:grid-cols-3 gap-3">{result.conditionMatches.slice(0,3).map(c=><article key={c.conditionCode} className={'rounded-2xl p-5 border '+(c.followUpLevel===3?'border-rose-400 bg-rose-950/50':c.followUpLevel===2?'border-amber-400/60 bg-amber-950/20':'border-cyan-400/40 bg-cyan-950/25')}>
      <h3 className="font-bold">{name(c.conditionCode,c.conditionName)}</h3>
      <div className="text-4xl font-extrabold my-3 tabular-nums" dir="ltr">{c.probabilityPercent ?? c.associationIndex}%</div>
      <p className="text-xs">{c.probabilityPercent!=null?(ar?"الاحتمال المقدّر":"Estimated probability"):(ar?"تطابق الأعراض":"Symptom match")}</p>
      <p className="mt-2 text-sm font-bold">{c.followUpLevel===3?(ar?'حرج — تنبيه الطبيب':'Critical — doctor alert'):c.followUpLevel===2?(ar?'يتطلب مراجعة طبية':'Medical review'):(ar?'متابعة اعتيادية':'Routine follow-up')}</p>
      <p className="text-xs text-slate-300 mt-2">{c.matchedSymptomCodes?.map(code=>ar?symptomArabic[code] || code:code.replaceAll('_',' ')).join(' · ')}</p>
    </article>)}</div>
    <h3 className="text-sm font-bold">{ar?'النتائج الأخرى وتفاصيل الحساب':'Other findings and calculation details'}</h3>
    {result.conditionMatches.map(c=><details key={c.conditionCode} className="p-3 rounded-xl border border-white/10"><summary>{name(c.conditionCode,c.conditionName)} · {c.associationIndex}% · {strength[c.strength]}</summary><dl className="grid grid-cols-2 gap-2 mt-2 text-xs">{Object.entries(c.breakdown).map(([key,value])=><div key={key}><dt>{ar?labels[key] || key:englishLabels[key] || key}</dt><dd>{typeof value==='number'?Number(value.toFixed(5)):ar?phases[value] || value:value}</dd></div>)}</dl></details>)}
    <aside className="p-3 rounded-xl border border-amber-400/20"><strong>{ar?'الإشعاع':'Radiation'}</strong><p className="text-sm">{ar?result.radiation.explanationAr:result.radiation.explanation}</p></aside>
    {result.knowledgeOnly.map(card=><aside key={card.code} className="p-3 border border-white/10 rounded-xl"><strong>{name(card.code,card.name)}</strong><p className="text-sm">{ar?card.explanationAr:card.explanation}</p></aside>)}
    {result.contextWarnings.length>0 && <p className="text-xs text-amber-200">{ar?'بعض بيانات المرحلة غير متاحة؛ لم تُفترض بيانات مهمة أو أحداث غير مسجّلة.':'Some phase data is unavailable; unrecorded mission data and events were not assumed.'}</p>}
    <p className="text-xs text-slate-300 border-t border-white/10 pt-3">{ar?result.footerAr:result.footer}</p>
  </section>;
}
