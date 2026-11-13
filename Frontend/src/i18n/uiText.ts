import existing from './ui-ar.json';
import { clinicalPhrases } from './clinical';

const words: Record<string,string> = {
  ...existing, ...clinicalPhrases,
  'Average sleep was below 5 hours across two nights.':'متوسط النوم أقل من ٥ ساعات خلال ليلتين.',
  'Average sleep was below 6.5 hours across three nights.':'متوسط النوم أقل من ٦٫٥ ساعات خلال ثلاث ليالٍ.',
  'Mood was 1/5, or remained at 2/5 or lower for five days.':'المزاج ١ من ٥، أو ظل عند ٢ من ٥ أو أقل لمدة خمسة أيام.',
  'Mood was 3/5, or remained at 2/5 or lower for two days.':'المزاج ٣ من ٥، أو ظل عند ٢ من ٥ أو أقل لمدة يومين.',
  '[SLEEP] Wellness review':'مراجعة نمط النوم', '[MOOD] Wellness review':'مراجعة نمط المزاج',
  'Review this pattern with your medical monitor. Follow the mission plan. Demonstration rule; this is not a diagnosis.':'راجع هذا النمط مع المراقب الطبي واتبع خطة المهمة. قاعدة توضيحية وليست تشخيصًا.',
  'Automatic rule: SLEEP':'قاعدة تلقائية: النوم', 'Automatic rule: MOOD':'قاعدة تلقائية: المزاج',
  'Mood Score (1 - 5)':'درجة المزاج (١ - ٥)', 'diastolic':'الانبساطي',
  'Pending synchronization':'في انتظار المزامنة', 'Automatic':'تلقائي',
  'The local queue is full. Synchronize existing measurements first.':'قائمة الانتظار المحلية ممتلئة. زامن القراءات الحالية أولًا.',
  'Sign in as the astronaut who recorded these measurements.':'سجّل الدخول بحساب رائد الفضاء الذي سجّل هذه القراءات.',
  'Choose a whole reminder interval.':'اختر فاصل تذكير بعدد صحيح موجب.',
  'The maximum reminder interval is seven days.':'أقصى فاصل بين التذكيرات هو سبعة أيام.',
  'Reminder changed. Refresh and retry.':'تغيّرت إعدادات التذكير. حدّث الصفحة وأعد المحاولة.',
  'This assignment is already complete or inactive.':'هذه المهمة مكتملة بالفعل أو غير نشطة.',
  'This assignment is not active.':'هذه المهمة غير نشطة.',
  'You can only manage your own reminders.':'يمكنك إدارة تذكيرات مهامك فقط.',
  'Assignment not found.':'المهمة غير موجودة.',
  'Ground Medical Monitor':'طبيب المتابعة الأرضية','Ground':'الأرضي','MonitorOnly':'للطبيب فقط','Prescription Desk':'مكتب التوجيهات','Physiological Rationale':'المبرر الفسيولوجي','Hz':'هرتز',
  'Morgan Vance':'مورغان فانس','Jordan Lee':'جوردان لي','Taylor Quinn':'تايلور كوين','Casey Rivera':'كيسي ريفيرا','Dr. Casey Rivera':'د. كيسي ريفيرا',
  'Ares III Expedition':'بعثة أريس الثالثة','Mars - Acidalia Planitia':'المريخ - سهل أسيداليا','Lunar South Pole':'القطب الجنوبي للقمر',
  'No data':'لا توجد بيانات','Saving…':'جارٍ الحفظ…','Saved':'تم الحفظ','Save':'حفظ','Retry':'إعادة المحاولة','Loading…':'جارٍ التحميل…',
  'Severity':'شدة الأعراض','Mild':'خفيفة','Moderate':'متوسطة','Severe':'شديدة','Duration (hours)':'المدة بالساعات',
  'Routine':'متابعة روتينية','MedicalAttention':'تحتاج إلى متابعة طبية','Emergency':'طوارئ','Pending':'قيد الانتظار',
  'Acknowledged':'تم الاطلاع','Completed':'مكتمل','Dismissed':'مستبعد','Active':'نشط','Resolved':'تم الحل',
  'Scheduled':'مجدول','Aborted':'ملغى','Excellent':'ممتاز','Good':'جيد','Fair':'متوسط','Poor':'ضعيف',
  'Medical':'طبي','Physical':'بدني','Psychological':'نفسي','Sleep':'النوم','Environmental':'بيئي',
  'Low':'منخفض','Medium':'متوسط','High':'مرتفع','Urgent':'عاجل','Daily Recurring':'متكرر يوميًا','Countermeasure':'تمرين وقائي','Once':'مرة واحدة','DailyRecurring':'متكرر يوميًا',
  'MicrogravityFloating':'طفو في انعدام الجاذبية','Sitting':'جلوس','Supine':'استلقاء','Standing':'وقوف','Resting':'راحة',
  'DuringExercise':'أثناء التمرين','PostExercise':'بعد التمرين','PreSleep':'قبل النوم','PostSleep':'بعد النوم','ExtravehicularActivity':'نشاط خارج المركبة',
  'Fasting':'صائم','PostMeal':'بعد الوجبة','Random':'عشوائي','Focused & Optimistic':'تركيز وتفاؤل','Calm & Balanced':'هدوء واتزان',
  'Anxious & Stressed':'قلق وتوتر','Tired & Fatigued':'تعب وإرهاق','Isolated & Lonely':'عزلة ووحدة',
  'Heart rate history':'سجل نبض القلب','No data. Record a measurement or retry loading the dashboard.':'لا توجد بيانات. سجّل قياسًا أو أعد تحميل لوحة المتابعة.',
  'No measurements have been loaded from the backend.':'لم تُحمّل أي قياسات من الخادم بعد.',
  'Status is based on the latest backend readings. Demo thresholds require mission medical review.':'تعتمد الحالة على أحدث القياسات المسجلة. الحدود التوضيحية تحتاج إلى مراجعة الفريق الطبي للمهمة.',
  'Live updates interrupted. Reconnecting…':'انقطع التحديث المباشر. جارٍ إعادة الاتصال…','Network unavailable':'الاتصال بالشبكة غير متاح',
  'Failed to fetch':'تعذّر الاتصال بالخادم','Enter your password to sign in.':'أدخل كلمة المرور لتسجيل الدخول.',
  'Select an astronaut.':'اختر رائد فضاء.','Select an authenticated astronaut and at least one symptom.':'اختر رائد فضاء مسجل الدخول وعرضًا واحدًا على الأقل.',
  'Assessment failed. Please retry.':'تعذّر تقييم الأعراض. حاول مرة أخرى.',
  'The API returned an unexpected response. Check the API URL and reverse proxy.':'أعاد الخادم استجابة غير متوقعة. تحقق من عنوان الخادم وإعدادات الاتصال.',
  'This backend is running the older symptom assessment. Deploy the updated backend to use the new rule-based assessment.':'الخادم يستخدم إصدارًا قديمًا من تقييم الأعراض. شغّل إصدار الخادم المحدّث لاستخدام التقييم الجديد.',
  'Could not save. Please retry.':'تعذّر الحفظ. حاول مرة أخرى.','Dismiss error':'إغلاق رسالة الخطأ','Initial password':'كلمة المرور الأولية',
  'The server could not complete the request. Please retry.':'تعذّر على الخادم إتمام الطلب. حاول مرة أخرى.',
  'Invalid credentials.':'بيانات الدخول غير صحيحة.','Invalid username or password.':'اسم المستخدم أو كلمة المرور غير صحيحة.',
  'Role must be either \'Astronaut\' or \'Monitor\'.':'يجب أن يكون الدور رائد فضاء أو طبيب رحلة.',
  'At least one symptom code must be provided.':'اختر عرضًا واحدًا على الأقل.','Symptom code cannot be empty.':'لا يمكن ترك العرض فارغًا.',
  'At least one reading must be provided.':'أدخل قياسًا واحدًا على الأقل.','Severity must be mild, moderate, or severe.':'اختر شدة الأعراض: خفيفة أو متوسطة أو شديدة.',
  'Message content cannot be empty.':'لا يمكن إرسال رسالة فارغة.',
  'h':'ساعة','m':'دقيقة','Normal:':'طبيعي:','Critical:':'حرج:','Deep:':'نوم عميق:','REM:':'نوم حركة العين السريعة:','Light:':'نوم خفيف:','Awake:':'يقظة:',
  'bpm':'نبضة/دقيقة','breaths/min':'نفس/دقيقة','mmHg':'ملم زئبق','mg/dL':'ملغ/ديسيلتر','°C':'°م','cm':'سم','kg':'كغ','min':'دقيقة',
  'HR':'نبض القلب','BP':'ضغط الدم','SPO2':'تشبع الأكسجين','SpO2':'تشبع الأكسجين','TEMP':'حرارة الجسم','GLUCOSE':'سكر الدم','RESP':'معدل التنفس',
  'SANS':'التغيرات العصبية والبصرية المرتبطة بالفضاء','JWT':'رمز التحقق','PDF':'مستند محمول','UTC':'توقيت غرينتش','CO2':'ثاني أكسيد الكربون','O2':'الأكسجين','NASA':'ناسا','SignalR':'الاتصال المباشر','ARED':'جهاز تمارين المقاومة','EVA':'النشاط خارج المركبة','REM':'حركة العين السريعة',
  'Monitor':'طبيب الرحلة','Astronaut':'رائد فضاء','Online':'متصل','Offline':'غير متصل',
  'All Crew':'كل أفراد الطاقم','Physical / Countermeasure':'تمارين بدنية ووقائية','Circadian':'الإيقاع اليومي','API':'واجهة الخادم',
  'English':'الإنجليزية','Arabic':'العربية','Normal':'طبيعي','Critical':'حرج','Guidance':'إرشادات','Level':'المستوى',
};
const replacements = Object.entries(words).sort((a,b)=>b[0].length-a[0].length).map(([en,ar])=>[
  new RegExp('(?<![A-Za-z])'+en.replace(/[.*+?^${}()|[\]\\]/g,'\\$&')+'(?![A-Za-z])','g'),ar
] as const);
const cache = new Map<string,string>();
const fields: Record<string,string> = {
  'HabitPoints':'نقاط العادة','ScheduledTime':'موعد المهمة','PrescribedBy':'الطبيب المسؤول',
  'Username':'اسم المستخدم','Email':'البريد الإلكتروني','Password':'كلمة المرور','InitialPassword':'كلمة المرور الأولية',
  'FirstName':'الاسم الأول','LastName':'اسم العائلة','FullName':'الاسم الكامل','Gender':'الجنس','BloodType':'فصيلة الدم',
  'DateOfBirth':'تاريخ الميلاد','HeightCm':'الطول','WeightKg':'الوزن','AstronautId':'رائد الفضاء','AstronautCode':'رمز رائد الفضاء',
  'Title':'العنوان','Content':'المحتوى','Reason':'السبب','Value':'القيمة','SecondaryValue':'القيمة الثانية',
  'Current password':'كلمة المرور الحالية','New password':'كلمة المرور الجديدة','Token':'رمز الدخول','RefreshToken':'رمز تجديد الجلسة',
  'Mission code':'رمز المهمة','Mission name':'اسم المهمة','Destination':'الوجهة','StartDate':'تاريخ البدء','EndDate':'تاريخ الانتهاء',
  'StartTime':'وقت البدء','EndTime':'وقت الانتهاء','MoodScore':'درجة المزاج','StressLevel':'مستوى التوتر','DominantEmotion':'الشعور الغالب',
  'DeepSleepMinutes':'دقائق النوم العميق','RemSleepMinutes':'دقائق نوم حركة العين السريعة','LightSleepMinutes':'دقائق النوم الخفيف','AwakeMinutes':'دقائق اليقظة','SleepEfficiencyPercentage':'كفاءة النوم',
  'Alert title':'عنوان التنبيه','Alert message':'رسالة التنبيه','ActionRequired':'الإجراء المطلوب','Resolution notes':'ملاحظات الحل',
  'AlertSeverity':'شدة التنبيه','RecommendationCategory':'فئة التوصية','RecommendationPriority':'أولوية التوصية','RecommendationStatus':'حالة التوصية','mission status':'حالة المهمة','SleepQuality':'جودة النوم',
  'ClientGeneratedId':'معرّف السجل','RecordedAt timestamp':'وقت تسجيل القياس','RecordedAt':'وقت التسجيل','DeviceSyncTime':'وقت المزامنة','SymptomCodes list':'قائمة الأعراض','Readings collection':'قائمة القياسات',
  'VitalSignTypeId':'نوع المؤشر الحيوي','DataQuality':'جودة البيانات','BodyPosition':'وضعية الجسم','ActivityContext':'سياق النشاط','MealContext':'سياق الوجبة','MinutesSinceLastMeal':'الدقائق منذ آخر وجبة',
};
export function translateUiText(value: string | number | null | undefined, language: string): string {
  let text = String(value ?? '');
  if (language !== 'ar' || !/[A-Za-z]/.test(text) || /^(https?:\/\/|[^\s]+@[^\s]+$)/.test(text)) return text;
  const original = text;
  if (cache.has(original)) return cache.get(original)!;
  text = words[text] || text;
  for (const [field,ar] of Object.entries(fields).sort((a,b)=>b[0].length-a[0].length)) text=text.split(field).join(ar);
  text=text.replace(/ is required\./g,' مطلوب.').replace(/ must be greater than 0\./g,' يجب أن يكون أكبر من صفر.')
    .replace(/ cannot exceed (\d+) characters\./g,' لا يمكن أن يتجاوز $1 حرفًا.')
    .replace(/ must be at least (\d+) characters\./g,' يجب ألا يقل عن $1 أحرف.')
    .replace(/ must be between (\d+) and (\d+)\./g,' يجب أن يكون بين $1 و$2.')
    .replace(/ must be non-negative\./g,' يجب أن يكون صفرًا أو أكبر.').replace(/ cannot be negative\./g,' لا يمكن أن يكون سالبًا.')
    .replace(/ must be in the past\./g,' يجب أن يكون في الماضي.').replace(/ cannot be null\./g,' لا يمكن أن تكون فارغة.')
    .replace(/Valid (.+?) مطلوب\./g,'يجب اختيار $1 صحيح.');
  text = text.replace(/Reported (mild|moderate|severe) symptoms:/g,(_,s)=>`الأعراض المبلغ عنها (${s==='mild'?'خفيفة':s==='moderate'?'متوسطة':'شديدة'}):`)
    .replace(/Red flag symptom\(s\) reported:/g,'علامات الخطر المبلغ عنها:')
    .replace(/Symptoms have lasted ([\d.]+) hours\./g,'استمرت الأعراض لمدة $1 ساعة.')
    .replace(/Related (\w+) reading is/g,'القياس المرتبط لمؤشر $1 هو')
    .replace(/Latest sleep duration was ([\d.]+) hours alongside psychological distress\./g,'بلغت مدة النوم الأخيرة $1 ساعة مع وجود ضغط نفسي.')
    .replace(/Enter the password for (.+) to sign in\./g,'أدخل كلمة مرور $1 لتسجيل الدخول.')
    .replace(/\b(\d+(?:\.\d+)?)\s*h\b/g,'$1 ساعة').replace(/\b(\d+(?:\.\d+)?)\s*m\b/g,'$1 دقيقة');
  for (const [pattern, ar] of replacements) text = text.replace(pattern, ar);
  if (cache.size > 2000) cache.clear();
  cache.set(original,text);
  return text;
}
