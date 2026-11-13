import type { BodyRegionId } from '../data/bodyRegions';

export type AnatomyObjectMap = Partial<Record<BodyRegionId,string[]>>;
const regions:BodyRegionId[]=['brain','heart','lungs','eyes','stomach'];
export function parseAnatomyObjects(value?:string):AnatomyObjectMap {
  if(!value)return {};
  try {const parsed=JSON.parse(value);return Object.fromEntries(regions.filter(r=>Array.isArray(parsed[r])).map(r=>[r,parsed[r].filter((v:unknown)=>typeof v==='string'&&v.length>0&&v.length<256)]));}catch{return {};}
}
export function anatomyViewerUrl(modelId:string,key:string):string|null {
  if(!modelId.trim()||!key.trim())return null;
  const url=new URL('https://human.biodigital.com/viewer/');
  url.searchParams.set('id',modelId.trim());url.searchParams.set('dk',key.trim());return url.toString();
}
export function regionForObject(id:string,name='',mapping:AnatomyObjectMap={}):BodyRegionId|null {
  for(const region of regions)if(mapping[region]?.includes(id))return region;
  const label=(id+' '+name).replace(/[_-]/g,' ').toLowerCase();
  if(/\b(brain|cerebrum|cerebellum|brainstem)\b/.test(label))return 'brain';
  if(/\b(heart|cardiac|myocardium)\b/.test(label))return 'heart';
  if(/\b(lung|lungs|bronchus|bronchi|trachea)\b/.test(label))return 'lungs';
  if(/\b(eye|eyes|eyeball|retina|cornea)\b/.test(label))return 'eyes';
  if(/\b(stomach|gastric|intestine|pancreas)\b/.test(label))return 'stomach';
  return null;
}
export const REGION_SYMPTOMS:Record<BodyRegionId,string[]>={
  brain:['DIZZINESS','FAINTING','HEADACHE','SEVERE_SUDDEN_HEADACHE','CONFUSION_DISORIENTATION','SEIZURE','DIFFICULTY_CONCENTRATING','FATIGUE','POOR_SLEEP','DAYTIME_SLEEPINESS'],
  heart:['CHEST_PAIN','PALPITATIONS','FAINTING','DIZZINESS','NECK_FACE_SWELLING','SWEATING'],
  lungs:['DIFFICULTY_BREATHING','RESPIRATORY_IRRITATION','NASAL_CONGESTION','CHEST_PAIN'],
  eyes:['BLURRED_VISION','NEAR_VISION_CHANGE','SUDDEN_VISION_LOSS','LIGHT_FLASHES','EYE_IRRITATION'],
  stomach:['NAUSEA_VOMITING','LOSS_OF_APPETITE','FLANK_PAIN_HEMATURIA','DIZZINESS','SWEATING','TREMOR']
};

export type OrganSeverity = 'mild' | 'moderate' | 'severe';
export const ORGAN_SEVERITY_COLORS: Record<OrganSeverity, string> = {
  mild: '#facc15', moderate: '#fb923c', severe: '#ef4444',
};

/** Highest selected intensity for this region; no selection means no symptom tint. */
export function organSymptomSeverity(region: BodyRegionId, symptoms: ReadonlyArray<{code:string;severity?:string}>): OrganSeverity | null {
  let highest: OrganSeverity | null = null;
  for (const symptom of symptoms) {
    if (!REGION_SYMPTOMS[region].includes(symptom.code)) continue;
    if (symptom.severity === 'severe') return 'severe';
    if (symptom.severity === 'moderate') highest = 'moderate';
    else if (!highest) highest = 'mild';
  }
  return highest;
}
