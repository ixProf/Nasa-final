const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),ts=require('typescript');
const output={};
vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(__dirname,'../src/services/anatomy.ts'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText,{exports:output,URL});

test('organ tint follows the highest selected intensity and clears when symptoms are removed',()=>{
 const severity=output.organSymptomSeverity;
 assert.equal(severity('brain',[{code:'HEADACHE',severity:'mild'}]),'mild');
 const selected=[{code:'HEADACHE',severity:'mild'},{code:'DIZZINESS',severity:'moderate'},{code:'CONFUSION_DISORIENTATION',severity:'severe'}];
 assert.equal(severity('brain',selected),'severe');
 assert.equal(severity('brain',selected.slice(0,2)),'moderate');
 assert.equal(severity('brain',selected.slice(0,1)),'mild');
 assert.equal(severity('brain',[]),null);
 assert.equal(severity('eyes',selected),null);
 assert.equal(severity('brain',[...selected].reverse()),'severe');
 assert.equal(severity('brain',[{code:'HEADACHE'}]),'mild');
 assert.equal(output.ORGAN_SEVERITY_COLORS.mild,'#facc15');
 assert.equal(output.ORGAN_SEVERITY_COLORS.moderate,'#fb923c');
 assert.equal(output.ORGAN_SEVERITY_COLORS.severe,'#ef4444');
});

test('anatomy embed stays disabled without real configuration and encodes published model IDs',()=>{
 assert.equal(output.anatomyViewerUrl('','key'),null);assert.equal(output.anatomyViewerUrl('model',''),null);
 const url=new URL(output.anatomyViewerUrl('production/model&dk=untrusted','viewer-key'));
 assert.equal(url.origin,'https://human.biodigital.com');assert.equal(url.searchParams.get('id'),'production/model&dk=untrusted');
 assert.equal(url.searchParams.getAll('dk').length,1);assert.equal(url.searchParams.get('dk'),'viewer-key');
});
test('actual provider object IDs map to body regions without inventing model IDs',()=>{
 const map=output.parseAnatomyObjects('{"heart":["object-123"],"brain":[null,"brain-9"],"unknown":["x"]}');
 assert.equal(output.regionForObject('object-123','',map),'heart');
 assert.equal(output.regionForObject('maleAdult_Left_Lung_ID'),'lungs');
 assert.equal(output.regionForObject('actual-id','Left Retina'),'eyes');
 assert.equal(output.regionForObject('skin-id','Skin'),null);
 assert.equal(Object.keys(output.parseAnatomyObjects('invalid')).length,0);
 assert.equal(map.brain.length,1);assert.equal(map.unknown,undefined);
});
test('body-region symptom filters reference existing canonical codes and do not alter input selections',()=>{
 const seed=fs.readFileSync(path.join(__dirname,'../../Backend/Data/Seed/ModelBuilderExtensions.cs'),'utf8');
 const codes=new Set([...seed.matchAll(/new Symptom \{[^\n]*Code\s*=\s*"([A-Z_]+)"/g)].map(m=>m[1]));
 for(const symptoms of Object.values(output.REGION_SYMPTOMS))for(const code of symptoms)assert.ok(codes.has(code),code);
 assert.ok(output.REGION_SYMPTOMS.eyes.includes('SUDDEN_VISION_LOSS'));
 assert.ok(output.REGION_SYMPTOMS.heart.includes('CHEST_PAIN'));
 assert.ok(!output.REGION_SYMPTOMS.eyes.includes('PALPITATIONS'));
});
