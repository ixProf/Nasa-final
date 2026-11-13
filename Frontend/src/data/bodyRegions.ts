/**
 * bodyRegions.ts
 *
 * Shared anatomical data configuration for the 3D Body Map.
 * This file is the single source of truth for:
 *  - Anatomical region identifiers and display names (EN + AR)
 *  - Mesh name hints for the GLB model (update after model inspection)
 *  - Highlight colours for 3D mesh selection
 *  - Status indicators driven by live telemetry
 *  - Spaceflight-specific medical descriptions
 *
 * Both the 3D component (BodyMap3D.tsx) and the existing SVG fallback
 * consume this file — do NOT duplicate these values elsewhere.
 */

import type { VitalSignStatus } from '../types/telemetry';

// ─── Types ──────────────────────────────────────────────────────────────────

export type BodyRegionId =
  | 'brain'
  | 'heart'
  | 'lungs'
  | 'eyes'
  | 'stomach';

export interface BodyRegion {
  /** Unique stable identifier used across components and routes */
  id: BodyRegionId;

  /** Human-readable display label (English) */
  nameEn: string;

  /** Human-readable display label (Arabic) */
  nameAr: string;

  /**
   * Candidate mesh names to search for inside the loaded GLB.
   * Names are tried in order; the first match wins.
   * Update this list after inspecting the actual model in Blender or
   * via the GLTFLoader debug dump.
   *
   * Common naming conventions across popular anatomical models:
   *   BodyParts3D  → lower-case_underscore
   *   Z-Anatomy     → Anatomical_EN or anatomical.001
   *   Sketchfab     → varies per author
   */
  meshNames: string[];

  /**
   * Three.js hex colour used for hover highlight.
   * Keep luminous so it reads clearly against dark anatomical textures.
   */
  hoverColor: string;

  /**
   * Three.js hex colour used when this region is selected.
   */
  selectedColor: string;

  /**
   * Default tint applied to the mesh at rest (not hovered / not selected).
   * Use white for untinted, or a slight anatomical tint.
   */
  baseColor: string;

  /**
   * Short spaceflight medical description shown in the sidebar panel (EN).
   */
  descriptionEn: string;

  /**
   * Short spaceflight medical description shown in the sidebar panel (AR).
   */
  descriptionAr: string;

  /**
   * Spaceflight-specific health note (EN).
   */
  spaceflightNoteEn: string;

  /**
   * Spaceflight-specific health note (AR).
   */
  spaceflightNoteAr: string;

  /**
   * Which VitalCode(s) are most relevant to this region.
   * Used to drive the status indicator from live telemetry data.
   */
  relatedVitalCodes: string[];

  /**
   * Lucide icon type that the existing VitalCard uses for this region.
   * Kept here so SVG fallback and 3D tooltips share the same icon logic.
   */
  iconType: 'heart' | 'lungs' | 'blood' | 'thermometer' | 'glucose' | 'resp';

  /**
   * Runtime status — this field is NOT stored here statically.
   * The component reads it from TelemetryContext and injects it.
   * We declare it here so TypeScript knows the enriched shape.
   */
  status?: VitalSignStatus;
}

// ─── Region Definitions ──────────────────────────────────────────────────────

/**
 * Five anatomical regions supported by the application.
 * Order matters: it determines rendering priority and sidebar list order.
 */
export const BODY_REGIONS: BodyRegion[] = [
  {
    id: 'brain',
    nameEn: 'Brain',
    nameAr: 'الدماغ',
    meshNames: [
      // Try common naming conventions from popular models
      'brain',
      'Brain',
      'Brain_mesh',
      'brain_mesh',
      'Cerebrum',
      'cerebrum',
      'Head_Brain',
      'BrainMesh',
      'brain001',
      'Brain.001',
    ],
    hoverColor: '#a78bfa',       // Violet-400
    selectedColor: '#7c3aed',    // Violet-600
    baseColor: '#ffffff',
    descriptionEn:
      'Neurological control centre. Microgravity causes cephalad fluid shift and intracranial pressure elevation, leading to spaceflight-associated neuro-ocular syndrome (SANS) and cognitive performance changes.',
    descriptionAr:
      'مركز التحكم العصبي. تُسبب انعدام الجاذبية اندفاع سوائل نحو الرأس وارتفاعاً في الضغط داخل الجمجمة، مما يُفضي إلى متلازمة SANS وتغيرات معرفية.',
    spaceflightNoteEn:
      'Monitor ICP symptoms: headache, vision blur, papilledema. Elevated ICP is a known spaceflight risk beyond 3 months.',
    spaceflightNoteAr:
      'رصد أعراض ارتفاع الضغط داخل الجمجمة: الصداع، تشوش الرؤية. يُعدّ الارتفاع خطراً معروفاً في الرحلات الطويلة.',
    relatedVitalCodes: ['HR', 'SPO2'],
    iconType: 'resp',
  },
  {
    id: 'heart',
    nameEn: 'Heart',
    nameAr: 'القلب',
    meshNames: [
      'heart',
      'Heart',
      'Heart_mesh',
      'heart_mesh',
      'Cardio',
      'myocardium',
      'Myocardium',
      'heart001',
      'Heart.001',
      'CardiacMuscle',
    ],
    hoverColor: '#fda4af',       // Rose-300
    selectedColor: '#f43f5e',    // Rose-500
    baseColor: '#ffffff',
    descriptionEn:
      'Cardiovascular pump. In microgravity the heart becomes more spherical, cardiac output decreases at rest, and orthostatic hypotension is common during and after re-entry.',
    descriptionAr:
      'المضخة القلبية الوعائية. يصبح القلب أكثر كروية في انعدام الجاذبية، ويقل الناتج القلبي، وينتشر انخفاض الضغط الانتصابي.',
    spaceflightNoteEn:
      'HR and BP are primary cardiovascular indicators. Deconditioning risk rises significantly after 60+ mission days.',
    spaceflightNoteAr:
      'معدل القلب وضغط الدم مؤشران رئيسيان. يرتفع خطر فقدان اللياقة القلبية بعد 60 يوماً من المهمة.',
    relatedVitalCodes: ['HR', 'BP'],
    iconType: 'heart',
  },
  {
    id: 'lungs',
    nameEn: 'Lungs',
    nameAr: 'الرئتان',
    meshNames: [
      'lungs',
      'Lungs',
      'lung',
      'Lung',
      'Lung_L',
      'Lung_R',
      'left_lung',
      'right_lung',
      'LeftLung',
      'RightLung',
      'lungs_mesh',
      'lungs001',
      'Lungs.001',
      'Pulmonary',
    ],
    hoverColor: '#67e8f9',       // Cyan-300
    selectedColor: '#06b6d4',    // Cyan-500
    baseColor: '#ffffff',
    descriptionEn:
      'Pulmonary gas exchange organ. Microgravity changes ventilation-perfusion distribution, reducing functional residual capacity and altering pulmonary blood flow distribution.',
    descriptionAr:
      'عضو تبادل الغازات الرئوية. تُغير انعدام الجاذبية توزيع التهوية والتروية، مع انخفاض في السعة الوظيفية المتبقية.',
    spaceflightNoteEn:
      'SpO2 is the primary pulmonary monitor. Elevated cabin CO2 (> 4 mmHg ppCO2) can rapidly reduce O2 saturation.',
    spaceflightNoteAr:
      'تشبع الأكسجين هو المؤشر الرئيسي. ارتفاع CO2 في المقصورة يخفض التشبع بسرعة.',
    relatedVitalCodes: ['SPO2', 'RESP'],
    iconType: 'lungs',
  },
  {
    id: 'eyes',
    nameEn: 'Eyes / Optic',
    nameAr: 'العيون والعصب البصري',
    meshNames: [
      'eyes',
      'Eyes',
      'eye',
      'Eye',
      'Eye_L',
      'Eye_R',
      'left_eye',
      'right_eye',
      'LeftEye',
      'RightEye',
      'Eyeball',
      'eyeball',
      'optic',
      'Optic',
      'OpticNerve',
    ],
    hoverColor: '#86efac',       // Green-300
    selectedColor: '#22c55e',    // Green-500
    baseColor: '#ffffff',
    descriptionEn:
      'Visual system including optic nerve and retina. SANS (Spaceflight-Associated Neuro-ocular Syndrome) affects ~70% of long-duration crewmembers — causing optic disc oedema, globe flattening, and hyperopic shift.',
    descriptionAr:
      'الجهاز البصري بما يشمل العصب البصري والشبكية. تُصيب SANS نحو 70% من أفراد الطاقم في المهمات الطويلة.',
    spaceflightNoteEn:
      'Weekly visual acuity log and fundus imaging are standard protocol. Report any new blind spots immediately.',
    spaceflightNoteAr:
      'تسجيل حدة البصر أسبوعياً وتصوير قعر العين بروتوكول معتمد. الإبلاغ فوراً عن أي بقعة عمياء جديدة.',
    relatedVitalCodes: ['BP', 'HR'],
    iconType: 'resp',
  },
  {
    id: 'stomach',
    nameEn: 'GI System',
    nameAr: 'الجهاز الهضمي',
    meshNames: [
      'stomach',
      'Stomach',
      'stomach_mesh',
      'GI',
      'gi',
      'gastrointestinal',
      'Gastrointestinal',
      'abdomen',
      'Abdomen',
      'pancreas',
      'Pancreas',
      'liver',
      'Liver',
      'digestive',
      'Digestive',
      'stomach001',
      'Stomach.001',
    ],
    hoverColor: '#fcd34d',       // Amber-300
    selectedColor: '#f59e0b',    // Amber-500
    baseColor: '#ffffff',
    descriptionEn:
      'Gastrointestinal and metabolic system. Space Adaptation Syndrome causes nausea, vomiting, and appetite loss in the first 72h. Fluid redistribution alters GI motility throughout the mission.',
    descriptionAr:
      'الجهاز الهضمي والتمثيل الغذائي. تُسبب متلازمة التكيف الفضائي غثياناً وقيئاً وفقدان شهية في أول 72 ساعة.',
    spaceflightNoteEn:
      'Glucose monitoring supports metabolic health tracking. Dehydration is common in first week due to diuresis.',
    spaceflightNoteAr:
      'مراقبة الجلوكوز تدعم تتبع الصحة الأيضية. الجفاف شائع في الأسبوع الأول بسبب الإدرار البولي.',
    relatedVitalCodes: ['GLUCOSE', 'TEMP'],
    iconType: 'glucose',
  },
];

// ─── Lookup Helpers ──────────────────────────────────────────────────────────

/** O(1) lookup by region id */
export const BODY_REGION_MAP = new Map<BodyRegionId, BodyRegion>(
  BODY_REGIONS.map((r) => [r.id, r])
);

/**
 * Given a GLB mesh name, return the matching BodyRegion (if any).
 * Comparison is case-insensitive and trims trailing numeric suffixes.
 */
export function findRegionByMeshName(meshName: string): BodyRegion | undefined {
  const normalised = meshName.toLowerCase().replace(/[._\s-]?\d+$/, '');
  return BODY_REGIONS.find((region) =>
    region.meshNames.some((candidate) => {
      const c = candidate.toLowerCase().replace(/[._\s-]?\d+$/, '');
      return normalised === c || normalised.startsWith(c) || c.startsWith(normalised);
    })
  );
}

/**
 * Returns all mesh name strings as a flat deduplicated Set.
 * Useful for fast membership testing inside the GLB traversal.
 */
export function getAllMeshNameHints(): Set<string> {
  const names = new Set<string>();
  BODY_REGIONS.forEach((r) => r.meshNames.forEach((n) => names.add(n.toLowerCase())));
  return names;
}

