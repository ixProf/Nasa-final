const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),ts=require('typescript');
const moduleExports={};
vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(__dirname,'../src/services/chatRouting.ts'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText,{exports:moduleExports});
const {chatRecipients}=moduleExports;
const crew=[{id:17,fullName:'New astronaut',astronautCode:'AST-017',isActive:true},{id:81,fullName:'Other astronaut',astronautCode:'AST-081',isActive:true}];
test('astronaut recipients contain only the medical team, regardless of the crew roster',()=>{
  const recipients=chatRecipients('Astronaut',crew);
  assert.equal(recipients.length,1);assert.equal(recipients[0].role,'Monitor');
  assert.equal(recipients[0].astronautId,undefined);
  assert.equal(chatRecipients('Astronaut',[]).length,1);
});
test('monitor recipients come from the real roster and include newly created astronauts',()=>{
  const recipients=chatRecipients('Monitor',[...crew,{id:99,fullName:'Inactive',astronautCode:'AST-099',isActive:false}]);
  assert.equal(recipients.length,2);assert.equal(recipients[0].astronautId,17);assert.equal(recipients[1].astronautId,81);
  assert.ok(recipients.every(rec=>rec.role==='Astronaut'));
  assert.equal(chatRecipients('Monitor',[]).length,0);
});
