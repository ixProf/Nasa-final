import { lazy, Suspense, useEffect, useId, useRef, useState } from 'react';
import { useLanguage } from '../../context/LanguageContext';
import type { BodyRegionId } from '../../data/bodyRegions';
import { anatomyViewerUrl, parseAnatomyObjects, regionForObject } from '../../services/anatomy';

const LocalBody=lazy(()=>import('./BodyMap3D').then(m=>({default:m.BodyMap3D})));
const env=(import.meta as ImportMeta & {env:Record<string,string>}).env;
const viewerUrl=anatomyViewerUrl(env.VITE_BIODIGITAL_MODEL_ID || '',env.VITE_BIODIGITAL_DEVELOPER_KEY || '');
const objectMapping=parseAnatomyObjects(env.VITE_BIODIGITAL_REGION_OBJECTS);
interface HumanViewer {
  on(event:string,handler:(data:any,...args:any[])=>void):void;
  send(event:string,...args:any[]):void;
  destroy?():void;
}
declare global {interface Window {HumanAPI?:new(id:string)=>HumanViewer;}}
let sdkPromise:Promise<void>|null=null;
function loadSdk():Promise<void> {
  if(window.HumanAPI)return Promise.resolve();
  if(sdkPromise)return sdkPromise;
  sdkPromise=new Promise<void>((resolve,reject)=>{
    const script=document.createElement('script');script.src='https://developer.biodigital.com/builds/api/human-api-3.0.0.min.js';script.async=true;
    const timer=window.setTimeout(()=>{script.remove();reject(new Error('Anatomy viewer timed out'));},12000);
    script.onload=()=>{clearTimeout(timer);if(window.HumanAPI)resolve();else reject(new Error('Anatomy viewer unavailable'));};
    script.onerror=()=>{clearTimeout(timer);script.remove();reject(new Error('Anatomy viewer unavailable'));};document.head.appendChild(script);
  }).catch(error=>{sdkPromise=null;throw error;});return sdkPromise;
}

export function AnatomyViewer({selectedRegionId,onRegionSelect}:{selectedRegionId:BodyRegionId|null;onRegionSelect:(region:BodyRegionId)=>void}) {
  const {language}=useLanguage();const ar=language==='ar';
  const [detailed,setDetailed]=useState(false),[ready,setReady]=useState(false),[failed,setFailed]=useState(false);
  const frameId='anatomy-'+useId().replace(/:/g,'');
  const human=useRef<HumanViewer|null>(null),selection=useRef(onRegionSelect),objects=useRef<Record<string,{name?:string}>>({});selection.current=onRegionSelect;
  useEffect(()=>{
    if(!detailed || !viewerUrl)return;
    let active=true;setReady(false);setFailed(false);
    const fallback=()=>{if(active){setDetailed(false);setFailed(true);}};
    const timeout=window.setTimeout(fallback,20000);
    void loadSdk().then(()=>{
      if(!active || !window.HumanAPI)return;
      const api=new window.HumanAPI(frameId);human.current=api;
      api.on('human.ready',()=>{
        if(!active)return;clearTimeout(timeout);setReady(true);
        api.send('scene.info',(data:any,objectMap?:any)=>{if(!active)return;const candidate=objectMap || data?.objects || data?.objectMap || data;objects.current=candidate && typeof candidate==='object' && !Array.isArray(candidate)?candidate:{};});
      });
      api.on('scene.picked',(event:{objectId?:string})=>{
        if(!active || !event?.objectId)return;
        const region=regionForObject(event.objectId,objects.current[event.objectId]?.name || '',objectMapping);
        if(region)selection.current(region);
      });
      api.on('human.error',fallback);
    }).catch(fallback);
    return()=>{active=false;clearTimeout(timeout);try{human.current?.destroy?.();}finally{human.current=null;objects.current={};}};
  },[detailed,frameId]);
  useEffect(()=>{
    if(!ready || !human.current)return;
    const ids=selectedRegionId?(objectMapping[selectedRegionId] || Object.keys(objects.current).filter(id=>regionForObject(id,objects.current[id]?.name || '',objectMapping)===selectedRegionId)):[];
    const selectionMap:Record<string,boolean>={replace:true};for(const id of ids)selectionMap[id]=true;
    human.current.send('scene.selectObjects',selectionMap);
  },[selectedRegionId,ready]);
  const loading=<div className="absolute inset-0 flex items-center justify-center text-sm text-slate-300" role="status">{ar?'جارٍ تحميل خريطة الجسم…':'Loading body map…'}</div>;
  return <div className="relative h-full w-full">
    {viewerUrl&&<div className="absolute top-2 start-2 z-20 flex gap-1 rounded-xl border border-white/15 bg-slate-950/90 p-1 text-xs"><button type="button" aria-pressed={!detailed} onClick={()=>setDetailed(false)} className="rounded-lg px-3 py-2 text-cyan-200">{ar?'الخريطة التفاعلية':'Interactive map'}</button><button type="button" aria-pressed={detailed} onClick={()=>setDetailed(true)} className="rounded-lg px-3 py-2 text-cyan-200">{ar?'التشريح التفصيلي':'Detailed anatomy'}</button></div>}
    {detailed&&viewerUrl?<><iframe id={frameId} src={viewerUrl} title={ar?'التشريح التفاعلي من بيوديجيتال':'BioDigital interactive anatomy'} className="w-full h-full border-0" allow="fullscreen" allowFullScreen referrerPolicy="strict-origin-when-cross-origin" onError={()=>{setFailed(true);setDetailed(false);}}/>{!ready&&loading}</>:<Suspense fallback={loading}><LocalBody selectedRegionId={selectedRegionId} onRegionSelect={onRegionSelect} height="100%" showParticles={true}/></Suspense>}
    {failed&&<p role="status" className="absolute bottom-2 start-2 end-2 rounded-lg bg-slate-950/90 p-2 text-[11px] text-slate-300">{ar?'التشريح التفصيلي غير متاح حاليًا. يمكنك استخدام الخريطة التفاعلية.':'Detailed anatomy is unavailable. The interactive map is available.'}</p>}
  </div>;
}
