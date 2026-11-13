const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),ts=require('typescript');
const output={},storage=new Map();
vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(__dirname,'../src/services/medicalReport.ts'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText,{exports:output,sessionStorage:{getItem:k=>storage.get(k),setItem:(k,v)=>storage.set(k,v)}});

test('report follows pagination even when a server caps the requested page size',async()=>{
 const calls=[];const rows=await output.collectReportPages(async page=>{calls.push(page);return {items:page===1?[1,2]:[3],totalCount:3};});
 assert.deepEqual(Array.from(rows),[1,2,3]);assert.deepEqual(calls,[1,2]);
});
test('report refuses to present a partial history as complete',async()=>{
 await assert.rejects(output.collectReportPages(async page=>({items:page===1?[1]:[],totalCount:2})),/Incomplete/);
 await assert.rejects(output.collectReportPages(async page=>{if(page===2)throw Error('server unavailable');return {items:[1],hasNextPage:true};}),/server unavailable/);
});
test('symptom session histories are isolated by account and astronaut',()=>{
 storage.clear();const entry={id:'assessment',recordedAt:'2026-11-13T00:00:00Z',symptoms:[{code:'HEADACHE',severity:'mild',durationHours:2}],result:{level:1}};
 assert.equal(output.saveReportAssessment('astro.vance',1,entry),true);
 assert.equal(output.readReportAssessments('astro.vance',1)[0].symptoms[0].durationHours,2);
 assert.equal(output.readReportAssessments('astro.lee',1).length,0);
 assert.equal(output.readReportAssessments('astro.vance',2).length,0);
});
