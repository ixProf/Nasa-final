const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const ts = require('typescript');

function client(fetch, language='en') {
  const values = new Map();
  values.set('nasa_lang',language);
  const errors = [];
  const exports = {};
  const contract = {};
  const submissions = {};
  const offline = {};
  vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(__dirname,'../src/services/offlineMeasurements.ts'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText,{exports:offline});
  vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(__dirname,'../src/services/submissions.ts'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText,{exports:submissions,crypto:globalThis.crypto});
  vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(__dirname,'../src/services/contract.ts'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText,{exports:contract});
  const source = ts.transpileModule(fs.readFileSync(path.join(__dirname,'../src/services/api.ts'),'utf8'),
    {compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
  vm.runInNewContext(source, {
    exports, require: name => name === './config' ? {API_BASE:'/api'} : name === './contract' ? contract : name === './submissions' ? submissions : name === './offlineMeasurements' ? offline : {reportError:e=>errors.push(e.message)},
    localStorage:{getItem:k=>values.get(k)||null,setItem:(k,v)=>values.set(k,v),removeItem:k=>values.delete(k)},
    window:{dispatchEvent(){}}, Event, CustomEvent, fetch, crypto:globalThis.crypto, URLSearchParams, atob,
  });
  return {api:exports.api,errors};
}
const json = (data,status=200) => new Response(JSON.stringify(data),{status,headers:{'content-type':'application/json'}});

test('database failures display actionable safe messages and a request ID in either language',async()=>{
  for(const lang of ['en','ar']){
    const {api}=client(async()=>json({code:'DATABASE_UPDATE_REQUIRED',message:'Run database migration.',messageAr:'يجب تحديث قاعدة البيانات.',traceId:'request-42'},503),lang);
    await assert.rejects(api.getMissions(),error=>error.message.includes(lang==='ar'?'يجب تحديث قاعدة البيانات.':'Run database migration.')&&error.message.includes('request-42')&&error.status===503);
  }
});
test('unexpected server exceptions never expose database details',async()=>{
  const {api}=client(async()=>json({detail:'password=secret; table internal',traceId:'reference-1'},500));
  await assert.rejects(api.getMissions(),e=>!e.message.includes('secret')&&e.message.includes('reference-1'));
});

const astronautToken=id=>'header.'+Buffer.from(JSON.stringify({astronaut_id:String(id)})).toString('base64url')+'.signature';
test('offline measurement retains its GUID and account scope until the server confirms sync',async()=>{
  let online=false, uploaded;
  const {api}=client(async(url,options)=>{
    if(!online)throw new TypeError('Failed to fetch');
    uploaded=JSON.parse(options.body);
    return json({itemResults:uploaded.vitalSignReadings.map(row=>({clientGeneratedId:row.clientGeneratedId,itemType:'VitalSign',status:0}))});
  });
  api.setTokens(astronautToken(1),'refresh');
  await assert.rejects(api.recordVital({vitalSignTypeCode:'HR',value:75}),e=>e.queued===true);
  const [queued]=api.getOfflineMeasurements();assert.ok(queued.clientGeneratedId);assert.equal(queued.payload.value,75);
  api.setTokens(astronautToken(2),'refresh');assert.equal(api.getOfflineMeasurements().length,0);
  api.setTokens(astronautToken(1),'refresh');online=true;
  const result=await api.syncPendingMeasurements();
  assert.equal(result.synced,1);assert.equal(uploaded.vitalSignReadings[0].clientGeneratedId,queued.clientGeneratedId);
  assert.equal(api.getOfflineMeasurements().length,0);
});
test('partial sync leaves missing results pending and shows rejected records as rejected',async()=>{
  let online=false;
  const {api}=client(async(url,options)=>{
    if(!online)throw new TypeError('Failed to fetch');
    const rows=JSON.parse(options.body).moodEntries;
    return json({itemResults:[{clientGeneratedId:rows[0].clientGeneratedId,itemType:'Mood',status:2,reason:'Outside permitted time window.'}]});
  });
  api.setTokens(astronautToken(1),'refresh');
  for(const moodScore of [3,4])await assert.rejects(api.recordMood({moodScore,moodScaleMaximum:5}));
  online=true;await api.syncPendingMeasurements();
  const rows=api.getOfflineMeasurements();assert.equal(rows.length,2);assert.equal(rows[0].syncStatus,'Rejected');assert.equal(rows[1].syncStatus,'Pending');
});
test('cached guides remain readable offline but a server error is not hidden by cached data',async()=>{
  let mode='online';const {api}=client(async()=>{if(mode==='offline')throw new TypeError('Failed to fetch');return mode==='failure'?json({code:'DATABASE_UPDATE_REQUIRED',message:'Migrate first.'},503):json([{code:'HR',normalRange:'60–100'}]);});
  await api.getGuides();mode='offline';assert.equal((await api.getGuides())[0].code,'HR');assert.ok(api.cachedGuideDate);
  mode='failure';await assert.rejects(api.getGuides(),/Migrate first/);
});

test('simultaneous 401s share one refresh and retry with the rotated token', async()=>{
  let refreshes=0;
  const {api}=client(async(url,options)=>{
    if(url==='/api/auth/refresh') {refreshes++; await new Promise(r=>setTimeout(r,10)); return json({token:'new',refreshToken:'next'});}
    return options.headers.Authorization==='Bearer new' ? json({ok:true}) : json({},401);
  });
  api.setTokens('old','refresh');
  await Promise.all([api.getDashboardMe(),api.getCurrentUser(),api.getMissions()]);
  assert.equal(refreshes,1);
  assert.equal(api.getAccessToken(),'new');
});

test('invalid login never retries a stored refresh token',async()=>{
  let calls=0; const {api}=client(async()=>{calls++;return json({message:'Invalid credentials'},401);});
  api.setTokens('old','refresh');
  await assert.rejects(api.login('user','wrong'),/Invalid credentials/);
  assert.equal(calls,1);
});

test('validation and transport failures remain failures and are visible',async()=>{
  const {api,errors}=client(async()=>json({errors:{value:['Value is required.']}},400));
  await assert.rejects(api.recordVital({vitalSignTypeCode:'HR',value:0}),/Value is required/);
  assert.equal(errors.length,1);
  const broken=client(async()=>{throw new TypeError('Network unavailable');});
  await assert.rejects(broken.api.getMissions(),/Network unavailable/);
  assert.equal(broken.errors[0],'Network unavailable');
});

test('HTML fallback is rejected as a proxy configuration error',async()=>{
  const {api}=client(async()=>new Response('<html></html>',{headers:{'content-type':'text/html'}}));
  await assert.rejects(api.getMissions(),/reverse proxy/);
});

test('logout clears the local token even if the backend is unreachable',async()=>{
  const {api}=client(async()=>{throw new Error('Offline');});
  api.setTokens('old','refresh');
  await assert.rejects(api.logout(),/Offline/);
  assert.equal(api.getAccessToken(),null);
});

test('logout during refresh does not resurrect the previous session',async()=>{
  let release;
  const {api}=client(async(url)=> url.endsWith('/refresh') ? new Promise(resolve=>{release=()=>resolve(json({token:'new',refreshToken:'next'}));}) : json({},401));
  api.setTokens('old','refresh');
  const request=api.getDashboardMe();
  while(!release) await new Promise(r=>setImmediate(r));
  api.clearTokens(); release();
  await assert.rejects(request);
  assert.equal(api.getAccessToken(),null);
});


test('hosted numeric enum contract is converted in both directions',async()=>{
 let payload;
 const {api}=client(async(url,options)=>{payload=JSON.parse(options.body);return json({id:42,vitalSignTypeCode:'HR',value:72,status:2,bodyPosition:1,activityContext:1});});
 const saved=await api.recordVital({vitalSignTypeCode:'HR',value:72,bodyPosition:'MicrogravityFloating',activityContext:'Resting'});
 assert.equal(payload.bodyPosition,1); assert.equal(payload.activityContext,1);
 assert.equal(saved.status,'MedicalAttention');assert.equal(saved.bodyPosition,'MicrogravityFloating');
});

test('history loads every page in the requested period',async()=>{
  let calls=0;
  const {api}=client(async(url)=>{
    calls++;
    const page=Number(new URL(url,'https://frontend.example').searchParams.get('pageNumber'));
    assert.match(url,/fromUtc=/);
    return json({items:Array.from({length:page===1?100:23},(_,i)=>({id:(page-1)*100+i,status:1})),totalCount:123});
  });
  const history=await api.getVitalsForPeriod('2026-10-30T00:00:00Z');
  assert.equal(history.length,123);
  assert.equal(calls,2);
});

test('production defaults to MonsterASP while development uses the proxy',()=>{
  const source=fs.readFileSync(path.join(__dirname,'../src/services/config.ts'),'utf8').replace('import.meta','importMeta');
  const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
  for(const dev of [true,false]) {
    const exports={};vm.runInNewContext(code,{exports,importMeta:{env:{DEV:dev}}});
    assert.equal(exports.API_BASE,dev?'/api':'https://astrohealth.runasp.net/api');
    assert.equal(exports.HUB_BASE,dev?'/hubs':'https://astrohealth.runasp.net/hubs');
  }
});


test('rapid identical reading submissions send one request and share the result',async()=>{
 let calls=0;
 const {api}=client(async(url,options)=>{calls++;await new Promise(r=>setTimeout(r,10));return json({id:44,...JSON.parse(options.body),status:1});});
 const payload={vitalSignTypeCode:'HR',value:77};
 const [first,second]=await Promise.all([api.recordVital(payload),api.recordVital(payload)]);
 assert.equal(calls,1);assert.equal(first.id,second.id);assert.equal(first.clientGeneratedId,second.clientGeneratedId);
 await api.recordVital(payload);assert.equal(calls,2);
});

test('retry after an uncertain write keeps the same idempotency key and timestamp',async()=>{
 const bodies=[];
 const {api}=client(async(url,options)=>{bodies.push(JSON.parse(options.body));if(bodies.length===1)throw new Error('Connection lost');return json({id:3,...bodies[1]});});
 const payload={vitalSignTypeCode:'HR',value:79};
 await assert.rejects(api.recordVital(payload));
 await api.recordVital(payload);
 assert.equal(bodies[0].clientGeneratedId,bodies[1].clientGeneratedId);
 assert.equal(bodies[0].recordedAt,bodies[1].recordedAt);
});

test('concurrent dashboard reads reuse a request instead of duplicating it',async()=>{
 let calls=0;const {api}=client(async()=>{calls++;await new Promise(r=>setTimeout(r,5));return json({fullName:'Crew'});});
 await Promise.all([api.getDashboardMe(),api.getDashboardMe()]);assert.equal(calls,1);
 await api.getDashboardMe();assert.equal(calls,2);
});
