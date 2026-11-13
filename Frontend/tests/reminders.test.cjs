const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),ts=require('typescript');
const exportsForModule={};
vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(__dirname,'../src/services/reminderInbox.ts'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText,{exports:exportsForModule});
const {mergeReminderInbox,intervalInMinutes}=exportsForModule;
const reminder=(id,token,time='2026-11-13T12:00:00Z',owner=1)=>({recommendationId:id,astronautId:owner,token,sentAt:time,title:'Task',soundEnabled:true});
test('reminder intervals convert minutes and hours and reject invalid values',()=>{
 assert.equal(intervalInMinutes(5,'minutes'),5);assert.equal(intervalInMinutes(2,'hours'),120);assert.equal(intervalInMinutes(168,'hours'),10080);
 for(const n of [0,-1,1.5,NaN])assert.throws(()=>intervalInMinutes(n,'minutes'));
 assert.throws(()=>intervalInMinutes(169,'hours'));
});
test('live event and polling cannot duplicate a popup or leak another astronaut task',()=>{
 const r=reminder(1,'same');const list=mergeReminderInbox([r],[r,reminder(2,'other',undefined,2)],1);
 assert.equal(list.length,1);assert.equal(list[0].token,'same');
});
test('latest reminder replaces old one per task while a stale poll cannot replace newer data',()=>{
 const current=reminder(1,'new','2026-11-13T13:00:00Z');
 const stale=mergeReminderInbox([current],[reminder(1,'old')],1);assert.equal(stale[0].token,'new');
 const fresh=mergeReminderInbox(stale,[reminder(1,'next','2026-11-13T14:00:00Z')],1);assert.equal(fresh.length,1);assert.equal(fresh[0].token,'next');
});
test('a newly arriving task is never silently dropped from a full inbox',()=>{
 const current=Array.from({length:20},(_,i)=>reminder(i,'token-'+i));
 const list=mergeReminderInbox(current,[reminder(21,'manual-new')],1);
 assert.equal(list.length,21);assert.equal(list[20].token,'manual-new');
});
