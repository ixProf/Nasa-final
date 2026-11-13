import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import { AsyncForm } from "../AsyncForm";
import { reportError } from '../../services/errors';
import React, { useState } from 'react';
import {
  Users,
  UserPlus,
  Heart,
  Wind,
  Droplets,
  Thermometer,
  Eye,
  Edit3,
  X,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { api } from '../../services/api';

export const CrewMonitorPage: React.FC = () => {
  const tx = useUiText();
  const { language } = useLanguage();
  const {
    crewMembers,
    provisionAstronaut,
    updateAstronautProfile,
    toggleAstronautActive,
  } = useTelemetry();
  const isAr = language === 'ar';

  // New Astronaut state
  const [showAddCrew, setShowAddCrew] = useState(false);
  const [fullNameEn, setFullNameEn] = useState('');
  const [fullNameAr, setFullNameAr] = useState('');
  const [code, setCode] = useState('AST-004');
  const [bloodType, setBloodType] = useState('O+');
  const [initialPassword,setInitialPassword] = useState('');
  const [saving,setSaving] = useState(false);

  // Snapshot & Edit Astronaut states
  const [snapshotCrewId, setSnapshotCrewId] = useState<number | null>(null);
  const [snapshotData, setSnapshotData] = useState<any | null>(null);
  const [editingCrewId, setEditingCrewId] = useState<number | null>(null);
  const [editFirstName, setEditFirstName] = useState('');
  const [editLastName, setEditLastName] = useState('');
  const [editBlood, setEditBlood] = useState('O+');
  const [editHeight, setEditHeight] = useState('175');
  const [editWeight, setEditWeight] = useState('70');

  const handleAddAstronaut = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fullNameEn.trim() && !fullNameAr.trim()) return;
    const en = fullNameEn.trim() || fullNameAr.trim();
    const ar = fullNameAr.trim() || fullNameEn.trim();
    setSaving(true);
    try { await provisionAstronaut(en, ar, code.trim(), `${code.toLowerCase()}@mission.space`, bloodType, initialPassword); }
    catch(error){reportError(error);throw error;} finally {setSaving(false);}
    setInitialPassword('');
    setFullNameEn('');
    setFullNameAr('');
    setShowAddCrew(false);
  };

  const handleOpenSnapshot = async (id: number) => {
    setSnapshotCrewId(id);
    setEditingCrewId(null);
    const localCrew = crewMembers.find((c) => c.id === id);
    try {
      const snap = await api.getAstronautSnapshot(id);
      setSnapshotData(snap);
    } catch {
      setSnapshotData({
        astronautId: id,
        fullName: localCrew?.fullName || 'Astronaut',
        astronautCode: localCrew?.astronautCode || 'AST-001',
        missionName: localCrew?.missionName || 'Ares III Expedition',
        overallStatus: localCrew?.overallStatus || 'Routine',
        latestSleep: { durationHours: 7.5, sleepEfficiencyPercentage: 94 },
        latestMood: { moodScore: 9, stressLevel: 2 },
      });
    }
  };

  const handleOpenEdit = (crew: any) => {
    setEditingCrewId(crew.id);
    setSnapshotCrewId(null);
    const parts = (crew.fullName || '').split(' ');
    setEditFirstName(parts[0] || '');
    setEditLastName(parts.slice(1).join(' ') || '');
    setEditBlood(crew.bloodType || 'O+');
    setEditHeight('175');
    setEditWeight('70');
  };

  const handleSaveEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingCrewId) return;
    await updateAstronautProfile(editingCrewId, {
      firstName: editFirstName.trim() || 'Astronaut',
      lastName: editLastName.trim() || 'Crew',
      bloodType: editBlood,
      heightCm: parseFloat(editHeight) || 175,
      weightKg: parseFloat(editWeight) || 70,
      missionId: 1,
    });
    setEditingCrewId(null);
  };

  const statusBadge = (status: string) => {
    if (status === 'Emergency') {
      return {
        label: isAr ? 'حرج' : 'Critical',
        cls: 'bg-rose-500/15 text-rose-300 border-rose-400/30',
      };
    }
    if (status === 'MedicalAttention') {
      return {
        label: isAr ? 'تحت الملاحظة' : 'Attention',
        cls: 'bg-amber-500/15 text-amber-300 border-amber-400/30',
      };
    }
    return {
      label: isAr ? 'مستقر' : 'Stable',
      cls: 'bg-emerald-500/15 text-emerald-300 border-emerald-400/30',
    };
  };

  return (
    <div className="flex flex-col gap-3.5 animate-fade-in">
      <div className="glass-panel rounded-[22px] p-5">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-blue-500/20 border border-blue-400/30 flex items-center justify-center text-blue-300">
              <Users className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                {tx(isAr
                  ? 'المتابعة الصحية لأفراد الطاقم وإدارة ملفاتهم'
                  : 'Crew Health Monitor & Astronaut Profiles')}
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? 'مراقبة المؤشرات الحيوية المباشرة لجميع رواد الفضاء وتحديث بياناتهم الحيوية'
                  : 'Real-time crew telemetry overview, full health snapshots, and biometric profile management')}
              </p>
            </div>
          </div>

          <AsyncButton
            type="button"
            onClick={() => setShowAddCrew(!showAddCrew)}
            className="px-3.5 py-2 rounded-xl bg-blue-600/80 hover:bg-blue-500 text-white text-xs font-bold flex items-center gap-1.5 transition-all"
          >
            <UserPlus className="w-3.5 h-3.5" />
            <span>{tx(isAr ? 'إضافة رائد فضاء جديد' : 'Add Astronaut')}</span>
          </AsyncButton>
        </div>

        {showAddCrew && (
          <AsyncForm
            onSubmit={handleAddAstronaut}
            className="mb-4 p-4 rounded-xl bg-slate-900/70 border border-white/15 grid grid-cols-1 sm:grid-cols-5 gap-2.5"
          >
            <input
              type="text"
              required
              value={fullNameAr}
              onChange={(e) => setFullNameAr(e.target.value)}
              placeholder={tx(isAr ? 'الاسم بالعربية' : 'Arabic Name')}
              className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-2 text-xs text-white"
            />
            <input
              type="text"
              value={fullNameEn}
              onChange={(e) => setFullNameEn(e.target.value)}
              placeholder={tx(isAr ? 'الاسم بالإنجليزية' : 'English Name')}
              className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-2 text-xs text-white"
            />
            <input
              type="text"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder={tx("AST-004")}
              className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-2 text-xs text-white"
            />
            <select
              value={bloodType}
              onChange={(e) => setBloodType(e.target.value)}
              className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-2 text-xs text-white"
            >
              <option value="O+">{tx("O+")}</option>
              <option value="A+">{tx("A+")}</option>
              <option value="B+">{tx("B+")}</option>
              <option value="AB+">{tx("AB+")}</option>
            </select>
            <AsyncButton
              type="submit" disabled={saving}
              className="rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold py-2"
            >
              {tx(isAr ? 'حفظ' : 'Save')}
            </AsyncButton>
          </AsyncForm>
        )}

        {/* Inline Astronaut Snapshot Drawer (/api/dashboard/astronauts/{id}) */}
        {snapshotCrewId && snapshotData && (
          <div className="mb-4 p-4 rounded-2xl bg-slate-900/80 border border-cyan-400/40 flex flex-col gap-3">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="text-sm font-bold text-cyan-300">
                  {tx(isAr ? 'الملف الصحي التفصيلي: ' : 'Health Snapshot: ')}
                  <span className="text-white">{tx(snapshotData.fullName)}</span> (
                  {snapshotData.astronautCode})
                </h4>
                <p className="text-xs text-slate-300">{tx(snapshotData.missionName)}</p>
              </div>
              <AsyncButton
                type="button"
                onClick={() => setSnapshotCrewId(null)}
                className="p-1.5 rounded-lg bg-white/5 hover:bg-white/10 text-slate-300"
              >
                <X className="w-4 h-4" />
              </AsyncButton>
            </div>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 text-xs">
              <div className="p-2.5 rounded-xl bg-slate-950/60 border border-white/10">
                <p className="text-slate-400">{tx(isAr ? 'الحالة العامة' : 'Overall Status')}</p>
                <p className="font-bold text-emerald-300 mt-0.5">
                  {tx(snapshotData.overallStatus === 'Routine'
                    ? isAr
                      ? 'مستقرة'
                      : 'Routine'
                    : isAr
                    ? 'تحتاج متابعة'
                    : 'Attention')}
                </p>
              </div>
              <div className="p-2.5 rounded-xl bg-slate-950/60 border border-white/10">
                <p className="text-slate-400">{tx(isAr ? 'آخر سجل للنوم' : 'Latest Sleep')}</p>
                <p className="font-bold text-white mt-0.5" dir="ltr">
                  {snapshotData.latestSleep?.durationHours ?? 7.5}{tx("h ( ")}{snapshotData.latestSleep?.sleepEfficiencyPercentage ?? 94}%)
                </p>
              </div>
              <div className="p-2.5 rounded-xl bg-slate-950/60 border border-white/10">
                <p className="text-slate-400">{tx(isAr ? 'مؤشر المزاج' : 'Mood Score')}</p>
                <p className="font-bold text-cyan-300 mt-0.5" dir="ltr">
                  {snapshotData.latestMood?.moodScore ?? 9} / 10
                </p>
              </div>
              <div className="p-2.5 rounded-xl bg-slate-950/60 border border-white/10">
                <p className="text-slate-400">{tx(isAr ? 'التنبيهات النشطة' : 'Active Alerts')}</p>
                <p className="font-bold text-amber-300 mt-0.5">
                  {snapshotData.activeAlerts?.length ?? 0}
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Inline Edit Astronaut Form (PUT /api/astronauts/{id}) */}
        {editingCrewId && (
          <AsyncForm
            onSubmit={handleSaveEdit}
            className="mb-4 p-4 rounded-2xl bg-slate-900/80 border border-blue-400/40 space-y-3"
          >
            <div className="flex items-center justify-between">
              <h4 className="text-sm font-bold text-blue-300">
                {tx(isAr ? 'تعديل البيانات الحيوية لرائد الفضاء' : 'Edit Astronaut Biometric Profile')}
              </h4>
              <AsyncButton
                type="button"
                onClick={() => setEditingCrewId(null)}
                className="p-1.5 rounded-lg bg-white/5 hover:bg-white/10 text-slate-300"
              >
                <X className="w-4 h-4" />
              </AsyncButton>
            </div>
            <div className="grid grid-cols-2 sm:grid-cols-6 gap-2.5">
              <input
                type="text"
                required
                value={editFirstName}
                onChange={(e) => setEditFirstName(e.target.value)}
                placeholder={tx(isAr ? 'الاسم الأول' : 'First Name')}
                className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-1.5 text-xs text-white"
              />
              <input
                type="text"
                required
                value={editLastName}
                onChange={(e) => setEditLastName(e.target.value)}
                placeholder={tx(isAr ? 'الاسم الأخير' : 'Last Name')}
                className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-1.5 text-xs text-white"
              />
              <select
                value={editBlood}
                onChange={(e) => setEditBlood(e.target.value)}
                className="rounded-xl bg-slate-950/80 border border-white/15 px-2.5 py-1.5 text-xs text-white"
              >
                <option value="O+">{tx("O+")}</option>
                <option value="A+">{tx("A+")}</option>
                <option value="B+">{tx("B+")}</option>
                <option value="AB+">{tx("AB+")}</option>
              </select>
              <input
                type="number"
                value={editHeight}
                onChange={(e) => setEditHeight(e.target.value)}
                placeholder={tx(isAr ? 'الطول (سم)' : 'Height (cm)')}
                className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-1.5 text-xs text-white"
              />
              <input
                type="number"
                value={editWeight}
                onChange={(e) => setEditWeight(e.target.value)}
                placeholder={tx(isAr ? 'الوزن (كجم)' : 'Weight (kg)')}
                className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-1.5 text-xs text-white"
              />
              <AsyncButton
                type="submit"
                className="rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold py-1.5"
              >
                {tx(isAr ? 'حفظ التعديلات' : 'Update Profile')}
              </AsyncButton>
            </div>
          </AsyncForm>
        )}

        <div className="grid grid-cols-1 md:grid-cols-3 gap-3.5">
          {crewMembers.map((crew) => {
            const badge = statusBadge(crew.overallStatus);
            return (
              <div
                key={crew.id}
                className="p-4 rounded-2xl bg-slate-900/55 border border-white/10 flex flex-col justify-between gap-3"
              >
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <h4 className="text-sm font-bold text-white">
                      {tx(isAr ? crew.fullNameAr : crew.fullName)}
                    </h4>
                    <p className="text-xs text-slate-400 mt-0.5">
                      {tx(crew.astronautCode)} • {tx(crew.bloodType)} • {tx(crew.missionName)}
                    </p>
                  </div>
                  <span
                    className={`text-[11px] font-bold px-2.5 py-0.5 rounded-full border ${badge.cls}`}
                  >
                    {tx(badge.label)}
                  </span>
                </div>

                <div className="grid grid-cols-2 gap-2">
                  <div className="p-2.5 rounded-xl bg-slate-950/60 border border-white/5 flex items-center gap-2">
                    <Heart className="w-4 h-4 text-rose-400 shrink-0" />
                    <div dir="ltr">
                      <p className="text-xs font-extrabold text-white">{tx(crew.hr)} {tx(" bpm")}</p>
                    </div>
                  </div>
                  <div className="p-2.5 rounded-xl bg-slate-950/60 border border-white/5 flex items-center gap-2">
                    <Wind className="w-4 h-4 text-cyan-400 shrink-0" />
                    <div dir="ltr">
                      <p className="text-xs font-extrabold text-white">{tx(crew.spo2)}%</p>
                    </div>
                  </div>
                  <div className="p-2.5 rounded-xl bg-slate-950/60 border border-white/5 flex items-center gap-2">
                    <Droplets className="w-4 h-4 text-blue-400 shrink-0" />
                    <div dir="ltr">
                      <p className="text-xs font-extrabold text-white">{tx(crew.bp)}</p>
                    </div>
                  </div>
                  <div className="p-2.5 rounded-xl bg-slate-950/60 border border-white/5 flex items-center gap-2">
                    <Thermometer className="w-4 h-4 text-amber-400 shrink-0" />
                    <div dir="ltr">
                      <p className="text-xs font-extrabold text-white">{tx(crew.temp)}{tx("°C")}</p>
                    </div>
                  </div>
                </div>

                <div className="flex items-center justify-between pt-2 border-t border-white/5 text-xs">
                  <div className="flex items-center gap-1.5">
                    <AsyncButton
                      type="button"
                      onClick={() => handleOpenSnapshot(crew.id)}
                      className="px-2.5 py-1 rounded-lg bg-cyan-500/15 hover:bg-cyan-500/25 text-cyan-200 font-semibold flex items-center gap-1"
                    >
                      <Eye className="w-3 h-3" />
                      <span>{tx(isAr ? 'الملف الكامل' : 'Snapshot')}</span>
                    </AsyncButton>
                    <AsyncButton
                      type="button"
                      onClick={() => handleOpenEdit(crew)}
                      className="px-2.5 py-1 rounded-lg bg-blue-500/15 hover:bg-blue-500/25 text-blue-200 font-semibold flex items-center gap-1"
                    >
                      <Edit3 className="w-3 h-3" />
                      <span>{tx(isAr ? 'تعديل' : 'Edit')}</span>
                    </AsyncButton>
                  </div>

                  <AsyncButton
                    type="button"
                    onClick={() => toggleAstronautActive(crew.id)}
                    className={`font-semibold ${
                      crew.isActive ? 'text-emerald-300' : 'text-slate-400'
                    }`}
                  >
                    {tx(crew.isActive
                      ? isAr
                        ? 'نشط في الخدمة'
                        : 'Active'
                      : isAr
                      ? 'متوقف'
                      : 'Inactive')}
                  </AsyncButton>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
