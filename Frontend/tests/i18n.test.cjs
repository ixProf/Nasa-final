const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
const ts=require('typescript');
const cache=new Map();
function load(file){
 const absolute=path.resolve(__dirname,'../src/i18n',file);
 if(cache.has(absolute))return cache.get(absolute);
 if(absolute.endsWith('.json'))return JSON.parse(fs.readFileSync(absolute,'utf8'));
 const exports={};cache.set(absolute,exports);
 const source=ts.transpileModule(fs.readFileSync(absolute+'.ts','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022,esModuleInterop:true}}).outputText;
 vm.runInNewContext(source,{exports,require:name=>load(name),console});return exports;
}
const {symptomArabic,conditionArabic}=load('clinical');
const {translateUiText}=load('uiText');
test('all 18 server symptom codes have Arabic names',()=>{
 const seed=fs.readFileSync(path.join(__dirname,'../../Backend/Data/Seed/ModelBuilderExtensions.cs'),'utf8');
 const codes=[...seed.matchAll(/new Symptom \{[^\n]*Code = "([A-Z_]+)"/g)].map(m=>m[1]);
 assert.equal(codes.length,18);
 for(const code of codes){assert.ok(symptomArabic[code],code);assert.doesNotMatch(symptomArabic[code],/[A-Za-z]/);}
});
test('all 12 spaceflight conditions have Arabic names, descriptions and guidance',()=>{
 assert.equal(Object.keys(conditionArabic).length,12);
 const seed=fs.readFileSync(path.join(__dirname,'../../Backend/Data/Seed/ModelBuilderExtensions.cs'),'utf8');
 const codes=[...seed.matchAll(/new SpaceCondition\s*\{[^}]*?Code = "([A-Z_]+)"/g)].map(m=>m[1]);
 assert.equal(codes.length,12);for(const code of codes)assert.ok(conditionArabic[code],code);
 for(const record of Object.values(conditionArabic))for(const value of Object.values(record)){assert.ok(value.length>5);assert.doesNotMatch(value,/[A-Za-z]/);}
});
test('assessment reasons, actions, severity and units translate when Arabic is selected',()=>{
 const text='Level 1: Reported mild symptoms: Headache. Symptoms have lasted 25 hours.';
 assert.doesNotMatch(translateUiText(text,'ar'),/[A-Za-z]/);
 assert.equal(translateUiText(text,'en'),text);
 assert.equal(translateUiText('Severity','ar'),'شدة الأعراض');
 assert.equal(translateUiText('mmHg','ar'),'ملم زئبق');
 assert.doesNotMatch(translateUiText('Re-measure relevant indicators, log symptoms and contact the medical officer for review.','ar'),/[A-Za-z]/);
});
test('Arabic switch does not change identifiers, email addresses or URLs',()=>{
 for(const value of ['https://astrohealth.runasp.net','crew@mission.space','AST-004'])assert.equal(translateUiText(value,'ar'),value);
});


test('Arabic interface branches do not retain untranslated English annotations',()=>{
 const misses=[];
 function walk(dir){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){
   const file=path.join(dir,entry.name);
   if(entry.isDirectory())walk(file);
   else if(file.endsWith('.tsx')){
     const source=ts.createSourceFile(file,fs.readFileSync(file,'utf8'),ts.ScriptTarget.Latest,true,ts.ScriptKind.TSX);
     function visit(node){
       if(ts.isConditionalExpression(node)&&ts.isStringLiteralLike(node.whenTrue)&&/[\u0600-\u06ff]/.test(node.whenTrue.text)){
         const translated=translateUiText(node.whenTrue.text,'ar');if(/[A-Za-z]/.test(translated))misses.push(translated);
       }
       ts.forEachChild(node,visit);
     }
     visit(source);
   }
 }}
 walk(path.join(__dirname,'../src/components'));
 assert.deepEqual(misses,[]);
});
