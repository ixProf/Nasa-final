import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import { AsyncForm } from "../AsyncForm";
import React, { useState } from 'react';
import { Rocket, Plus, Trash2, Calendar, MapPin } from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';

export const SpaceMissionsPage: React.FC = () => {
  const tx = useUiText();
  const { language } = useLanguage();
  const {
    crewMembers,
    missions,
    createMission,
    updateMissionStatus,
    deleteMissionById,
    assignCrewToMission,
    unassignCrewFromMission,
  } = useTelemetry();
  const isAr = language === 'ar';

  const [showAddMission, setShowAddMission] = useState(false);
  const [mCode, setMCode] = useState('ARTEMIS-IV');
  const [mNameEn, setMNameEn] = useState('');
  const [mNameAr, setMNameAr] = useState('');
  const [mDestEn, setMDestEn] = useState('Lunar South Pole');
  const [mDestAr, setMDestAr] = useState('القطب الجنوبي للقمر');

  const handleAddMission = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!mNameEn.trim() && !mNameAr.trim()) return;
    const en = mNameEn.trim() || mNameAr.trim();
    const ar = mNameAr.trim() || mNameEn.trim();
    await createMission(mCode.trim(), en, ar, mDestEn, mDestAr);
    setMNameEn('');
    setMNameAr('');
    setShowAddMission(false);
  };

  return (
    <div className="flex flex-col gap-3.5 animate-fade-in">
      <div className="glass-panel rounded-[22px] p-5">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-cyan-500/20 border border-cyan-400/30 flex items-center justify-center text-cyan-300">
              <Rocket className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                {tx(isAr
                  ? 'إدارة المهمات الفضائية وتوزيع الطاقم'
                  : 'Space Missions & Crew Assignment')}
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? 'إنشاء المهمات الفضائية وتحديث حالتها وتعيين رواد الفضاء عليها'
                  : 'Create missions, update operational status, and assign astronauts to active flights')}
              </p>
            </div>
          </div>

          <AsyncButton
            type="button"
            onClick={() => setShowAddMission(!showAddMission)}
            className="px-3.5 py-2 rounded-xl bg-cyan-600/80 hover:bg-cyan-500 text-white text-xs font-bold flex items-center gap-1.5 transition-all"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>{tx(isAr ? 'إنشاء مهمة جديدة' : 'Create Mission')}</span>
          </AsyncButton>
        </div>

        {showAddMission && (
          <AsyncForm
            onSubmit={handleAddMission}
            className="mb-4 p-4 rounded-xl bg-slate-900/70 border border-white/15 grid grid-cols-1 sm:grid-cols-6 gap-2.5"
          >
            <input
              type="text"
              required
              value={mNameAr}
              onChange={(e) => setMNameAr(e.target.value)}
              placeholder={tx(isAr ? 'اسم المهمة بالعربية' : 'Mission Arabic Name')}
              className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-2 text-xs text-white"
            />
            <input
              type="text"
              value={mNameEn}
              onChange={(e) => setMNameEn(e.target.value)}
              placeholder={tx(isAr ? 'اسم المهمة بالإنجليزية' : 'Mission English Name')}
              className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-2 text-xs text-white"
            />
            <input
              type="text"
              value={mCode}
              onChange={(e) => setMCode(e.target.value)}
              placeholder={tx("ARTEMIS-IV")}
              className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-2 text-xs text-white"
            />
            <input
              type="text"
              value={mDestAr}
              onChange={(e) => setMDestAr(e.target.value)}
              placeholder={tx(isAr ? 'الوجهة بالعربية' : 'Destination (AR)')}
              className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-2 text-xs text-white"
            />
            <input
              type="text"
              value={mDestEn}
              onChange={(e) => setMDestEn(e.target.value)}
              placeholder={tx(isAr ? 'الوجهة بالإنجليزية' : 'Destination (EN)')}
              className="rounded-xl bg-slate-950/80 border border-white/15 px-3 py-2 text-xs text-white"
            />
            <AsyncButton
              type="submit"
              className="rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold py-2"
            >
              {tx(isAr ? 'حفظ المهمة' : 'Save Mission')}
            </AsyncButton>
          </AsyncForm>
        )}

        <div className="grid grid-cols-1 gap-3.5">
          {missions.map((m) => (
            <div
              key={m.id}
              className="p-4 rounded-2xl bg-slate-900/55 border border-white/10 flex flex-col gap-3"
            >
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <div className="flex items-center gap-2">
                    <h4 className="text-base font-bold text-white">
                      {tx(isAr ? m.nameAr : m.name)}
                    </h4>
                    <span className="text-[11px] font-bold px-2.5 py-0.5 rounded-full bg-cyan-500/15 text-cyan-300 border border-cyan-400/30">
                      {tx(m.code)}
                    </span>
                  </div>
                  <div className="flex flex-wrap items-center gap-4 text-xs text-slate-300 mt-1.5">
                    <span className="flex items-center gap-1">
                      <MapPin className="w-3.5 h-3.5 text-cyan-400" />
                      {tx(isAr ? m.destinationAr : m.destination)}
                    </span>
                    <span className="flex items-center gap-1" dir="ltr">
                      <Calendar className="w-3.5 h-3.5 text-blue-400" />
                      {tx(m.startDate)} → {tx(m.endDate)}
                    </span>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  {/* Update Mission Status (PUT /api/missions/{id}) */}
                  <select
                    value={m.status}
                    onChange={(e) =>
                      updateMissionStatus(
                        m.id,
                        e.target.value as 'Scheduled' | 'Active' | 'Completed' | 'Aborted'
                      )
                    }
                    className="rounded-xl bg-slate-950/75 border border-white/15 px-3 py-1.5 text-xs font-bold text-emerald-300"
                  >
                    <option value="Scheduled">{tx(isAr ? 'مجدولة' : 'Scheduled')}</option>
                    <option value="Active">{tx(isAr ? 'نشطة' : 'Active')}</option>
                    <option value="Completed">{tx(isAr ? 'مكتملة' : 'Completed')}</option>
                    <option value="Aborted">{tx(isAr ? 'ملغاة' : 'Aborted')}</option>
                  </select>

                  {missions.length > 1 && (
                    <AsyncButton
                      type="button"
                      onClick={() => deleteMissionById(m.id)}
                      className="p-2 rounded-xl bg-rose-500/15 hover:bg-rose-500/30 text-rose-300 transition-all"
                      title={tx(isAr ? 'حذف المهمة' : 'Delete Mission')}
                    >
                      <Trash2 className="w-4 h-4" />
                    </AsyncButton>
                  )}
                </div>
              </div>

              {/* Assign / Unassign Astronauts to Mission */}
              <div className="pt-3 border-t border-white/10 flex flex-wrap items-center gap-2">
                <span className="text-xs font-semibold text-slate-400">
                  {tx(isAr ? 'طاقم المهمة (انقر للتعيين أو الإلغاء):' : 'Assigned Crew (Click to toggle):')}
                </span>
                {crewMembers.map((crew) => {
                  const assigned = m.crewIds?.includes(crew.id);
                  return (
                    <AsyncButton
                      key={crew.id}
                      type="button"
                      onClick={() =>
                        assigned
                          ? unassignCrewFromMission(m.id, crew.id)
                          : assignCrewToMission(m.id, crew.id)
                      }
                      className={`px-3 py-1 rounded-full text-xs font-bold border transition-all ${
                        assigned
                          ? 'bg-blue-500/20 border-blue-400/40 text-blue-200'
                          : 'bg-slate-950/50 border-white/10 text-slate-400 hover:text-white'
                      }`}
                    >
                      {tx(isAr ? crew.fullNameAr : crew.fullName)} {tx(assigned ? '✓' : '+')}
                    </AsyncButton>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

