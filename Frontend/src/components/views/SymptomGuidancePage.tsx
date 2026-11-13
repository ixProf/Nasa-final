import { AsyncButton } from "../AsyncButton";
import { symptomArabic, conditionArabic } from '../../i18n/clinical';
import { translateUiText } from '../../i18n/uiText';
import { useUiText } from "../../i18n/useUiText";
import { reportError } from '../../services/errors';
import React, { useState, useEffect } from 'react';
import {
  Stethoscope,
  CheckCircle2,
  Sparkles,
  BookOpen,
  Search,
  AlertTriangle,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { api } from '../../services/api';
import { BodyMapPanel } from '../body3d/BodyMapPanel';

interface SymptomItem {
  id: number;
  code: string;
  name: string;
  nameAr: string;
  category: string;
  isRedFlag?: boolean;
}

interface ConditionGuideItem {
  id: number;
  code: string;
  name: string;
  nameAr: string;
  description: string;
  descriptionAr: string;
  selfCareGuidance: string;
  selfCareGuidanceAr: string;
  whenToContactMonitor: string;
  whenToContactMonitorAr: string;
}

const defaultSymptoms: SymptomItem[] = [
  { id: 1, code: 'NAUSEA', name: 'Nausea', nameAr: 'غثيان دوار الفضاء', category: 'Vestibular' },
  { id: 2, code: 'DIZZINESS', name: 'Dizziness / Vertigo', nameAr: 'دوار وعدم اتزان', category: 'Vestibular' },
  { id: 3, code: 'HEADACHE', name: 'Headache', nameAr: 'صداع', category: 'Neurological' },
  { id: 4, code: 'VOMITING', name: 'Vomiting', nameAr: 'قيء', category: 'Gastrointestinal' },
  { id: 5, code: 'NASAL_CONGESTION', name: 'Nasal Congestion', nameAr: 'احتقان الأنف والجيوب', category: 'ENT' },
  { id: 6, code: 'FACIAL_PRESSURE', name: 'Facial Fullness / Pressure', nameAr: 'ضغط وامتلاء الوجه', category: 'Cardiovascular' },
  { id: 7, code: 'FATIGUE', name: 'Fatigue', nameAr: 'إرهاق عام', category: 'General' },
  { id: 8, code: 'POOR_CONCENTRATION', name: 'Poor Concentration', nameAr: 'ضعف التركيز الذهني', category: 'Cognitive' },
  { id: 9, code: 'IRRITABILITY', name: 'Irritability', nameAr: 'توتر وسرعة انفعال', category: 'Psychological' },
  { id: 10, code: 'DAYTIME_DROWSINESS', name: 'Daytime Drowsiness', nameAr: 'نعاس أثناء ساعات العمل', category: 'Sleep' },
  { id: 11, code: 'DIFFICULTY_SLEEPING', name: 'Difficulty Sleeping', nameAr: 'صعوبة الدخول في النوم', category: 'Sleep' },
  { id: 12, code: 'DRY_MOUTH', name: 'Dry Mouth', nameAr: 'جفاف الفم والعطش', category: 'Hydration' },
  { id: 13, code: 'LOW_URINE', name: 'Reduced Urine Output', nameAr: 'قلة السوائل الإخراجية', category: 'Renal' },
  { id: 14, code: 'LOWER_BACK_PAIN', name: 'Lower Back Pain', nameAr: 'ألم أسفل الظهر والفقرات', category: 'Musculoskeletal' },
  { id: 15, code: 'VISION_CHANGES', name: 'Blurred Vision / Vision Changes', nameAr: 'تشوش الرؤية القريبة', category: 'Ophthalmic' },
  { id: 16, code: 'FAINTING', name: 'Fainting / Syncope', nameAr: 'إغماء أو فقدان وعي مؤقت', category: 'Cardiovascular', isRedFlag: true },
  { id: 17, code: 'CHEST_PAIN', name: 'Chest Pain', nameAr: 'ألم في الصدر', category: 'Cardiovascular', isRedFlag: true },
  { id: 18, code: 'DIFFICULTY_BREATHING', name: 'Difficulty Breathing', nameAr: 'ضيق في التنفس', category: 'Respiratory', isRedFlag: true },
];

const defaultConditions: ConditionGuideItem[] = [
  {
    id: 1,
    code: 'SAS',
    name: 'Space Adaptation Syndrome',
    nameAr: 'متلازمة التكيف الفضائي (دوار الفضاء)',
    description: 'Vestibular adaptation during the first 72 hours of microgravity exposure.',
    descriptionAr: 'إعادة توازن الجهاز الدهليزي والأذن الداخلية خلال أول 72 ساعة من انعدام الجاذبية.',
    selfCareGuidance: 'Limit rapid head turns, keep eyes fixed on stable cabin references, and sip electrolyte fluids.',
    selfCareGuidanceAr: 'تقليل حركة الرأس المفاجئة، تثبيت النظر، وشرب السوائل المتوازنة على دفعات.',
    whenToContactMonitor: 'If vomiting persists beyond 24 hours or prevents fluid retention.',
    whenToContactMonitorAr: 'في حال استمرار القيء لأكثر من 24 ساعة أو عدم القدرة على الاحتفاظ بالسوائل.',
  },
  {
    id: 2,
    code: 'FLUID_SHIFT',
    name: 'Cephalad Fluid Shift',
    nameAr: 'اندفاع السوائل نحو الرأس',
    description: 'Redistribution of 1.5–2L of body fluid toward the head and chest in microgravity.',
    descriptionAr: 'تحرك سوائل الجسم نحو الجزء العلوي والرأس نتيجة غياب الجاذبية.',
    selfCareGuidance: 'Use saline nasal spray, elevate upper body posture, and monitor blood pressure.',
    selfCareGuidanceAr: 'استخدام بخاخ الأنف الملحي ومتابعة قراءات ضغط الدم بانتظام.',
    whenToContactMonitor: 'If accompanied by visual changes or severe pulsating headache.',
    whenToContactMonitorAr: 'عند ترافقه مع تغير في الرؤية أو صداع نابض شديد.',
  },
  {
    id: 3,
    code: 'SANS',
    name: 'Spaceflight-Associated Neuro-ocular Syndrome',
    nameAr: 'متلازمة العصب البصري المرتبطة بالفضاء (SANS)',
    description: 'Optic disc edema and hyperopic shift caused by cephalad venous congestion.',
    descriptionAr: 'تورم خفيف في القرص البصري وتغير في حدة الرؤية القريبة.',
    selfCareGuidance: 'Use adjustable Space Anticipation Glasses and log visual acuity weekly.',
    selfCareGuidanceAr: 'استخدام نظارات القراءة القابلة للتعديل وتسجيل فحص النظر الدوري.',
    whenToContactMonitor: 'Immediately if blind spots, flashes, or sudden vision loss occur.',
    whenToContactMonitorAr: 'فوراً في حال ظهور بقع معتمة أو تراجع مفاجئ في الرؤية.',
  },
  {
    id: 4,
    code: 'CIRCADIAN_DISRUPTION',
    name: 'Circadian Rhythm Disruption',
    nameAr: 'اضطراب الساعة البيولوجية والنوم',
    description: 'Misalignment of sleep-wake cycles due to 16 orbital sunrises per day.',
    descriptionAr: 'اختلال دورة النوم والاستيقاظ بسبب تعاقب الشروق والغروب المداري كل 90 دقيقة.',
    selfCareGuidance: 'Dim cabin blue-spectrum lights 90 minutes before sleep and wear sleep mask.',
    selfCareGuidanceAr: 'تخفيف الإضاءة الزرقاء قبل النوم بـ 90 دقيقة واستخدام قناع النوم.',
    whenToContactMonitor: 'If total sleep stays under 5 hours for 3 consecutive mission days.',
    whenToContactMonitorAr: 'إذا قل إجمالي النوم عن 5 ساعات لمدة 3 أيام متتالية.',
  },
  {
    id: 5,
    code: 'DEHYDRATION',
    name: 'Spaceflight Dehydration',
    nameAr: 'الجفاف ونقص السوائل في الفضاء',
    description: 'Reduced thirst sensation and diuresis following cephalad fluid shift.',
    descriptionAr: 'انخفاض الشعور بالعطش مع زيادة إدرار السوائل في بداية المهمة.',
    selfCareGuidance: 'Drink 2.5L of water with electrolyte tablets daily on a scheduled timer.',
    selfCareGuidanceAr: 'شرب 2.5 لتر من الماء مع أملاح الإماهة يومياً وفق جدول زمني ثابت.',
    whenToContactMonitor: 'If dizziness, tachycardia (>100 bpm at rest), or dark urine persists.',
    whenToContactMonitorAr: 'في حال استمرار الدوار أو تسارع النبض أثناء الراحة فوق 100 نبضة.',
  },
  {
    id: 6,
    code: 'BACK_PAIN_SYNDROME',
    name: 'Microgravity Spinal Elongation Back Pain',
    nameAr: 'ألم أسفل الظهر وتمدد الفقرات',
    description: 'Intervertebral disc expansion lengthening the spine by up to 5 cm.',
    descriptionAr: 'تمدد الغضاريف بين الفقرات نتيجة انعدام وزن الجسم.',
    selfCareGuidance: 'Adopt a fetal knee-to-chest posture in sleeping bag and perform gentle lumbar stretches.',
    selfCareGuidanceAr: 'ضم الركبتين نحو الصدر أثناء النوم وأداء تمارين إطالة الظهر الخفيفة.',
    whenToContactMonitor: 'If numbness, tingling, or radiating leg pain develops.',
    whenToContactMonitorAr: 'في حال الشعور بتنميل أو ألم يمتد إلى الأطراف السفلية.',
  },
  {
    id: 7,
    code: 'CO2_HEADACHE',
    name: 'Elevated Cabin CO2 Headache',
    nameAr: 'صداع ارتفاع ثاني أكسيد الكربون في المقصورة',
    description: 'Localized pockets of CO2 accumulating around the head in low-airflow zones.',
    descriptionAr: 'تجمع جيوب من ثاني أكسيد الكربون في المناطق ضعيفة التهوية داخل المركبة.',
    selfCareGuidance: 'Relocate to a high-ventilation module, turn on personal fan, and check ppCO2.',
    selfCareGuidanceAr: 'الانتقال إلى منطقة جيدة التهوية وتشغيل المروحة الشخصية وفحص مستوى الغاز.',
    whenToContactMonitor: 'If cabin ppCO2 exceeds 4 mmHg or multiple crew members report headaches.',
    whenToContactMonitorAr: 'إذا شعر أكثر من فرد بالصداع أو تجاوز المؤشر البيئي الحد المسموح.',
  },
  {
    id: 8,
    code: 'ORTHOSTATIC_INTOLERANCE',
    name: 'Orthostatic Intolerance',
    nameAr: 'عدم تحمل الوقوف وانخفاض الضغط الانتصابي',
    description: 'Cardiovascular deconditioning reducing blood pressure regulation.',
    descriptionAr: 'تراجع استجابة الأوعية الدموية لتنظيم ضغط الدم.',
    selfCareGuidance: 'Complete daily ARED resistance and treadmill countermeasure exercises.',
    selfCareGuidanceAr: 'الالتزام بتمارين المقاومة والجري اليومية وشرب السوائل.',
    whenToContactMonitor: 'If lightheadedness or presyncope occurs during routine activity.',
    whenToContactMonitorAr: 'عند الشعور بدوار شديد أو قرب الإغماء أثناء النشاط المعتاد.',
  },
  {
    id: 9,
    code: 'CARDIORESPIRATORY_RED_FLAG',
    name: 'Acute Cardiorespiratory Event (Red Flag)',
    nameAr: 'حالة قلبية أو تنفسية طارئة (إنذار أحمر)',
    description: 'Critical symptoms including chest pain, syncope, or acute dyspnea.',
    descriptionAr: 'أعراض حرجة تشمل ألم الصدر أو ضيق التنفس الحاد أو الإغماء.',
    selfCareGuidance: 'Cease all physical activity immediately, secure harness, and apply 100% O2 mask if hypoxic.',
    selfCareGuidanceAr: 'التوقف الفوري عن أي مجهود، تثبيت الجسم، واستخدام قناع الأكسجين عند الحاجة.',
    whenToContactMonitor: 'IMMEDIATELY — Trigger Emergency Alert and open direct audio/chat with Flight Surgeon.',
    whenToContactMonitorAr: 'فوراً — إرسال تنبيه طوارئ والتواصل المباشر مع طبيب الرحلة.',
  },
  {
    id: 10,
    code: 'BONE_LOSS',
    name: 'Microgravity Bone Mineral Density Loss',
    nameAr: 'نقص كثافة العظام في انعدام الجاذبية',
    description: 'Unloading of weight-bearing bones causing 1–1.5% monthly bone loss if unmitigated.',
    descriptionAr: 'فقدان تدريجي لكثافة العظام الحاملة للوزن نتيجة غياب الجاذبية.',
    selfCareGuidance: 'Perform resistive loading on ARED daily and take prescribed Vitamin D3 + Calcium.',
    selfCareGuidanceAr: 'أداء تمارين المقاومة اليومية وتناول مكملات فيتامين د والكالسيوم.',
    whenToContactMonitor: 'If localized bone pain or renal colic symptoms appear.',
    whenToContactMonitorAr: 'عند الشعور بألم موضعي في العظام أو أعراض حصوات الكلى.',
  },
  {
    id: 11,
    code: 'MUSCLE_ATROPHY',
    name: 'Skeletal Muscle Atrophy',
    nameAr: 'ضمور وضعف العضلات الهيكلية',
    description: 'Loss of postural muscle mass and strength in calves, quadriceps, and back.',
    descriptionAr: 'تراجع الكتلة العضلية في الساقين والظهر نتيجة قلة الاستخدام في الفضاء.',
    selfCareGuidance: 'Complete 2 hours of combined aerobic and high-load resistance exercise daily.',
    selfCareGuidanceAr: 'إتمام ساعتين يومياً من تمارين اللياقة والمقاومة مع التغذية الغنية بالبروتين.',
    whenToContactMonitor: 'If asymmetric weakness or tendon strain occurs during training.',
    whenToContactMonitorAr: 'عند الشعور بضعف غير متماثل أو شد عضلي حاد أثناء التمرين.',
  },
  {
    id: 12,
    code: 'VENOUS_THROMBOEMBOLISM',
    name: 'Spaceflight Venous Stasis & Thrombosis Prevention',
    nameAr: 'ركود الدم الوريدي والوقاية من الجلطات',
    description: 'Slowed jugular and upper-body venous blood flow in microgravity.',
    descriptionAr: 'بطء تدفق الدم في أوردة الجزء العلوي من الجسم بسبب انعدام الجاذبية.',
    selfCareGuidance: 'Maintain optimal hydration and perform scheduled neck/upper-limb mobility exercises.',
    selfCareGuidanceAr: 'الحفاظ على الترطيب الكافي وأداء تمارين تنشيط الدورة الدموية بانتظام.',
    whenToContactMonitor: 'Immediately if neck swelling, unilateral arm swelling, or chest discomfort occurs.',
    whenToContactMonitorAr: 'فوراً في حال ملاحظة تورم في الرقبة أو الذراع أو ضيق في الصدر.',
  },
];

export const SymptomGuidancePage: React.FC = () => {
  const tx = useUiText();
  const { language } = useLanguage();
  const { activeScenario, currentAstronautId, currentRole, selectedChatAstronautId } = useTelemetry();
  const isAr = language === 'ar';

  const [symptoms, setSymptoms] = useState<SymptomItem[]>([]);
  const [conditions, setConditions] = useState<ConditionGuideItem[]>([]);
  const [conditionSearch, setConditionSearch] = useState('');
  const [selectedCodes, setSelectedCodes] = useState<string[]>(['HEADACHE']);
  const [assessing, setAssessing] = useState(false);
  const [assessmentError, setAssessmentError] = useState('');
  const [severity, setSeverity] = useState('mild');
  const [durationHours, setDurationHours] = useState(0);
  const [assessmentResult, setAssessmentResult] = useState<{
    urgency: string;
    summaryAr: string;
    summaryEn: string;
    selfCareAr: string;
    selfCareEn: string;
    contactWhenAr: string;
    contactWhenEn: string;
  } | null>(null);

  useEffect(() => {
    api
      .getSymptoms()
      .then((res) => {
        if (Array.isArray(res) && res.length > 0) {
          setSymptoms(
            res.map((s: any, idx: number) => {
              const fb = defaultSymptoms.find((d) => d.code === s.code);
              return {
                id: s.id || idx + 1,
                code: s.code,
                name: s.name,
                nameAr: symptomArabic[s.code] || fb?.nameAr || s.name,
                category: s.category || fb?.category || 'General',
                isRedFlag: s.isRedFlag ?? fb?.isRedFlag ?? false,
              };
            })
          );
        }
      })
      .catch(reportError);

    api
      .getSpaceConditions()
      .then((res) => {
        if (Array.isArray(res) && res.length > 0) {
          setConditions(
            res.map((c: any, idx: number) => {
              const fb = conditionArabic[c.code] || defaultConditions.find((d) => d.code === c.code);
              return {
                id: c.id || idx + 1,
                code: c.code,
                name: c.name,
                nameAr: fb?.nameAr || c.name,
                description: c.description,
                descriptionAr: fb?.descriptionAr || c.description,
                selfCareGuidance: c.selfCareGuidance,
                selfCareGuidanceAr: fb?.selfCareGuidanceAr || c.selfCareGuidance,
                whenToContactMonitor: c.whenToContactMonitor,
                whenToContactMonitorAr: fb?.whenToContactMonitorAr || c.whenToContactMonitor,
              };
            })
          );
        }
      })
      .catch(reportError);
  }, []);

  const toggleSymptom = (code: string) => {
    setSelectedCodes((prev) =>
      prev.includes(code) ? prev.filter((c) => c !== code) : [...prev, code]
    );
  };

  const handleAssess = async () => {
    const target = currentRole === 'Monitor' ? selectedChatAstronautId : currentAstronautId;
    if (!target || selectedCodes.length === 0) { setAssessmentError('Select an authenticated astronaut and at least one symptom.'); return; }
    setAssessing(true); setAssessmentError(''); setAssessmentResult(null);
    try {
      const res = await api.assessSymptoms(target, selectedCodes, severity, durationHours);
      if (!Array.isArray(res.reasons) || !Array.isArray(res.recommendedActions)) {
        throw new Error('This backend is running the older symptom assessment. Deploy the updated backend to use the new rule-based assessment.');
      }
      const reason = res.reasons.join(' ');
      const action = res.recommendedActions.join(' ');
      setAssessmentResult({urgency:res.level === 3 ? 'Emergency' : res.level === 2 ? 'MedicalAttention' : 'Routine', summaryEn:'Level '+res.level+': '+reason,summaryAr:translateUiText('Level '+res.level+': '+reason,'ar'),selfCareEn:action,selfCareAr:translateUiText(action,'ar'),contactWhenEn:res.disclaimer+' '+res.thresholdLabel,contactWhenAr:translateUiText(res.disclaimer+' '+res.thresholdLabel,'ar')});
    } catch (error) { setAssessmentError(error instanceof Error ? error.message : 'Assessment failed. Please retry.'); }
    finally { setAssessing(false); }
  };

  const filteredConditions = conditions.filter((c) => {
    if (!conditionSearch.trim()) return true;
    const q = conditionSearch.toLowerCase();
    return (
      c.name.toLowerCase().includes(q) ||
      c.nameAr.includes(q) ||
      c.description.toLowerCase().includes(q) ||
      c.descriptionAr.includes(q)
    );
  });

  return (
    <div className="flex flex-col gap-3.5 animate-fade-in">
      {/* ── Interactive 3D Anatomical Body Map ── */}
      <BodyMapPanel />

      {/* Top Card: Interactive 18-Symptom Assessment Engine (/api/symptoms & /api/symptoms/assess) */}
      <div className="glass-panel rounded-[22px] p-5">
        {/* Active Scenario Live Interconnection Banner */}
        {activeScenario.id !== 'nominal' && (
          <div className="mb-4 p-3.5 rounded-2xl bg-gradient-to-r from-amber-500/15 via-slate-900/90 to-cyan-500/10 border border-amber-400/40 text-xs text-amber-200 flex flex-col md:flex-row items-start md:items-center justify-between gap-3 shadow-md animate-fade-in">
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-xl bg-amber-500/20 border border-amber-400/40 flex items-center justify-center text-amber-300 shrink-0">
                <AlertTriangle className="w-5 h-5 animate-pulse" />
              </div>
              <div>
                <span className="font-extrabold text-white text-xs block">
                  {tx(isAr ? 'أعراض حية مقترنة بالسيناريو النشط:' : 'Active Scenario Associated Symptoms:')}{tx(' ')}
                  <span className="text-cyan-300 font-bold">{tx(isAr ? activeScenario.titleAr : activeScenario.titleEn)}</span>
                </span>
                <span className="text-slate-300 text-[11px] block mt-0.5">
                  {tx(isAr ? activeScenario.diagnosisAr : activeScenario.diagnosisEn)}
                </span>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-1.5">
              {(isAr ? activeScenario.symptomsAr : activeScenario.symptomsEn).map((sym, i) => (
                <span
                  key={i}
                  className="px-2.5 py-1 rounded-lg bg-black/50 border border-amber-400/30 text-[11px] font-semibold text-amber-200"
                >
                  • {tx(sym)}
                </span>
              ))}
            </div>
          </div>
        )}

        <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-cyan-500/20 border border-cyan-400/30 flex items-center justify-center text-cyan-300">
              <Stethoscope className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                {tx(isAr
                  ? 'التقييم الطبي الذاتي للأعراض (18 عرضاً معتمداً)'
                  : 'Interactive Symptom Self-Assessment (18 Clinical Symptoms)')}
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? 'اختر الأعراض الحالية لتحليل الحالة الطبية وتلقي إرشادات الرعاية الفورية'
                  : 'Select current symptoms to match spaceflight conditions and receive clinical guidance')}
              </p>
            </div>
          </div>

          <AsyncButton
            type="button"
            onClick={handleAssess}
            disabled={assessing || selectedCodes.length === 0}
            className="px-4 py-2 rounded-xl bg-gradient-to-r from-blue-600 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white font-bold text-xs shadow-lg transition-all flex items-center gap-2 disabled:opacity-50"
          >
            <Sparkles className="w-4 h-4" />
            <span>
              {tx(assessing
                ? isAr
                  ? 'جاري تحليل الأعراض...'
                  : 'Analyzing...'
                : isAr
                ? `تحليل الأعراض المختارة (${selectedCodes.length})`
                : `Analyze Selected (${selectedCodes.length})`)}
            </span>
          </AsyncButton>
        </div>

        {assessmentError && <p role="alert" className="mb-3 text-rose-300">{tx(assessmentError)}</p>}
        <div className="flex gap-4 mb-4 text-sm">
          <label>{tx("Severity ")}<select className="bg-slate-900 p-2" value={severity} onChange={e=>setSeverity(e.target.value)}><option value="mild">{tx("Mild")}</option><option value="moderate">{tx("Moderate")}</option><option value="severe">{tx("Severe")}</option></select></label>
          <label>{tx("Duration (hours) ")}<input className="bg-slate-900 p-2 w-24" type="number" min="0" max="8760" value={durationHours} onChange={e=>setDurationHours(Number(e.target.value))}/></label>
        </div>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-2">
          {symptoms.map((sym) => {
            const active = selectedCodes.includes(sym.code);
            return (
              <AsyncButton
                key={sym.code}
                type="button"
                onClick={() => toggleSymptom(sym.code)}
                className={`p-2.5 rounded-xl text-start border transition-all flex items-center justify-between gap-1.5 ${
                  active
                    ? sym.isRedFlag
                      ? 'bg-rose-500/25 border-rose-400 text-white'
                      : 'bg-cyan-500/20 border-cyan-400/60 text-white'
                    : sym.isRedFlag
                    ? 'bg-rose-950/30 border-rose-500/30 text-rose-200 hover:border-rose-400/50'
                    : 'bg-slate-900/50 border-white/10 text-slate-300 hover:border-white/25'
                }`}
              >
                <div className="min-w-0">
                  <span className="text-xs font-bold block truncate">
                    {tx(isAr ? sym.nameAr : sym.name)}
                  </span>
                  {sym.isRedFlag && (
                    <span className="text-[10px] text-rose-300 font-semibold flex items-center gap-1 mt-0.5">
                      <AlertTriangle className="w-3 h-3" />
                      {tx(isAr ? 'عرض حرج' : 'Red Flag')}
                    </span>
                  )}
                </div>
                <span
                  className={`w-4 h-4 rounded-full border shrink-0 flex items-center justify-center ${
                    active ? 'border-cyan-400 bg-cyan-400 text-slate-950' : 'border-slate-500'
                  }`}
                >
                  {active && <CheckCircle2 className="w-3 h-3" />}
                </span>
              </AsyncButton>
            );
          })}
        </div>

        {assessmentResult && (
          <div
            className={`mt-4 p-4 rounded-xl border space-y-2 ${
              assessmentResult.urgency === 'Emergency'
                ? 'bg-rose-950/50 border-rose-400/50'
                : 'bg-slate-900/65 border-cyan-400/35'
            }`}
          >
            <div className="flex items-center justify-between">
              <h4 className="text-sm font-bold text-white">
                {tx(isAr ? assessmentResult.summaryAr : assessmentResult.summaryEn)}
              </h4>
              <span
                className={`text-[11px] font-bold px-2.5 py-0.5 rounded-full border ${
                  assessmentResult.urgency === 'Emergency'
                    ? 'bg-rose-500/20 text-rose-200 border-rose-400/40'
                    : 'bg-cyan-500/15 text-cyan-200 border-cyan-400/30'
                }`}
              >
                {tx(assessmentResult.urgency === 'Emergency'
                  ? isAr
                    ? 'طوارئ طبية'
                    : 'Emergency'
                  : isAr
                  ? 'إرشاد طبي'
                  : 'Guidance')}
              </span>
            </div>
            <p className="text-xs text-slate-200 leading-relaxed">
              <strong className="text-cyan-300">
                {tx(isAr ? 'الإرشادات الذاتية: ' : 'Self-Care: ')}
              </strong>
              {tx(isAr ? assessmentResult.selfCareAr : assessmentResult.selfCareEn)}
            </p>
            <p className="text-xs text-amber-200/90 leading-relaxed">
              <strong className="text-amber-300">
                {tx(isAr ? 'متى تتواصل مع طبيب الرحلة: ' : 'When to Contact Flight Surgeon: ')}
              </strong>
              {tx(isAr ? assessmentResult.contactWhenAr : assessmentResult.contactWhenEn)}
            </p>
          </div>
        )}
      </div>

      {/* Bottom Card: All 12 Spaceflight Medical Conditions Catalog (/api/symptoms/guidance) */}
      <div className="glass-panel rounded-[22px] p-5">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-blue-500/20 border border-blue-400/30 flex items-center justify-center text-blue-300">
              <BookOpen className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                {tx(isAr
                  ? 'الدليل المرجعي للحالات الطبية الفضائية (12 متلازمة وحالة)'
                  : 'Spaceflight Medical Conditions Reference (12 Syndromes)')}
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? 'بروتوكولات الرعاية الذاتية المعتمدة للتعامل مع تأثيرات الفضاء'
                  : 'Clinical self-care protocols and escalation thresholds for spaceflight conditions')}
              </p>
            </div>
          </div>

          <div className="relative w-full sm:w-64">
            <Search className="w-4 h-4 text-slate-400 absolute top-2.5 start-3" />
            <input
              type="text"
              value={conditionSearch}
              onChange={(e) => setConditionSearch(e.target.value)}
              placeholder={tx(isAr ? 'بحث في الدليل الطبي...' : 'Search medical guide...')}
              className="w-full rounded-xl bg-slate-900/75 border border-white/15 ps-9 pe-3 py-2 text-xs text-white placeholder-slate-400 focus:outline-none focus:border-cyan-400"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 max-h-[340px] overflow-y-auto pr-1">
          {filteredConditions.map((cond) => (
            <div
              key={cond.code}
              className="p-3.5 rounded-xl bg-slate-900/55 border border-white/10 flex flex-col justify-between gap-2"
            >
              <div>
                <h4 className="text-xs font-bold text-white mb-1">
                  {tx(isAr ? cond.nameAr : cond.name)}
                </h4>
                <p className="text-[11px] text-slate-300 leading-relaxed mb-2">
                  {tx(isAr ? cond.descriptionAr : cond.description)}
                </p>
              </div>
              <div className="pt-2 border-t border-white/10 space-y-1 text-[11px]">
                <p className="text-cyan-200 leading-relaxed">
                  <strong className="text-cyan-400">
                    {tx(isAr ? 'الرعاية الذاتية: ' : 'Self-Care: ')}
                  </strong>
                  {tx(isAr ? cond.selfCareGuidanceAr : cond.selfCareGuidance)}
                </p>
                <p className="text-amber-200/90 leading-relaxed">
                  <strong className="text-amber-400">
                    {tx(isAr ? 'استشارة الطبيب: ' : 'Contact Doctor: ')}
                  </strong>
                  {tx(isAr ? cond.whenToContactMonitorAr : cond.whenToContactMonitor)}
                </p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

