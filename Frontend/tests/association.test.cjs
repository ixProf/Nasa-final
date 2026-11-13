const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),ts=require('typescript');
const exportsObject={};
vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(__dirname,'../src/services/offlineAssociation.ts'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText,{exports:exportsObject});
const source=fs.readFileSync(path.join(__dirname,'../../Backend/Data/Seed/ModelBuilderExtensions.cs'),'utf8');
const symptoms=[...source.matchAll(/new Symptom \{ Id=(\d+), Code="([^"]+)"[^\n]*IsRedFlag=(true|false)/g)].map(m=>({id:+m[1],code:m[2],isRedFlag:m[3]==='true'}));
const byId=new Map(symptoms.map(s=>[s.id,s]));
const links=[...source.matchAll(/new ConditionSymptom \{Id=(\d+),ConditionId=(\d+),SymptomId=(\d+),Weight=([^,]+)/g)].map(m=>{const parts=m[4].replaceAll('m','').split('/').map(Number);return {conditionId:+m[2],symptomCode:byId.get(+m[3]).code,weight:parts.length===2?parts[0]/parts[1]:parts[0]};});
const conditions=[...source.matchAll(/new SpaceCondition\s*\{([^}]+)\}/g)].map(m=>{const body=m[1],id=+body.match(/Id\s*=\s*(\d+)/)[1];return {code:body.match(/\bCode\s*=\s*"([^"]+)"/)[1],name:'Catalog pattern',hazardCode:body.match(/HazardCode\s*=\s*"([^"]+)"/)[1],confidenceCap:body.match(/ConfidenceCap\s*=\s*(\d+)/)?.[1]*1 || null,isMatchable:!body.includes('IsMatchable = false')&&!body.includes('IsMatchable=false'),associatedSymptoms:links.filter(l=>l.conditionId===id)};});
test('offline scores conform to server acceptance values using the actual seed catalog',()=>{
 assert.equal(symptoms.length,32);assert.equal(conditions.length,14);
 const r=exportsObject.offlineAssociation(['FATIGUE','DIFFICULTY_CONCENTRATING','POOR_SLEEP','DAYTIME_SLEEPINESS'],symptoms,conditions,'en');
 assert.equal(r.conditionMatches.find(c=>c.conditionCode==='SLEEP_DEFICIENCY').associationIndex,78.57);
 assert.equal(r.conditionMatches.find(c=>c.conditionCode==='CLOSED_ENVIRONMENT_AIR_QUALITY').associationIndex,20.03);
 assert.equal(r.hazardScores[0].hazard,'ISOLATION');assert.equal(r.radiation.status,'withheld');
});
test('offline red flags and urgent symptoms retain escalation, without claiming alert delivery',()=>{
 const evaluate=codes=>exportsObject.offlineAssociation(codes,symptoms,conditions,'ar');
 for(const s of symptoms.filter(s=>s.isRedFlag)){const r=evaluate([s.code]);assert.equal(r.level,3);assert.equal(r.triggeredAlertId,undefined);assert.equal(r.offline,true);}
 assert.equal(evaluate(['PALPITATIONS']).level,2);assert.equal(evaluate(['LIGHT_FLASHES']).level,1);
 assert.equal(evaluate(['NECK_FACE_SWELLING']).knowledgeOnly.length,1);
 assert.equal(evaluate(['CHEST_PAIN']).conditionMatches.length,0);
 assert.throws(()=>evaluate(['UNRECOGNIZED']),/incomplete/);
});
test('offline unknown timing uses conservative gates and retains severe-symptom triage',()=>{
 const r=exportsObject.offlineAssociation(['NAUSEA_VOMITING','DIZZINESS','SWEATING','HEADACHE'],symptoms,conditions,'en','severe');
 assert.equal(r.conditionMatches.find(c=>c.conditionCode==='SPACE_MOTION_SICKNESS').associationIndex,39.66);
 assert.equal(r.level,2);assert.equal(r.guidanceMode,'UNAVAILABLE');
});
