import { reportError } from '../services/errors';
import { applyResolvedAlert } from '../services/alertLifecycle';
import React, { createContext, useContext, useState, useEffect, useRef } from 'react';
import {
  VitalSignCardData,
  VitalSignStatus,
  VitalSignReading,
  VitalCode,
  HeartRateTimePoint,
  HealthAlert,
  SleepRecord,
  MoodEntry,
  Recommendation,
  RecommendationStatus,
  ChatMessage,
  CrewMemberStatus,
  MissionInfo,
  OfflineSyncItem,
  CreateVitalReadingPayload,
  ClinicalScenario,
  ScenarioId,
} from '../types/telemetry';
import { signalRService } from '../services/signalrService';
import { api } from '../services/api';

export interface AstronautProfileData {
  id: number;
  astronautCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  dateOfBirth: string;
  gender: string;
  bloodType: string;
  heightCm: number;
  weightKg: number;
  emergencyContactInfo?: string;
  missionId?: number;
  missionName?: string;
}

interface TelemetryContextType {
  vitals: Record<VitalCode, VitalSignCardData>;
  vitalsLog: VitalSignReading[];
  overallStatus: VitalSignStatus;
  heartRateHistory: HeartRateTimePoint[];
  alerts: HealthAlert[];
  unreadAlertsCount: number;
  sleepRecords: SleepRecord[];
  moodEntries: MoodEntry[];
  recommendations: Recommendation[];
  chatMessages: ChatMessage[];
  unreadChatCount: number;
  selectedChatAstronautId: number;
  setSelectedChatAstronautId: (id: number) => void;
  crewMembers: CrewMemberStatus[];
  missions: MissionInfo[];
  offlineQueue: OfflineSyncItem[];
  isOnline: boolean;
  isAuthenticated: boolean;
  currentRole: 'Astronaut' | 'Monitor';
  activeAccount: string;
  currentUserCode: string;
  currentUserName: string;
  currentAstronautId: number | null;
  astronautProfile: AstronautProfileData;
  setCurrentRole: (role: 'Astronaut' | 'Monitor') => void;
  switchAccount: (username: string) => Promise<boolean>;
  login: (username: string, password?: string) => Promise<{ success: boolean; message?: string }>;
  logout: () => Promise<void>;
  recordVital: (payload: CreateVitalReadingPayload) => Promise<void>;
  recordSleep: (
    payload: Omit<
      SleepRecord,
      'id' | 'clientGeneratedId' | 'astronautId' | 'astronautFullName' | 'recordedAt'
    >
  ) => Promise<void>;
  recordMood: (payload: {
    moodScore: number;
    stressLevel: number;
    dominantEmotion: string;
    notes?: string;
  }) => Promise<void>;
  updateRecommendationStatus: (id: number, newStatus: RecommendationStatus) => Promise<void>;
  createRecommendation: (
    payload: Omit<Recommendation, 'id' | 'generatedAt' | 'status'>
  ) => Promise<void>;
  deleteRecommendation: (id: number) => Promise<void>;
  triggerRecommendationAlert: (recId: number) => void;
  acknowledgeAllAlerts: () => Promise<void>;
  triggerManualAlert: (
    title: string,
    message: string,
    severity: 'MedicalAttention' | 'Emergency'
  ) => Promise<void>;
  acknowledgeAlert: (id: number) => Promise<void>;
  resolveAlert: (id: number, notes: string) => Promise<void>;
  dismissAlert: (id: number) => void;
  sendChatMessage: (
    content: string,
    senderRole?: 'Astronaut' | 'Monitor',
    targetAstronautId?: number,
    recipientName?: string,
    recipientRole?: string
  ) => Promise<void>;
  markAllChatRead: () => void;
  provisionAstronaut: (
    fullName: string,
    fullNameAr: string,
    code: string,
    email: string,
    bloodType: string,
    initialPassword: string
  ) => Promise<void>;
  updateAstronautProfile: (
    id: number,
    payload: {
      firstName: string;
      lastName: string;
      bloodType: string;
      heightCm: number;
      weightKg: number;
      emergencyContactInfo?: string;
      missionId?: number;
    }
  ) => Promise<void>;
  toggleAstronautActive: (id: number) => Promise<void>;
  createMission: (
    code: string,
    name: string,
    nameAr: string,
    destination: string,
    destinationAr: string
  ) => Promise<void>;
  updateMissionStatus: (
    id: number,
    status: 'Scheduled' | 'Active' | 'Completed' | 'Aborted'
  ) => Promise<void>;
  deleteMissionById: (id: number) => Promise<void>;
  assignCrewToMission: (missionId: number, astronautId: number) => Promise<void>;
  unassignCrewFromMission: (missionId: number, astronautId: number) => Promise<void>;
  executeOfflineSyncBatch: () => Promise<{ synced: number; duplicates: number }>;
  toggleOnlineStatus: () => void;
  activeScenario: ClinicalScenario;
  allScenarios: ClinicalScenario[];
  activateScenario: (scenarioId: ScenarioId) => void;
  isLivePulsing: boolean;
  toggleLivePulsing: () => void;
  lastTelemetryBeatAt: string;
}

const initialVitals: Record<VitalCode, VitalSignCardData> = {
  HR: {
    code: 'HR',
    titleAr: 'نبض القلب',
    titleEn: 'Heart Rate',
    value: '72',
    unitAr: 'نبضة / دقيقة',
    unitEn: 'bpm',
    rangeText: '60 - 100',
    status: 'Routine',
    iconType: 'heart',
    progressPercent: 62,
  },
  SPO2: {
    code: 'SPO2',
    titleAr: 'مستوى الأكسجين',
    titleEn: 'Oxygen Level',
    value: '98%',
    unitAr: '',
    unitEn: '',
    rangeText: '95 - 100%',
    status: 'Routine',
    iconType: 'lungs',
    progressPercent: 92,
  },
  BP: {
    code: 'BP',
    titleAr: 'ضغط الدم',
    titleEn: 'Blood Pressure',
    value: '118',
    secondaryValue: '76',
    unitAr: 'mmHg',
    unitEn: 'mmHg',
    rangeText: '90 - 120 / 60 - 80',
    status: 'Routine',
    iconType: 'blood',
    progressPercent: 70,
  },
  TEMP: {
    code: 'TEMP',
    titleAr: 'درجة الحرارة',
    titleEn: 'Temperature',
    value: '36.6°C',
    unitAr: '',
    unitEn: '',
    rangeText: '36.1 - 37.2°C',
    status: 'Routine',
    iconType: 'thermometer',
    progressPercent: 55,
  },
  GLUCOSE: {
    code: 'GLUCOSE',
    titleAr: 'سكر الدم',
    titleEn: 'Blood Glucose',
    value: '88',
    unitAr: 'mg/dL',
    unitEn: 'mg/dL',
    rangeText: '70 - 99',
    status: 'Routine',
    iconType: 'glucose',
    progressPercent: 64,
  },
  RESP: {
    code: 'RESP',
    titleAr: 'معدل التنفس',
    titleEn: 'Respiratory Rate',
    value: '15',
    unitAr: 'نفس / دقيقة',
    unitEn: 'breaths/min',
    rangeText: '12 - 18',
    status: 'Routine',
    iconType: 'resp',
    progressPercent: 58,
  },
};

const initialHistory: HeartRateTimePoint[] = [];

const initialVitalsLog: VitalSignReading[] = [];

const initialSleepRecords: SleepRecord[] = [];

const initialMoodEntries: MoodEntry[] = [];

const initialRecommendations: Recommendation[] = [];

const initialChatMessages: ChatMessage[] = [];

const initialCrewMembers: CrewMemberStatus[] = [];

const initialMissions: MissionInfo[] = [];

const initialOfflineQueue: OfflineSyncItem[] = [];

const initialAstronautProfile: AstronautProfileData = { id:0,astronautCode:'',firstName:'',lastName:'',fullName:'',dateOfBirth:'',gender:'',bloodType:'',heightCm:0,weightKg:0 };

const accountCredentials: Record<
  string,
  { role: 'Astronaut' | 'Monitor'; code: string; astronautId: number }
> = {
  'astro.vance': {    role: 'Astronaut',
    code: 'AST-001',
    astronautId: 1,
  },
  'astro.lee': {    role: 'Astronaut',
    code: 'AST-002',
    astronautId: 2,
  },
  'astro.quinn': {    role: 'Astronaut',
    code: 'AST-003',
    astronautId: 3,
  },
  'monitor.rivera': {    role: 'Monitor',
    code: 'MONITOR',
    astronautId: 1,
  },
};

export const CLINICAL_SCENARIOS: ClinicalScenario[] = [
  {
    id: 'nominal',
    titleAr: 'الحالة المستقرة النموذجية',
    titleEn: 'Nominal Baseline State',
    subtitleAr: 'مؤشرات فسيولوجية مثالية مستقرة داخل المقصورة',
    subtitleEn: 'Optimal resting biometrics within pressurized habitat',
    descriptionAr:
      'جميع العلامات الحيوية ضمن النطاقات الخضراء لرواد الفضاء، استقرار تام في التوازن الأكسجيني والدورة الدموية.',
    descriptionEn:
      'All vitals within green nominal aerospace ranges, full hemodynamic and metabolic stability.',
    category: 'Routine',
    severity: 'Routine',
    targetVitals: {
      hr: 72,
      spo2: 98,
      bpSystolic: 118,
      bpDiastolic: 76,
      temp: 36.6,
      resp: 15,
      glucose: 88,
    },
    symptomsAr: ['لا توجد أعراض مرضية', 'حالة يقظة وتركيز ذهني ممتازة', 'استقرار حركي وتكيف حسي متكامل'],
    symptomsEn: ['No pathological symptoms', 'Sharp cognitive readiness', 'Full vestibular adaptation'],
    diagnosisAr: 'استقرار فسيولوجي نموذجي — جاهزية تامة لتنفيذ المهام المدارية (Flight Ready Nominal)',
    diagnosisEn: 'Nominal Physiological Equilibrium — Full Mission Task Readiness',
    clinicalActionsAr: [
      'مواصلة الجدول الروتيني للمهمة',
      'تسجيل القياسات الدورية كالمعتاد',
      'الحفاظ على الإماهة الطبيعية 2.5 لتر/يوم',
    ],
    clinicalActionsEn: [
      'Continue nominal timeline',
      'Periodic routine telemetry logging',
      'Standard 2.5L daily hydration',
    ],
    flightSurgeonDirectiveAr: 'طبيب الرحلة: المؤشرات في أفضل مستوياتها، استمر في جدول العمل اليومي المعتمد.',
    flightSurgeonDirectiveEn: 'Flight Surgeon: Biometrics optimal. Cleared for all scheduled operations.',
  },
  {
    id: 'eva_sortie',
    titleAr: 'نشاط خارج المركبة (EVA Spacewalk)',
    titleEn: 'Extravehicular Activity (EVA Sortie)',
    subtitleAr: 'جهد بدني واستقلابي فائق داخل بذلة الفضاء EMU',
    subtitleEn: 'Intense metabolic workload inside Extravehicular Mobility Unit',
    descriptionAr:
      'ارتفاع كبير في معدل الأيض واستهلاك الأكسجين نتيجة مقاومة ضغط البذلة وانعدام الوزن أثناء العمل الخارجي.',
    descriptionEn:
      'Significant metabolic surge and oxygen consumption countering EMU suit pressure resistance during spacewalk.',
    category: 'Exertion',
    severity: 'MedicalAttention',
    targetVitals: {
      hr: 128,
      spo2: 95,
      bpSystolic: 136,
      bpDiastolic: 88,
      temp: 37.4,
      resp: 22,
      glucose: 94,
    },
    symptomsAr: [
      'إجهاد عضلي في الساعدين والكتفين',
      'ضيق تنفس جهدي متوسط',
      'فرط تعرق واحترار داخل خوذة البذلة',
      'جفاف الفم والحاجة للسوائل',
    ],
    symptomsEn: [
      'Forearm & shoulder muscular strain',
      'Moderate exertional dyspnea',
      'Heavy perspiration & thermal load',
      'Mild xerostomia (dry mouth)',
    ],
    diagnosisAr: 'إجهاد بدني واستقلابي ناتج عن نشاط السير الفضائي (High Metabolic EVA Exertion Strain)',
    diagnosisEn: 'High Metabolic EVA Exertion Strain & Elevated Thermal Storage',
    clinicalActionsAr: [
      'رفع معدل تدفق سائل التبريد في البذلة (LCVG Coolant Flow Max)',
      'التوقف عن الحركة لمدة 5 دقائق لخفض الحمل القلبي دون 100 bpm',
      'تناول 400 مل من كيس الشرب المدمج داخل البذلة (In-suit Drink Bag)',
      'فحص مستويات ثاني أكسيد الكربون (CO2 Scrubber Sensor)',
    ],
    clinicalActionsEn: [
      'Maximize Liquid Cooling & Ventilation Garment (LCVG) flow',
      '5-minute work halt to restore heart rate below 100 bpm',
      'Hydrate via In-suit Drink Bag (400ml)',
      'Verify CO2 scrubber canister levels',
    ],
    flightSurgeonDirectiveAr:
      'طبيب الرحلة: نلاحظ ارتفاع معدل النبض إلى 128؛ خذ فترة راحة في مكانك لمدة 5 دقائق وارفع تبريد البذلة.',
    flightSurgeonDirectiveEn:
      'Flight Surgeon: Exertion index elevated at 128 bpm; pause translation and increase LCVG cooling.',
  },
  {
    id: 'hypoxia',
    titleAr: 'نقص الأكسجين وانخفاض ضغط الكابينة',
    titleEn: 'Acute Hypoxia & Depressurization Hazard',
    subtitleAr: 'هبوط حرج في الضغط الجزئي للأكسجين مع تسرع قلبي تعويضي',
    subtitleEn: 'Critical drop in oxygen saturation with compensatory tachycardia',
    descriptionAr:
      'حالة طارئة خطيرة ناتجة عن تسريب ضغط هوائي أو خلل في أنظمة دعم الحياة ECLSS تؤدي لنقص أكسجة حرج.',
    descriptionEn:
      'Critical emergency caused by cabin depressurization or ECLSS failure, triggering severe hypoxemia.',
    category: 'CriticalEmergency',
    severity: 'Emergency',
    targetVitals: {
      hr: 116,
      spo2: 87,
      bpSystolic: 104,
      bpDiastolic: 64,
      temp: 36.4,
      resp: 26,
      glucose: 82,
    },
    symptomsAr: [
      'دوار حاد وعدم اتزان',
      'رؤية نفقية وتشوش بصري',
      'بطء التفكير وصعوبة التركيز',
      'زرقة طفيفة في أطراف الأصابع',
    ],
    symptomsEn: [
      'Acute dizziness & vertigo',
      'Tunnel vision & visual blur',
      'Cognitive slowdown & impairment',
      'Peripheral cyanosis on nail beds',
    ],
    diagnosisAr:
      'نقص تأكسج دم حاد حرج ناتج عن نقص الأكسجين الجزئي (Acute Hypoxemia - Emergent Cabin Decompression)',
    diagnosisEn:
      'Acute Hypoxemia & Compensatory Tachycardia secondary to Cabin Depressurization',
    clinicalActionsAr: [
      'ارتداء قناع الأكسجين الطارئ فوراً بنسبة تدفق 100% O2',
      'إحكام إغلاق خوذة ومفاصل بذلة الضغط والتحقق من صمام الأمان',
      'إعلان حالة إنذار طوارئ حمراء لمركز التحكم وتفعيل بروتوكول العزل',
      'الجلوس في مقعد التثبيت لمنع السقوط أو فقدان الوعي الفجائي',
    ],
    clinicalActionsEn: [
      'Don emergency oxygen mask immediately with 100% O2 flow',
      'Seal pressure suit helmet & lock visors',
      'Broadcast Red Emergency Alert to Mission Control',
      'Secure in crew couch to prevent impact from sudden syncope',
    ],
    flightSurgeonDirectiveAr:
      'طبيب الرحلة — إنذار أحمر طارئ: تشبع الأكسجين 87%! ارتدِ قناع الأكسجين فوراً وتوقف عن أي نشاط!',
    flightSurgeonDirectiveEn:
      'Flight Surgeon — RED EMERGENCY ALERT: SpO2 at 87%! Don emergency O2 mask immediately!',
  },
  {
    id: 'sas_sickness',
    titleAr: 'متلازمة دوار الفضاء والتكيف الحسي (SAS)',
    titleEn: 'Space Adaptation Syndrome (SAS)',
    subtitleAr: 'اضطراب الدهليز العصبي وصعود سوائل الجسم للرأس',
    subtitleEn: 'Neuro-vestibular conflict with cephalic fluid redistribution',
    descriptionAr:
      'رد فعل شائع خلال الأيام الأولى من دخول المدار ناجم عن تعارض إشارات الأذن الداخلية مع حاسة البصر.',
    descriptionEn:
      'Neuro-vestibular conflict where otolith organs fail to sense gravitational vectors, paired with fluid shifts.',
    category: 'Adaptation',
    severity: 'MedicalAttention',
    targetVitals: {
      hr: 84,
      spo2: 98,
      bpSystolic: 132,
      bpDiastolic: 86,
      temp: 36.8,
      resp: 17,
      glucose: 86,
    },
    symptomsAr: [
      'غثيان متكرر وفقدان الشهية',
      'صداع احتقاني وضغط في الجيوب الأنفية',
      'دوار فوري عند تدوير الرأس بسرعة',
      'خمول وحساسية للضوء',
    ],
    symptomsEn: [
      'Recurrent nausea & anorexia',
      'Congestive headache from cephalic fluid shift',
      'Motion illusion on head roll/pitch',
      'Lethargy & photophobia',
    ],
    diagnosisAr:
      'متلازمة التكيف مع الجاذبية الصغرى الدرجة 2 (Space Adaptation Syndrome - Grade 2 Vestibular Malaise)',
    diagnosisEn:
      'Space Adaptation Syndrome (SAS) - Grade 2 Vestibular Malaise & Cephalic Fluid Shift',
    clinicalActionsAr: [
      'تثبيت الرأس وتجنب الحركات الزاوية السريعة (Pitch/Yaw avoidance)',
      'تناول جرعة مضاد دوار (بروميثازين 25 ملغ حسب التوجيه الطبي)',
      'ارتداء حزام الضغط البطني للحد من احتقان الرأس',
      'شرب رشفات صغيرة متكررة من الماء مع أملاح الكهارل',
    ],
    clinicalActionsEn: [
      'Stabilize head, minimize rapid pitch/yaw head movements',
      'Administer Promethazine 25mg antiemetic countermeasure as directed',
      'Adjust cabin foot restraints to anchor body position',
      'Small sips of electrolyte fluid to prevent hypovolemic dehydration',
    ],
    flightSurgeonDirectiveAr:
      'طبيب الرحلة: أعراض تكيف دهليزي معتادة في هذه المرحلة؛ ثبت رأسك وتناول جرعة التهدئة الدوائية المقررة.',
    flightSurgeonDirectiveEn:
      'Flight Surgeon: Classic neuro-vestibular adaptation. Restrict head rotations and take scheduled antiemetic.',
  },
  {
    id: 'countermeasure_exercise',
    titleAr: 'حصة تمارين وقائية مكثفة (T2 Treadmill)',
    titleEn: 'Intense Countermeasure Exercise (T2/ARED)',
    subtitleAr: 'بروتوكول حماية العضلات وكثافة العظام في الجاذبية الصغرى',
    subtitleEn: 'Musculoskeletal & cardiovascular conditioning under bungee load',
    descriptionAr:
      'جلسة تدريب عالية الشدة باستخدام جهاز السير وأشرطة السحب لإنتاج جاذبية اصطناعية ومنع ضمور العظام.',
    descriptionEn:
      'High-intensity conditioning under bungee-harness tethering to preserve bone mineral density.',
    category: 'Exertion',
    severity: 'Routine',
    targetVitals: {
      hr: 148,
      spo2: 97,
      bpSystolic: 144,
      bpDiastolic: 84,
      temp: 37.3,
      resp: 25,
      glucose: 90,
    },
    symptomsAr: [
      'تسارع نبض فسيولوجي صحي',
      'تعرق غزير وإجهاد بدني نشط',
      'احمرار الوجه مع تدفق الدم للعضلات',
    ],
    symptomsEn: [
      'Healthy physiological tachycardia',
      'Profuse perspiration & muscular exertion',
      'Facial flushing with muscle perfusion',
    ],
    diagnosisAr:
      'استجابة قلبية وعائية مثالية للتمارين الفضائية الوقائية (Nominal Peak Aerobic Countermeasure Response)',
    diagnosisEn:
      'Nominal Peak Aerobic Countermeasure Response — Cardiovascular Conditioning Target Reached',
    clinicalActionsAr: [
      'الحفاظ على وتيرة الجري لـ 30 دقيقة ضمن النطاق التدريبي المستهدف',
      'مراقبة نبض القلب لضمان عدم تجاوزه الحد الأقصى الآمن (165 bpm)',
      'التهدئة التدريجية (Cool-down) لمدة 5 دقائق قبل فك الأحزمة',
      'شرب 750 مل من السوائل المعززة بالشوارد بعد انتهاء الجلسة',
    ],
    clinicalActionsEn: [
      'Maintain running pace for 30 minutes in target aerobic zone',
      'Monitor telemetry to ensure HR remains below 165 bpm ceiling',
      '5-minute gradual cool-down prior to harness release',
      'Consume 750ml electrolyte fluid post-session',
    ],
    flightSurgeonDirectiveAr:
      'طبيب الرحلة: أداء ممتاز، معدل النبض 148 ضمن النطاق المستهدف لبناء القدرة القلبية. ابدأ التهدئة بعد 5 دقائق.',
    flightSurgeonDirectiveEn:
      'Flight Surgeon: Outstanding aerobic output; pulse at 148 is right in target zone. Begin cool-down in 5 mins.',
  },
  {
    id: 'deep_sleep',
    titleAr: 'الراحة العميقة والنوم الساكن (Deep Sleep)',
    titleEn: 'Deep Slow-Wave Sleep & Circadian Recovery',
    subtitleAr: 'هدوء باراسمبثاوي واستشفاء بيولوجي في كيس النوم المداري',
    subtitleEn: 'Parasympathetic tone & circadian cellular restoration in sleep pod',
    descriptionAr:
      'مرحلة النوم العميق داخل كيس النوم المثبت، حيث تنخفض معدلات الأيض والنبض لإعادة بناء الأنسجة.',
    descriptionEn:
      'Deep slow-wave restorative sleep inside sleep cabin; basal metabolic rate with minimal workload.',
    category: 'Recovery',
    severity: 'Routine',
    targetVitals: {
      hr: 54,
      spo2: 99,
      bpSystolic: 106,
      bpDiastolic: 68,
      temp: 36.3,
      resp: 11,
      glucose: 78,
    },
    symptomsAr: [
      'لا توجد أعراض — استرخاء عضلي وعصبي تام',
      'تنفس بطيء ومنتظم',
      'استجابة بيولوجية ترميمية ممتازة',
    ],
    symptomsEn: [
      'No symptoms — full neuromusculoskeletal relaxation',
      'Slow, rhythmic diaphragmatic breathing',
      'Peak circadian cellular repair',
    ],
    diagnosisAr:
      'استشفاء بيولوجي ونوم عميق نموذجي (Optimal Slow-Wave Parasympathetic Sleep)',
    diagnosisEn:
      'Optimal Restorative Slow-Wave Sleep & Parasympathetic Dominance',
    clinicalActionsAr: [
      'الحفاظ على تدفق هواء المقصورة الهادئ لمنع تجمع ثاني أكسيد الكربون حول الرأس',
      'إبقاء سدادات الأذن وأقنعة العين لحجب الضوء وضوضاء مراوح المحطة',
      'عدم مقاطعة دورة النوم إلا في حالات الإنذارات الحرجة للمحطة',
    ],
    clinicalActionsEn: [
      'Ensure continuous cabin ventilation fan to prevent CO2 pocket formation',
      'Maintain earplugs and sleep mask to mitigate ISS acoustic levels',
      'Maintain silent telemetry monitoring without waking crew member',
    ],
    flightSurgeonDirectiveAr:
      'طبيب الرحلة: مؤشرات الراحة مثالية ومعدل النبض 54 يدل على نوم عميق ومريح. مستمرون في المراقبة الصامتة.',
    flightSurgeonDirectiveEn:
      'Flight Surgeon: Biometrics nominal; 54 bpm indicates restorative deep sleep. Quiet monitoring active.',
  },
];

const TelemetryContext = createContext<TelemetryContextType | undefined>(undefined);

export const TelemetryProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [vitals, setVitals] = useState<Record<VitalCode, VitalSignCardData>>(() => Object.fromEntries(Object.entries(initialVitals).map(([code, card]) => [code, { ...card, value: 'No data', secondaryValue: undefined, rangeText: '', progressPercent: 0 }])) as Record<VitalCode, VitalSignCardData>);
  const [vitalsLog, setVitalsLog] = useState<VitalSignReading[]>([]);
  const [heartRateHistory, setHeartRateHistory] = useState<HeartRateTimePoint[]>(initialHistory);
  const [alerts, setAlerts] = useState<HealthAlert[]>([]);
  const [sleepRecords, setSleepRecords] = useState<SleepRecord[]>([]);
  const [moodEntries, setMoodEntries] = useState<MoodEntry[]>([]);
  const [recommendations, setRecommendations] = useState<Recommendation[]>([]);
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>(initialChatMessages);
  const [selectedChatAstronautId, setSelectedChatAstronautId] = useState<number>(1);
  const [crewMembers, setCrewMembers] = useState<CrewMemberStatus[]>([]);
  const [missions, setMissions] = useState<MissionInfo[]>([]);
  const [offlineQueue, setOfflineQueue] = useState<OfflineSyncItem[]>([]);
  const [isOnline, setIsOnline] = useState<boolean>(navigator.onLine);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [currentRole, setCurrentRoleState] = useState<'Astronaut' | 'Monitor'>('Astronaut');
  const [activeAccount, setActiveAccount] = useState<string>('');
  const [currentUserName, setCurrentUserName] = useState<string>('');
  const [currentAstronautId, setCurrentAstronautId] = useState<number | null>(null);
  useEffect(()=>{
    const refresh=()=>{
      try {setOfflineQueue(api.getOfflineMeasurements().map(row=>({clientGeneratedId:row.clientGeneratedId,recordedAt:row.recordedAt,syncStatus:row.syncStatus,
        itemType:row.endpoint==='/vitalsigns'?'VitalSign':row.endpoint==='/sleep'?'SleepRecord':'MoodEntry',
        summary:row.reason || (row.endpoint==='/vitalsigns'?`${row.payload.value}${row.payload.secondaryValue?'/'+row.payload.secondaryValue:''}`:row.endpoint==='/mood'?`${row.payload.moodScore}/${row.payload.moodScaleMaximum || 10}`:String(row.payload.startTime))})));}
      catch(error){reportError(error);}
    };
    refresh();window.addEventListener('app:queue-changed',refresh);
    return()=>window.removeEventListener('app:queue-changed',refresh);
  },[currentAstronautId]);
  const [astronautProfile, setAstronautProfile] =
    useState<AstronautProfileData>(initialAstronautProfile);
  const [activeScenario, setActiveScenario] = useState<ClinicalScenario>(CLINICAL_SCENARIOS[0]);
  const [isLivePulsing, setIsLivePulsing] = useState<boolean>(false);
  const [lastTelemetryBeatAt, setLastTelemetryBeatAt] = useState<string>(new Date().toISOString());

  const pendingChatIds = useRef(new Map<string,string>());
  const sessionGeneration = useRef(0);
  const resetAccountData = () => {
    pendingChatIds.current.clear();
    setAlerts([]);
    setVitalsLog([]);
    setHeartRateHistory([]);
    setSleepRecords([]);
    setMoodEntries([]);
    setRecommendations([]);
    setChatMessages([]);
    setCrewMembers([]);
    setMissions([]);
    setOfflineQueue([]);
    setAstronautProfile(initialAstronautProfile);
    setIsLivePulsing(false);
    setVitals(Object.fromEntries(Object.entries(initialVitals).map(([code, card]) => [code,
      { ...card, value: 'No data', secondaryValue: undefined, rangeText: '', progressPercent: 0 }
    ])) as Record<VitalCode, VitalSignCardData>);
  };

  useEffect(() => {
    let mounted = true;
    const startingToken = api.getAccessToken();
    const generation = sessionGeneration.current;
    if (startingToken) api.getCurrentUser().then(user => {
      if (!mounted || generation !== sessionGeneration.current || !api.getAccessToken()) return;
      setActiveAccount(user.username); setCurrentUserName(user.fullName);
      setCurrentRoleState(user.role); setCurrentAstronautId(user.astronautId ?? null);
      if (user.astronautId) setSelectedChatAstronautId(user.astronautId);
      setIsAuthenticated(true);
    }).catch(reportError);
    const expired = () => { sessionGeneration.current++; api.clearTokens(); resetAccountData(); setIsAuthenticated(false); setActiveAccount(''); setCurrentUserName(''); setCurrentAstronautId(null); void signalRService.disconnect(); };
    window.addEventListener('app:unauthorized',expired);
    const online = () => setIsOnline(navigator.onLine);
    window.addEventListener('online', online); window.addEventListener('offline', online);
    return () => { mounted = false; window.removeEventListener('app:unauthorized',expired); window.removeEventListener('online',online); window.removeEventListener('offline',online); };
  }, []);

  const toggleLivePulsing = () => {
    setIsLivePulsing((prev) => !prev);
  };

  const activateScenario = (scenarioId: ScenarioId) => {
    const sc = CLINICAL_SCENARIOS.find((s) => s.id === scenarioId) || CLINICAL_SCENARIOS[0];
    setActiveScenario(sc);

    // 1. Update Vitals state with targeted scenario physiological values
    setVitals((prev) => ({
      ...prev,
      HR: {
        ...prev.HR,
        value: sc.targetVitals.hr.toString(),
        status:
          sc.targetVitals.hr > 120
            ? 'MedicalAttention'
            : sc.targetVitals.hr < 50
            ? 'MedicalAttention'
            : 'Routine',
        progressPercent: Math.min(100, Math.round((sc.targetVitals.hr / 160) * 100)),
      },
      SPO2: {
        ...prev.SPO2,
        value: `${sc.targetVitals.spo2}%`,
        status:
          sc.targetVitals.spo2 < 90
            ? 'Emergency'
            : sc.targetVitals.spo2 < 95
            ? 'MedicalAttention'
            : 'Routine',
        progressPercent: sc.targetVitals.spo2,
      },
      BP: {
        ...prev.BP,
        value: `${sc.targetVitals.bpSystolic}/${sc.targetVitals.bpDiastolic}`,
        status:
          sc.targetVitals.bpSystolic > 140 || sc.targetVitals.bpSystolic < 105
            ? 'MedicalAttention'
            : 'Routine',
      },
      TEMP: {
        ...prev.TEMP,
        value: `${sc.targetVitals.temp}°C`,
        status: sc.targetVitals.temp > 37.5 ? 'MedicalAttention' : 'Routine',
      },
      RESP: {
        ...prev.RESP,
        value: sc.targetVitals.resp.toString(),
        status:
          sc.targetVitals.resp > 24
            ? 'Emergency'
            : sc.targetVitals.resp > 20
            ? 'MedicalAttention'
            : 'Routine',
      },
      GLUCOSE: {
        ...prev.GLUCOSE,
        value: sc.targetVitals.glucose.toString(),
        status: 'Routine',
      },
    }));

    // 2. Append point to Heart Rate History for live chart visualization
    setHeartRateHistory((prev) => {
      const now = new Date();
      const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      const newPoint: HeartRateTimePoint = {
        timeLabelAr: timeStr,
        timeLabelEn: timeStr,
        value: sc.targetVitals.hr,
        recordedAt: now.toISOString(),
      };
      const updated = [...prev, newPoint];
      return updated.slice(-10);
    });

    // 3. System Alert Integration (ترابط الجذور في كامل المنظومة)
    if (sc.severity === 'Emergency') {
      const emergencyAlert: HealthAlert = {
        id: Date.now(),
        astronautId: currentAstronautId || 1,
        astronautName: currentUserName || 'Morgan Vance',
        title: sc.titleAr,
        message: `${sc.subtitleAr}. تشبع الأكسجين: ${sc.targetVitals.spo2}%، النبض: ${sc.targetVitals.hr} bpm. ${sc.flightSurgeonDirectiveAr}`,
        severity: 'Emergency',
        status: 'Active',
        vitalSignTypeCode: 'SPO2',
        triggeredAt: new Date().toISOString(),
      };
      setAlerts((prev) => [emergencyAlert, ...prev.filter((a) => a.title !== sc.titleAr)]);
    } else if (sc.severity === 'MedicalAttention') {
      const warnAlert: HealthAlert = {
        id: Date.now(),
        astronautId: currentAstronautId || 1,
        astronautName: currentUserName || 'Morgan Vance',
        title: sc.titleAr,
        message: `${sc.subtitleAr}. النبض: ${sc.targetVitals.hr} bpm، الضغط: ${sc.targetVitals.bpSystolic}/${sc.targetVitals.bpDiastolic}.`,
        severity: 'MedicalAttention',
        status: 'Active',
        vitalSignTypeCode: 'HR',
        triggeredAt: new Date().toISOString(),
      };
      setAlerts((prev) => [warnAlert, ...prev.filter((a) => a.title !== sc.titleAr)]);
    } else {
      // Nominal or sleep resolves active emergency alarms
      setAlerts((prev) =>
        prev.map((a) =>
          a.status === 'Active'
            ? {
                ...a,
                status: 'Resolved',
                resolvedAt: new Date().toISOString(),
                resolutionNotes: 'عادت المؤشرات الحيوية للاستقرار الطبيعي بعد تفعيل السيناريو المستقر',
              }
            : a
        )
      );
    }

    // 4. Recommendation Integration (ترابط التوصيات السريرية)
    const customRec: Recommendation = {
      id: Date.now(),
      astronautId: currentAstronautId || 1,
      astronautFullName: currentUserName || 'Morgan Vance',
      category:
        sc.category === 'CriticalEmergency'
          ? 'Medical'
          : sc.category === 'Recovery'
          ? 'Sleep'
          : 'Physical',
      title: sc.titleEn,
      titleAr: `بروتوكول سريري: ${sc.titleAr}`,
      content: sc.clinicalActionsEn[0] || sc.flightSurgeonDirectiveEn,
      contentAr: `${sc.flightSurgeonDirectiveAr} الإجراء الميداني الفوري: ${sc.clinicalActionsAr
        .slice(0, 2)
        .join(' | ')}`,
      reason: sc.diagnosisEn,
      reasonAr: sc.diagnosisAr,
      priority:
        sc.severity === 'Emergency'
          ? 'High'
          : sc.severity === 'MedicalAttention'
          ? 'Medium'
          : 'Low',
      generatedAt: new Date().toISOString(),
      status: 'Pending',
      scheduleType: sc.severity === 'Routine' ? 'DailyRecurring' : 'Once',
      scheduledTime: '13:00',
      prescribedBy: 'د. سارة أوكونور (طبيب الرحلة الرئيسي)',
    };
    setRecommendations((prev) => [customRec, ...prev.filter((r) => r.titleAr !== customRec.titleAr)]);

    // 5. Update crew members for doctor / ground monitoring console
    setCrewMembers((prev) =>
      prev.map((c) =>
        c.id === (currentAstronautId || 1)
          ? {
              ...c,
              hr: sc.targetVitals.hr,
              spo2: sc.targetVitals.spo2,
              bp: `${sc.targetVitals.bpSystolic}/${sc.targetVitals.bpDiastolic}`,
              temp: sc.targetVitals.temp,
              overallStatus: sc.severity,
              lastSyncAt: new Date().toISOString(),
            }
          : c
      )
    );
  };

  // Live continuous telemetry oscillation loop (حيوية ونبض التيليمتري الحقيقي)
  useEffect(() => {
    if (!isLivePulsing) return;

    const timer = setInterval(() => {
      setLastTelemetryBeatAt(new Date().toISOString());

      // Realistic micro-fluctuation around target vitals
      setVitals((prev) => {
        const baseHr = activeScenario.targetVitals.hr;
        const jitterHr = Math.floor(Math.random() * 3) - 1; // -1, 0, +1
        const liveHr = Math.max(45, Math.min(180, baseHr + jitterHr));

        const baseResp = activeScenario.targetVitals.resp;
        const jitterResp = Math.random() > 0.6 ? (Math.random() > 0.5 ? 1 : -1) : 0;
        const liveResp = Math.max(8, Math.min(35, baseResp + jitterResp));

        return {
          ...prev,
          HR: {
            ...prev.HR,
            value: liveHr.toString(),
          },
          RESP: {
            ...prev.RESP,
            value: liveResp.toString(),
          },
        };
      });
    }, 2400);

    return () => clearInterval(timer);
  }, [isLivePulsing, activeScenario]);

  const setCurrentRole = (role:'Astronaut'|'Monitor') => {
    if(role!==currentRole) reportError(new Error('Sign in with an account assigned to this role.'));
  };

  const logout = async () => {
    sessionGeneration.current++;
    const logoutRequest = api.logout().catch(() => {});
    void signalRService.disconnect();
    resetAccountData();
    setIsAuthenticated(false);
    setActiveAccount('');
    setCurrentUserName('');
    setCurrentRoleState('Astronaut');
    setCurrentAstronautId(null);
    setChatMessages([]);
    await logoutRequest;
  };

  const login = async (
    username: string,
    password?: string
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      if (!password) throw new Error('Enter your password to sign in.');
      const res = await api.login(username, password);
      sessionGeneration.current++;
      const resolvedRole = res.role as 'Astronaut' | 'Monitor';
      const resolvedName = res.fullName || username;
      const resolvedAstroId = res.astronautId ?? null;
      resetAccountData();
      setIsAuthenticated(true);
      setActiveAccount(username);
      setCurrentRoleState(resolvedRole);
      setCurrentUserName(resolvedName);
      setCurrentAstronautId(resolvedAstroId);
      if (resolvedAstroId) {
        setSelectedChatAstronautId(resolvedAstroId);
      }

      void signalRService.connect();
      return { success: true };
    } catch (err: any) {
      return { success: false, message: err?.message || 'Login failed' };
    }
  };

  const switchAccount = async (username: string): Promise<boolean> => {
    await logout();
    const res = await login(username);
    return res.success;
  };

  const formatCardValue = (code: VitalCode, val: number): string => {
    if (code === 'SPO2') return `${val}%`;
    if (code === 'TEMP') return `${val}°C`;
    return val.toString();
  };

  const chatTarget = useRef(selectedChatAstronautId);
  chatTarget.current = selectedChatAstronautId;
  useEffect(() => {
    if (!isAuthenticated) return;
    void signalRService.connect(() => api.getValidAccessToken());
    return () => { void signalRService.disconnect(); };
  }, [isAuthenticated, activeAccount]);

  useEffect(() => {
    if (!isAuthenticated || currentRole !== 'Astronaut') return;
    let mounted = true;
    const refreshAssignments = () => { api.getRecommendations().then(rows => { if(mounted)setRecommendations(Array.isArray(rows)?rows:[]); }).catch(reportError); };
    window.addEventListener('app:assignment-reminder',refreshAssignments);
    return () => { mounted=false;window.removeEventListener('app:assignment-reminder',refreshAssignments); };
  }, [isAuthenticated,currentRole,currentAstronautId]);

  // Hydrate records independently from live hub startup.
  useEffect(() => {
    let mounted = true;
    const bootstrapBackendSession = async () => {
      if (!isAuthenticated) return;
      try {

        const items = (value: any) => Array.isArray(value) ? value : value?.items || [];
        if (currentRole === 'Astronaut') {
          const [dash, profile, sleep, mood, recs] = await Promise.all([
            api.getDashboardMe(), api.getAstronautMe(), api.getSleepHistory(),
            api.getMoodHistory(), api.getRecommendations(),

          ]);
          if (!mounted) return;
          setAstronautProfile(profile);
          setAlerts(items(dash.activeAlerts));

          setSleepRecords(items(sleep)); setMoodEntries(items(mood));
          setRecommendations(items(recs));
          setVitals(prev => {
            const next = { ...prev };
            const seen = new Set<VitalCode>();
            for (const reading of dash.recentVitals || []) {
              const code = reading.vitalSignTypeCode as VitalCode;
              if (seen.has(code)) continue;
              seen.add(code);
              if (next[code]) next[code] = { ...next[code], value: formatCardValue(code, reading.value), secondaryValue: reading.secondaryValue?.toString(), status: reading.status };
            }
            return next;
          });
          const history = await api.getVitalsForPeriod(new Date(Date.now() - 14*86400000).toISOString());
          if (!mounted) return;
          setVitalsLog(items(history));
          setHeartRateHistory(items(history).filter((r: VitalSignReading) => r.vitalSignTypeCode === 'HR')
            .sort((a: VitalSignReading,b: VitalSignReading) => Date.parse(a.recordedAt)-Date.parse(b.recordedAt))
            .map((r: VitalSignReading) => ({value:r.value, recordedAt:r.recordedAt, timeLabelEn:new Date(r.recordedAt).toLocaleString('en'), timeLabelAr:new Date(r.recordedAt).toLocaleString('ar')})));
        } else {
          const [dash, missions, alerts, recs, rosterResult] = await Promise.all([api.getMonitorDashboard(),api.getMissions(),api.getAlerts(),api.getRecommendations(),api.getAstronauts()]);
          if (!mounted) return;
          setAlerts(items(alerts)); setRecommendations(items(recs));
          const roster = items(rosterResult);
          setMissions(items(missions).map((m:any) => ({...m,nameAr:m.name,destinationAr:m.destination,crewIds:roster.filter((a:any)=>a.missionId===m.id).map((a:any)=>a.id)})));
          setCrewMembers((dash.crewSummaries || []).map((c:any) => {
            const v=(code:string)=>c.latestVitals?.find((r:any)=>r.vitalSignTypeCode===code);
            const profile = roster.find((a:any)=>a.id===c.astronautId) || {};
            return {...profile,...c,id:c.astronautId,fullNameAr:c.fullName,email:profile.email || '',bloodType:profile.bloodType || '',hr:v('HR')?.value,spo2:v('SPO2')?.value,bp:v('BP') ? v('BP').value+'/'+(v('BP').secondaryValue ?? 'No data') : 'No data',temp:v('TEMP')?.value,lastSyncAt:c.lastReadingRecordedAt};
          }));
        }
      } catch (error) { if (mounted) reportError(error); }
    };
    void bootstrapBackendSession();

    const unsubAlert = signalRService.onAlertReceived((incomingAlert) => {
      setAlerts((prev) => {
        if (incomingAlert.status === 'Resolved') return applyResolvedAlert(prev, incomingAlert);
        const idx = prev.findIndex((a) => a.id === incomingAlert.id);
        if (idx >= 0) {
          const copy = [...prev];
          copy[idx] = incomingAlert;
          return copy;
        }
        return [incomingAlert, ...prev];
      });
    });

    const unsubChat = signalRService.onChatReceived((incomingMsg) => {
      const activeThread =
        currentRole === 'Monitor'
          ? chatTarget.current
          : currentAstronautId || 0;

      // STRICT ISOLATION: Only add message if it belongs to the active astronaut conversation thread!
      if (incomingMsg.astronautId === activeThread) {
        setChatMessages((prev) => {
          // If matching optimistic clientGeneratedId exists, update it to server state
          if (incomingMsg.clientGeneratedId) {
            const clientIdx = prev.findIndex(
              (m) => m.clientGeneratedId === incomingMsg.clientGeneratedId
            );
            if (clientIdx >= 0) {
              const copy = [...prev];
              copy[clientIdx] = {
                ...copy[clientIdx],
                id: incomingMsg.id,
                sentAt: incomingMsg.sentAt,
                deliveryState: 'sent',
              };
              return copy;
            }
          }
          if (prev.some((m) => m.id === incomingMsg.id)) return prev;

          const senderName =
            incomingMsg.senderFullName ||
            (incomingMsg.senderRole === 'Monitor'
              ? 'د. كيسي ريفيرا'
              : incomingMsg.astronautFullName || 'رائد فضاء');

          return [
            ...prev,
            {
              id: incomingMsg.id,
              clientGeneratedId: incomingMsg.clientGeneratedId || crypto.randomUUID(),
              astronautId: incomingMsg.astronautId,
              astronautFullName: incomingMsg.astronautFullName,
              senderUserId: incomingMsg.senderUserId,
              senderName,
              senderRole: incomingMsg.senderRole,
              content: incomingMsg.content,
              sentAt: incomingMsg.sentAt,
              isRead: incomingMsg.readAt != null,
              readAt: incomingMsg.readAt,
              deliveryState: 'sent',
            },
          ];
        });
      }
    });

    const unsubChatRead = signalRService.onChatRead((readMsg) => {
      setChatMessages((prev) =>
        prev.map((m) =>
          m.id === readMsg.id ||
          (readMsg.clientGeneratedId && m.clientGeneratedId === readMsg.clientGeneratedId)
            ? { ...m, isRead: true, readAt: readMsg.readAt || new Date().toISOString() }
            : m
        )
      );
    });

    return () => {
      mounted = false;
      unsubAlert();
      unsubChat();
      unsubChatRead();
    };
  }, [activeAccount, isAuthenticated, currentRole, currentAstronautId]);

  // Load chat messages when selectedChatAstronautId changes
  useEffect(() => {
    if (!isAuthenticated || !activeAccount) {
      setChatMessages([]);
      return;
    }
    let mounted = true;
    const targetId =
      currentRole === 'Monitor'
        ? selectedChatAstronautId
        : currentAstronautId || 0;

    api
      .getChatMessages(targetId)
      .then((chatRes) => {
        const chatItems = Array.isArray(chatRes) ? chatRes : chatRes?.items;
        if (Array.isArray(chatItems) && mounted) {
          setChatMessages(
            chatItems.map((m: any) => ({
              id: m.id,
              clientGeneratedId: m.clientGeneratedId || crypto.randomUUID(),
              astronautId: m.astronautId,
              astronautFullName: m.astronautFullName,
              senderUserId: m.senderUserId,
              senderName:
                m.senderFullName ||
                (m.senderRole === 'Monitor'
                  ? 'د. كيسي ريفيرا'
                  : m.astronautFullName || 'رائد فضاء'),
              senderRole: m.senderRole,
              content: m.content,
              sentAt: m.sentAt,
              isRead: m.readAt != null,
              readAt: m.readAt,
              deliveryState: 'sent',
            }))
          );
        }
      })
      .catch(reportError);
    return () => {
      mounted = false;
    };
  }, [selectedChatAstronautId, activeAccount, currentRole, isAuthenticated, currentAstronautId]);

  // Strict, non-random binding of astronaut overall status to active clinical scenario
  const rank: Record<VitalSignStatus, number> = {Routine:0,MedicalAttention:1,Emergency:2};
  const overallStatus: VitalSignStatus = [...Object.values(vitals).map(v=>v.status), ...alerts.filter(a=>a.status!=='Resolved').map(a=>a.severity)].reduce<VitalSignStatus>((a,b)=>rank[b]>rank[a]?b:a,'Routine');



  const recordVital = async (payload: CreateVitalReadingPayload) => {
    const saved = await api.recordVital(payload);
    setVitalsLog(prev => [saved, ...prev.filter(r=>r.id!==saved.id)]);
    setVitals(prev => ({ ...prev, [payload.vitalSignTypeCode]: { ...prev[payload.vitalSignTypeCode], value: formatCardValue(payload.vitalSignTypeCode,saved.value), secondaryValue:saved.secondaryValue?.toString(), status:saved.status } }));
    if (saved.vitalSignTypeCode === 'HR') setHeartRateHistory(prev => [...prev, {value:saved.value,recordedAt:saved.recordedAt,timeLabelEn:new Date(saved.recordedAt).toLocaleString('en'),timeLabelAr:new Date(saved.recordedAt).toLocaleString('ar')}]);
  };
  const recordSleep = async (payload: Omit<SleepRecord,'id'|'clientGeneratedId'|'astronautId'|'astronautFullName'|'recordedAt'>) => {
    const saved = await api.recordSleep(payload);
    setSleepRecords(prev => [saved, ...prev.filter(r=>r.id!==saved.id)]);
    refreshWellness();
  };
  const recordMood = async (payload: {moodScore:number;stressLevel:number;dominantEmotion:string;notes?:string}) => {
    const saved = await api.recordMood({...payload,moodScaleMaximum:5});
    setMoodEntries(prev => [saved, ...prev.filter(r=>r.id!==saved.id)]);
    refreshWellness();
  };
  const refreshWellness = () => {
    const token=api.getAccessToken();
    void api.getDashboardMe().then(summary=>{
      if(token!==api.getAccessToken())return;
      setAlerts(summary.activeAlerts || []);
      setRecommendations(summary.pendingRecommendations || []);
    }).catch(reportError);
  };

  const playAlertTone = (isUrgent: boolean = false) => {
    try {
      const AudioCtx = window.AudioContext || (window as any).webkitAudioContext;
      if (!AudioCtx) return;
      const ctx = new AudioCtx();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = isUrgent ? 'sawtooth' : 'sine';
      osc.frequency.setValueAtTime(isUrgent ? 880 : 587.33, ctx.currentTime);
      gain.gain.setValueAtTime(0.09, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.35);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start();
      osc.stop(ctx.currentTime + 0.35);
    } catch {}
  };

  const updateRecommendationStatus = async (id: number, newStatus: RecommendationStatus) => {
    try {
      const saved = await api.updateRecommendationStatus(id,newStatus);
      setRecommendations(prev=>prev.map(rec=>rec.id===id?{...rec,...saved}:rec));
    } catch (error) { reportError(error); throw error; }
  };
  const createRecommendation = async (payload: Omit<Recommendation,'id'|'generatedAt'|'status'>) => {
    try {
      const saved = await api.createRecommendation(payload);
      setRecommendations(prev=>[saved,...prev]);
    } catch (error) { reportError(error); throw error; }
  };

  const deleteRecommendation = async (id:number) => {
    try { await api.updateRecommendationStatus(id,'Dismissed');setRecommendations(prev=>prev.filter(r=>r.id!==id)); }
    catch(error){reportError(error);throw error;}
  };
  const triggerRecommendationAlert = (recId:number) => {
    const rec=recommendations.find(r=>r.id===recId);
    if(rec)reportError(new Error('Reminder: '+rec.title+' — '+rec.reason));
  };
  const acknowledgeAllAlerts = async () => {
    try {
      const saved=await Promise.all(alerts.filter(a=>a.status==='Active').map(a=>api.acknowledgeAlert(a.id)));
      setAlerts(prev=>prev.map(a=>saved.find(item=>item.id===a.id)||a));
    }catch(error){reportError(error);throw error;}
  };

  const triggerManualAlert = async (title:string,message:string,severity:'MedicalAttention'|'Emergency') => {
    const target = currentRole === 'Monitor' ? selectedChatAstronautId : currentAstronautId;
    if (!target) { reportError(new Error('Select an astronaut.')); return; }
    try { const saved = await api.triggerManualAlert(title,message,severity,target); setAlerts(prev=>[saved,...prev.filter(a=>a.id!==saved.id)]); }
    catch (error) { reportError(error); throw error; }
  };
  const acknowledgeAlert = async (id:number) => {
    try { const saved = await api.acknowledgeAlert(id); setAlerts(prev=>prev.map(a=>a.id===id?saved:a)); }
    catch (error) { reportError(error); throw error; }
  };
  const resolveAlert = async (id:number,notes:string) => {
    try { const saved = await api.resolveAlert(id,notes); setAlerts(prev=>applyResolvedAlert(prev,saved)); }
    catch (error) { reportError(error); throw error; }
  };

  const dismissAlert = (id: number) => {
    setAlerts((prev) => prev.filter((a) => a.id !== id));
  };

  const sendChatMessage = async (
    content: string,
    senderRole?: 'Astronaut' | 'Monitor',
    targetAstronautId?: number,
    recipientName?: string,
    recipientRole?: string
  ) => {
    const role = senderRole || currentRole;
    const astroId =
      targetAstronautId ||
      (role === 'Monitor'
        ? selectedChatAstronautId
        : currentAstronautId || 0);

    const chatKey = role+':'+astroId+':'+content;
    const clientGenId = pendingChatIds.current.get(chatKey) || crypto.randomUUID();
    pendingChatIds.current.set(chatKey,clientGenId);
    const optimisticMsg: ChatMessage = {
      id: Date.now(),
      clientGeneratedId: clientGenId,
      astronautId: astroId,
      senderUserId: activeAccount,
      senderName:
        role === 'Monitor'
          ? 'د. كيسي ريفيرا'
          : currentUserName || 'رائد فضاء',
      senderRole: role,
      recipientName,
      recipientRole,
      content,
      sentAt: new Date().toISOString(),
      isRead: false,
      deliveryState: 'sending',
    };

    const activeThreadId =
      role === 'Monitor'
        ? selectedChatAstronautId
        : currentAstronautId || 0;

    if (astroId === activeThreadId) {
      setChatMessages((prev) => prev.some(m=>m.clientGeneratedId===clientGenId) ? prev.map(m=>m.clientGeneratedId===clientGenId?{...m,deliveryState:'sending'}:m) : [...prev, optimisticMsg]);
    }

    try {
      const savedMsg = await api.sendChatMessage(content, astroId, clientGenId);
      if (savedMsg && savedMsg.id) {
        pendingChatIds.current.delete(chatKey);
        setChatMessages((prev) =>
          prev.map((m) =>
            m.clientGeneratedId === clientGenId
              ? {
                  ...m,
                  id: savedMsg.id,
                  sentAt: savedMsg.sentAt || m.sentAt,
                  senderUserId: savedMsg.senderUserId || m.senderUserId,
                  deliveryState: 'sent',
                }
              : m
          )
        );
      }
    } catch (error) {
      setChatMessages((prev) =>
        prev.map((m) =>
          m.clientGeneratedId === clientGenId ? { ...m, deliveryState: 'failed' } : m
        )
      );
      throw error;
    }
  };

  const markAllChatRead = () => {
    const nowIso = new Date().toISOString();
    setChatMessages((prev) =>
      prev.map((m) => {
        if (!m.isRead && m.senderRole !== currentRole && m.id < 1000000000) {
          api.markChatMessageRead(m.id).catch(reportError);
          return { ...m, isRead: true, readAt: nowIso };
        }
        return m;
      })
    );
  };

  const provisionAstronaut = async (fullName:string,fullNameAr:string,code:string,email:string,bloodType:string,initialPassword:string) => {
    const parts=fullName.trim().split(' ');
    const saved=await api.createAstronaut({username:email.split('@')[0],email,initialPassword,astronautCode:code,firstName:parts[0],lastName:parts.slice(1).join(' '),dateOfBirth:'1990-01-01T00:00:00Z',gender:'Unspecified',bloodType,heightCm:175,weightKg:72});
    setCrewMembers(prev=>[...prev,{...saved,fullNameAr:fullNameAr||saved.fullName,activeAlertsCount:0,overallStatus:'Routine',bp:'No data'}]);
  };
  const updateAstronautProfile = async (id:number,payload:{firstName:string;lastName:string;bloodType:string;heightCm:number;weightKg:number;emergencyContactInfo?:string;missionId?:number}) => {
    const existing=await api.getAstronautById(id);
    const saved=await api.updateAstronaut(id,{...existing,...payload});
    setCrewMembers(prev=>prev.map(c=>c.id===id?{...c,...saved}:c));
    if(astronautProfile.id===id)setAstronautProfile(saved);
  };
  const toggleAstronautActive = async (id:number) => {
    if(!crewMembers.find(c=>c.id===id)?.isActive){reportError(new Error('The backend does not support reactivation.'));return;}
    try {await api.deactivateAstronaut(id);setCrewMembers(prev=>prev.map(c=>c.id===id?{...c,isActive:false}:c));}
    catch(error){reportError(error);throw error;}
  };
  const createMission = async (code:string,name:string,nameAr:string,destination:string,destinationAr:string) => {
    try {const saved=await api.createMission({code,name,destination,startDate:new Date().toISOString()});
    setMissions(prev=>[...prev,{...saved,nameAr:nameAr||saved.name,destinationAr:destinationAr||saved.destination,crewIds:[]}]);}
    catch(error){reportError(error);throw error;}
  };
  const updateMissionStatus = async (id:number,status:'Scheduled'|'Active'|'Completed'|'Aborted') => {
    const target=missions.find(m=>m.id===id); if(!target)throw new Error('Mission not found.');
    await api.updateMission(id,{...target,status});
    setMissions(prev=>prev.map(m=>m.id===id?{...m,status}:m));
  };
  const deleteMissionById = async (id:number) => {
    await api.deleteMission(id);setMissions(prev=>prev.filter(m=>m.id!==id));
  };
  const assignCrewToMission = async (missionId:number,astronautId:number) => {
    await api.assignAstronautToMission(missionId,astronautId);
    setMissions(prev=>prev.map(m=>({...m,crewIds:m.id===missionId?[...new Set([...m.crewIds,astronautId])]:m.crewIds.filter(id=>id!==astronautId)})));
  };
  const unassignCrewFromMission = async (missionId:number,astronautId:number) => {
    await api.unassignAstronautFromMission(missionId,astronautId);
    setMissions(prev=>prev.map(m=>m.id===missionId?{...m,crewIds:m.crewIds.filter(id=>id!==astronautId)}:m));
  };

  const executeOfflineSyncBatch = async () => {
    if (!navigator.onLine) throw new Error('Connection unavailable. Measurements have not been synced.');
    const result=await api.syncPendingMeasurements();
    const token=api.getAccessToken();
    const reload=await Promise.allSettled([api.getVitalsHistory(),api.getSleepHistory(),api.getMoodHistory()]);
    if(token===api.getAccessToken()){
      if(reload[0].status==='fulfilled')setVitalsLog(reload[0].value.items || []);
      if(reload[1].status==='fulfilled')setSleepRecords(reload[1].value.items || []);
      if(reload[2].status==='fulfilled')setMoodEntries(reload[2].value.items || []);
      refreshWellness();
    }
    return result;
  };

  const toggleOnlineStatus = () => setIsOnline(navigator.onLine);

  const unreadAlertsCount = alerts.filter((a) => a.status === 'Active').length;
  const unreadChatCount = chatMessages.filter(
    (m) => !m.isRead && m.senderRole !== currentRole
  ).length;

  return (
    <TelemetryContext.Provider
      value={{
        vitals,
        vitalsLog,
        overallStatus,
        heartRateHistory,
        alerts,
        unreadAlertsCount,
        sleepRecords,
        moodEntries,
        recommendations,
        chatMessages,
        unreadChatCount,
        selectedChatAstronautId,
        setSelectedChatAstronautId,
        crewMembers,
        missions,
        offlineQueue,
        isOnline,
        isAuthenticated,
        currentRole,
        activeAccount,
        currentUserCode:
          accountCredentials[activeAccount]?.code ||
          (currentRole === 'Monitor' ? 'MONITOR' : 'AST-001'),
        currentUserName,
        currentAstronautId,
        astronautProfile,
        setCurrentRole,
        switchAccount,
        login,
        logout,
        recordVital,
        recordSleep,
        recordMood,
        updateRecommendationStatus,
        createRecommendation,
        deleteRecommendation,
        triggerRecommendationAlert,
        acknowledgeAllAlerts,
        triggerManualAlert,
        acknowledgeAlert,
        resolveAlert,
        dismissAlert,
        sendChatMessage,
        markAllChatRead,
        provisionAstronaut,
        updateAstronautProfile,
        toggleAstronautActive,
        createMission,
        updateMissionStatus,
        deleteMissionById,
        assignCrewToMission,
        unassignCrewFromMission,
        executeOfflineSyncBatch,
        toggleOnlineStatus,
        activeScenario,
        allScenarios: CLINICAL_SCENARIOS,
        activateScenario,
        isLivePulsing,
        toggleLivePulsing,
        lastTelemetryBeatAt,
      }}
    >
      {children}
    </TelemetryContext.Provider>
  );
};

export const useTelemetry = () => {
  const context = useContext(TelemetryContext);
  if (!context) {
    throw new Error('useTelemetry must be used within a TelemetryProvider');
  }
  return context;
};
