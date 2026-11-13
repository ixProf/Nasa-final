// Conservative local fallback using the versioned server catalog cached on this device.
// Missing mission data never creates boosts, radiation events or confirmed alerts.
export function offlineAssociation(codes:string[],symptoms:any[],conditions:any[],locale:string,severity='mild',durationHours=0,details:Array<{code:string;severity:string;durationHours:number}>=[]) {
  const selected=new Set(codes.map(c=>c.trim().toUpperCase()));
  const known=new Map(symptoms.map(s=>[s.code,s]));
  if([...selected].some(c=>!known.has(c)))throw new Error('The offline symptom catalog is incomplete. Reconnect before assessment.');
  const red=[...selected].filter(code=>known.get(code).isRedFlag);
  const urgent=selected.has('PALPITATIONS')||selected.has('NECK_FACE_SWELLING');
  const strength=(index:number)=>index<30?'weak':index<=60?'moderate':'strong';
  const matches=conditions.filter(c=>c.isMatchable && typeof c.confidenceCap==='number').flatMap(c=>{
    const links=(c.associatedSymptoms || []).filter((s:any)=>s.weight>0);
    const hit=links.filter((s:any)=>selected.has(s.symptomCode));if(!hit.length)return [];
    const total=links.reduce((sum:number,s:any)=>sum+s.weight,0);if(!total)return [];
    const weight=hit.reduce((sum:number,s:any)=>sum+s.weight,0),coverage=weight/total,evidence=Math.min(1,weight);
    const phased=['SPACE_MOTION_SICKNESS','ORTHOSTATIC_INTOLERANCE'].includes(c.code),gate=phased?0.5:1;
    const raw=coverage*evidence*100*gate,cap=Math.max(0,Math.min(99,c.confidenceCap)),index=Math.round((Math.min(cap,raw)+Number.EPSILON)*100)/100;
    return [{conditionCode:c.code,conditionName:c.name,hazardCode:c.hazardCode,associationIndex:index,strength:strength(index),matchedSymptomCodes:hit.map((s:any)=>s.symptomCode),
      explanation:phased?'Phase timing is unavailable; a conservative phase factor is applied.':'Computed from cached symptom weights; current mission context is unavailable.',
      explanationAr:phased?'توقيت المرحلة غير متاح؛ طُبّق معامل مرحلة تحفظي.':'حُسب من أوزان الأعراض المحفوظة؛ سياق المهمة الحالي غير متاح.',
      breakdown:{matchedWeight:weight,totalWeight:total,coverage,evidence,phaseGate:gate,vitalBoost:0,contextBoost:0,cap,uncappedIndex:raw,phaseState:phased?'unknown':'not_applicable'}}];
  }).sort((a,b)=>b.associationIndex-a.associationIndex || (a.conditionCode<b.conditionCode?-1:1));
  const hazards=new Map<string,any>();for(const m of matches)if(!hazards.has(m.hazardCode))hazards.set(m.hazardCode,{hazard:m.hazardCode,index:m.associationIndex,strength:m.strength,explanation:m.explanation,explanationAr:m.explanationAr,conditionCode:m.conditionCode});
  const cards=new Set<string>();if(selected.has('NECK_FACE_SWELLING'))cards.add('VENOUS_THROMBOEMBOLISM');if(selected.has('MUSCULOSKELETAL_PAIN'))cards.add('MUSCLE_ATROPHY');if(selected.has('FLANK_PAIN_HEMATURIA')){cards.add('RENAL_STONE_RISK');cards.add('BONE_LOSS');}
  const ar=locale==='ar';
  const symptomFindings=[...selected].map(code=>{
    const detail=details.find(d=>d.code===code),intensity=detail?.severity || (details.length?'mild':severity),hours=detail?.durationHours ?? (details.length?0:durationHours);
    const score=(intensity==='severe'?3:intensity==='moderate'?2:1)+(hours>24?1:0);
    return {code,name:known.get(code).name,severity:intensity,durationHours:hours,level:red.includes(code)?3:['PALPITATIONS','NECK_FACE_SWELLING'].includes(code)||score>=3?2:1};
  });
  const classified=matches.map(c=>({...c,probabilityPercent:null,probabilityStatus:'offline',followUpLevel:Math.max(1,...symptomFindings.filter(f=>c.matchedSymptomCodes.includes(f.code)).map(f=>f.level))})).sort((a,b)=>b.followUpLevel-a.followUpLevel || b.associationIndex-a.associationIndex);
  return {engineVersion:'2.0-demo',offline:true,level:Math.max(1,...symptomFindings.map(f=>f.level)),symptomFindings,redFlags:red,conditionMatches:classified,hazardScores:[...hazards.values()].sort((a,b)=>b.index-a.index || (a.hazard<b.hazard?-1:1)),
    knowledgeOnly:conditions.filter(c=>!c.isMatchable && cards.has(c.code)).map(c=>({code:c.code,name:c.name,explanation:'Cannot be assessed from symptoms; requires a specialized examination within the mission plan.',explanationAr:'لا يمكن تقييمها من الأعراض؛ تحتاج فحصًا متخصصًا ضمن خطة المهمة.'})),
    radiation:{status:'withheld',explanation:'Cannot be estimated from symptoms alone. Current mission events are unavailable offline.',explanationAr:'لا يمكن تقدير الإشعاع من الأعراض وحدها. أحداث المهمة الحالية غير متاحة دون اتصال.'},guidanceMode:'UNAVAILABLE',commsDelaySeconds:0,
    recommendedActions:red.length?[ar?'نبّه الطاقم والمسؤول الطبي الآن واتبع بروتوكول طوارئ المهمة.':'Alert crew and the medical officer now and follow the mission emergency protocol.']:[ar?'سجّل الأعراض وتواصل مع المسؤول الطبي وفق إجراء المهمة؛ لا يمكن تأكيد الاتصال دون شبكة.':'Log your symptoms and contact the medical officer through the mission procedure; connectivity cannot be confirmed offline.'],
    footer:'Guidance index based on written rules — not a diagnosis and not a medical probability. Contact your medical officer if you are concerned.',footerAr:'مؤشر استرشادي مبني على قواعد مكتوبة، وليس تشخيصًا ولا احتمالًا طبيًا. تواصل مع المسؤول الطبي عند القلق.',contextWarnings:['OFFLINE_CONTEXT_UNAVAILABLE']};
}
