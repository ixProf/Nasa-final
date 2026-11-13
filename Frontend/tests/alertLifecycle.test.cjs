const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),ts=require('typescript');
const exportsForModule={};
vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(__dirname,'../src/services/alertLifecycle.ts'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText,{exports:exportsForModule});
const {applyResolvedAlert,openAlertMessages}=exportsForModule;
const alert=(id,extra={})=>({id,astronautId:1,title:'Red-Flag Symptom Escalation',message:'Reported: chest pain',reason:'Automated escalation triggered by symptom guidance assessment detecting red-flag clinical signs.',severity:'Emergency',status:'Active',...extra});
test('banner deduplicates open warnings while showing the actual reported symptoms',()=>{
  const messages=openAlertMessages([alert(1),alert(2,{status:'Acknowledged'}),alert(3,{status:'Resolved',message:'Old symptoms'})]);
  assert.equal(messages.length,1);assert.equal(messages[0],'Reported: chest pain');
});
test('resolution clears only the same assessment issue and preserves history and unrelated alerts',()=>{
  const rows=[alert(1),alert(2,{status:'Acknowledged'}),alert(3,{astronautId:2}),alert(4,{message:'Different symptoms'}),alert(5,{status:'Resolved',actionTaken:'Earlier episode'})];
  const updated=applyResolvedAlert(rows,alert(1,{status:'Resolved',actionTaken:'Reviewed'}));
  assert.equal(updated.length,5);
  assert.equal(updated[0].status,'Resolved');assert.equal(updated[1].status,'Resolved');
  assert.equal(updated[2].status,'Active');assert.equal(updated[3].status,'Active');
  assert.equal(updated[4].actionTaken,'Earlier episode');assert.equal(rows[1].status,'Acknowledged');
});
test('acknowledgment retains the warning until resolution',()=>{
  assert.equal(openAlertMessages([alert(1,{status:'Acknowledged'})]).length,1);
  assert.equal(openAlertMessages([alert(1,{status:'Resolved'})]).length,0);
});
