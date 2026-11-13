export type VitalSignStatus = 'Routine' | 'MedicalAttention' | 'Emergency';

export type AlertSeverity = 'Routine' | 'MedicalAttention' | 'Emergency';

export type AlertStatus = 'Active' | 'Acknowledged' | 'Resolved';

export type BodyPosition = 'MicrogravityFloating' | 'Sitting' | 'Supine' | 'Standing';

export type ActivityContext =
  | 'Resting'
  | 'DuringExercise'
  | 'PostExercise'
  | 'PreSleep'
  | 'PostSleep'
  | 'ExtravehicularActivity';

export type MealContext = 'Fasting' | 'PostMeal' | 'Random';

export type SleepQuality = 'Poor' | 'Fair' | 'Good' | 'Excellent';

export type RecommendationCategory =
  | 'Medical'
  | 'Physical'
  | 'Psychological'
  | 'Sleep'
  | 'Environmental';

export type RecommendationPriority = 'Low' | 'Medium' | 'High' | 'Urgent';

export type RecommendationStatus = 'Pending' | 'Acknowledged' | 'Completed' | 'Dismissed';

export type MissionStatus = 'Scheduled' | 'Active' | 'Completed' | 'Aborted';

export type VitalCode = 'HR' | 'BP' | 'SPO2' | 'TEMP' | 'GLUCOSE' | 'RESP';

export type ActivePage =
  | 'dashboard'
  | 'monitor-dashboard'
  | 'vitals'
  | 'vital-guides'
  | 'sleep'
  | 'mood'
  | 'recommendations'
  | 'symptoms'
  | 'alerts'
  | 'chat'
  | 'crew'
  | 'missions'
  | 'reports-sync'
  | 'login';

export interface ReferenceRange {
  minNormal: number;
  maxNormal: number;
  emergencyBelow?: number;
  emergencyAbove?: number;
  minSecondaryNormal?: number;
  maxSecondaryNormal?: number;
  secondaryEmergencyBelow?: number;
  secondaryEmergencyAbove?: number;
}

export interface VitalSignReading {
  id: number;
  clientGeneratedId: string;
  astronautId: number;
  vitalSignTypeId: number;
  vitalSignTypeCode: VitalCode;
  vitalSignTypeName: string;
  value: number;
  secondaryValue?: number | null;
  status: VitalSignStatus;
  unit: string;
  measurementMethod?: string;
  sensorLocation?: string;
  bodyPosition?: BodyPosition;
  activityContext?: ActivityContext;
  mealContext?: MealContext;
  deviceId?: string;
  notes?: string;
  recordedAt: string;
  referenceRange?: ReferenceRange;
}

export interface VitalSignCardData {
  code: VitalCode;
  titleAr: string;
  titleEn: string;
  value: string;
  secondaryValue?: string;
  unitAr: string;
  unitEn: string;
  rangeText: string;
  status: VitalSignStatus;
  iconType: 'heart' | 'lungs' | 'blood' | 'thermometer' | 'glucose' | 'resp';
  progressPercent: number;
}

export interface HeartRateTimePoint {
  timeLabelAr: string;
  timeLabelEn: string;
  value: number;
  recordedAt: string;
}

export interface HealthAlert {
  id: number;
  astronautId: number;
  astronautName?: string;
  astronautFullName?: string;
  vitalSignTypeCode?: string;
  title: string;
  message: string;
  reason?: string;
  actionRequired?: string;
  severity: AlertSeverity;
  status: AlertStatus;
  triggeredAt: string;
  acknowledgedAt?: string;
  acknowledgedBy?: string;
  actionTaken?: string;
  resolvedAt?: string;
  resolutionNotes?: string;
  emergencyProcedureCode?: string;
  emergencyProcedureTitle?: string;
}

export interface SleepRecord {
  id: number;
  clientGeneratedId: string;
  astronautId: number;
  astronautFullName: string;
  recordedAt: string;
  startTime: string;
  endTime: string;
  durationHours: number;
  quality: SleepQuality;
  deepSleepMinutes: number;
  remSleepMinutes: number;
  lightSleepMinutes: number;
  awakeMinutes: number;
  sleepEfficiencyPercentage: number;
  notes?: string;
}

export interface MoodEntry {
  moodScaleMaximum?: number;
  id: number;
  clientGeneratedId: string;
  astronautId: number;
  astronautFullName: string;
  recordedAt: string;
  moodScore: number;
  stressLevel: number;
  dominantEmotion: string;
  notes?: string;
}

export type ScheduleType = 'Once' | 'DailyRecurring';

export interface Recommendation {
  reminderEnabled?: boolean;
  reminderIntervalMinutes?: number;
  reminderSoundEnabled?: boolean;
  isDailyHabit?: boolean;
  habitPoints?: number;
  id: number;
  astronautId: number;
  astronautFullName: string;
  generatedAt: string;
  title: string;
  titleAr?: string;
  content: string;
  contentAr?: string;
  reason: string;
  reasonAr?: string;
  category: RecommendationCategory;
  priority: RecommendationPriority;
  status: RecommendationStatus;
  scheduleType?: ScheduleType;
  scheduledTime?: string;
  prescribedBy?: string;
  actionDueDate?: string;
}

export interface ChatMessage {
  id: number;
  clientGeneratedId: string;
  /** Thread key: every conversation belongs to exactly one astronaut. */
  astronautId: number;
  astronautFullName?: string;
  senderUserId: string;
  senderName?: string;
  senderRole: 'Astronaut' | 'Monitor';
  recipientName?: string;
  recipientRole?: string;
  content: string;
  sentAt: string;
  readAt?: string | null;
  isRead?: boolean;
  /** Client-side delivery state for messages composed in this session. */
  deliveryState?: 'sending' | 'sent' | 'failed';
}

export interface AuthSession {
  userId: string;
  username: string;
  fullName: string;
  role: 'Astronaut' | 'Monitor';
  astronautId: number | null;
  astronautCode: string | null;
}

export interface CrewMemberStatus {
  id: number;
  astronautCode: string;
  fullName: string;
  fullNameAr: string;
  email: string;
  missionId: number | null;
  missionName: string;
  bloodType: string;
  isActive: boolean;
  overallStatus: VitalSignStatus;
  activeAlertsCount: number;
  hr: number;
  spo2: number;
  bp: string;
  temp: number;
  lastSyncAt: string;
}

export interface MissionInfo {
  id: number;
  code: string;
  name: string;
  nameAr: string;
  destination: string;
  destinationAr: string;
  startDate: string;
  endDate: string;
  status: MissionStatus;
  crewIds: number[];
}

export interface OfflineSyncItem {
  clientGeneratedId: string;
  itemType: 'VitalSign' | 'SleepRecord' | 'MoodEntry' | 'ChatMessage';
  summary: string;
  recordedAt: string;
  syncStatus: 'Pending' | 'Accepted' | 'Duplicate' | 'Rejected';
}

export interface CreateVitalReadingPayload {
  vitalSignTypeCode: VitalCode;
  value: number;
  secondaryValue?: number;
  bodyPosition?: BodyPosition;
  activityContext?: ActivityContext;
  mealContext?: MealContext;
  sensorLocation?: string;
  notes?: string;
}

export type ScenarioId =
  | 'nominal'
  | 'eva_sortie'
  | 'hypoxia'
  | 'sas_sickness'
  | 'countermeasure_exercise'
  | 'deep_sleep';

export interface ClinicalScenario {
  id: ScenarioId;
  titleAr: string;
  titleEn: string;
  subtitleAr: string;
  subtitleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  category: 'Routine' | 'Exertion' | 'CriticalEmergency' | 'Adaptation' | 'Recovery';
  severity: 'Routine' | 'MedicalAttention' | 'Emergency';
  targetVitals: {
    hr: number;
    spo2: number;
    bpSystolic: number;
    bpDiastolic: number;
    temp: number;
    resp: number;
    glucose: number;
  };
  symptomsAr: string[];
  symptomsEn: string[];
  diagnosisAr: string;
  diagnosisEn: string;
  clinicalActionsAr: string[];
  clinicalActionsEn: string[];
  flightSurgeonDirectiveAr: string;
  flightSurgeonDirectiveEn: string;
}
