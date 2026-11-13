import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import { AsyncForm } from "../AsyncForm";
import React, { useState } from 'react';
import { Smile, Plus, Clock, CheckCircle2, HeartHandshake } from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';

export const MoodWellnessPage: React.FC = () => {
  const tx = useUiText();
  const { language } = useLanguage();
  const { moodEntries, recordMood } = useTelemetry();
  const isAr = language === 'ar';

  const [moodScore, setMoodScore] = useState(4);
  const [stressLevel, setStressLevel] = useState(2);
  const [emotion, setEmotion] = useState('Focused & Optimistic');
  const [notes, setNotes] = useState('');
  const [savedToast, setSavedToast] = useState(false);

  const latestMood = moodEntries[0];

  const handleSaveMood = async (e: React.FormEvent) => {
    e.preventDefault();
    setSavedToast(false);
    await recordMood({
      moodScore,
      stressLevel,
      dominantEmotion: emotion,
      notes: notes.trim() || (isAr ? 'تركيز ذهني عالٍ واستقرار نفسي ممتاز' : 'High mental clarity and calm focus'),
    });
    setNotes('');
    setSavedToast(true);
    setTimeout(() => setSavedToast(false), 3000);
  };

  return (
    <div className="flex flex-col gap-3.5 animate-fade-in">
      {/* Top Summary Cards */}
      {latestMood && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div className="glass-panel rounded-2xl p-4 text-center">
            <p className="text-xs text-slate-300 mb-1">
              {tx(isAr ? 'مؤشر المزاج العام' : 'Current Mood Score')}
            </p>
            <p className="text-2xl font-extrabold text-emerald-300" dir="ltr">
              {tx(latestMood.moodScore)} / {latestMood.moodScaleMaximum || 10}
            </p>
          </div>
          <div className="glass-panel rounded-2xl p-4 text-center">
            <p className="text-xs text-slate-300 mb-1">
              {tx(isAr ? 'مستوى التوتر' : 'Stress Level')}
            </p>
            <p className="text-2xl font-extrabold text-cyan-300" dir="ltr">
              {tx(latestMood.stressLevel)} / 10
            </p>
          </div>
          <div className="glass-panel rounded-2xl p-4 text-center">
            <p className="text-xs text-slate-300 mb-1">
              {tx(isAr ? 'الحالة الشعورية السائدة' : 'Dominant Emotion')}
            </p>
            <p className="text-base font-extrabold text-white mt-1">
              {tx(latestMood.dominantEmotion)}
            </p>
          </div>
        </div>
      )}

      {/* Main Content: Check-In Form + Mood History */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-3.5">
        <AsyncForm
          onSubmit={handleSaveMood}
          className="lg:col-span-5 glass-panel rounded-[22px] p-5 flex flex-col justify-between gap-4"
        >
          <div>
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2.5">
                <div className="w-9 h-9 rounded-xl bg-emerald-500/20 border border-emerald-400/30 flex items-center justify-center text-emerald-300">
                  <Smile className="w-4 h-4" />
                </div>
                <div>
                  <h3 className="text-base font-bold text-white">
                    {tx(isAr ? 'تسجيل التقييم النفسي اليومي' : 'Daily Mood & Stress Check-In')}
                  </h3>
                  <p className="text-xs text-slate-300">
                    {tx(isAr ? 'متابعة التركيز الذهني والتكيف مع بيئة العزلة' : 'Track mental focus and mission adaptation')}
                  </p>
                </div>
              </div>
              {savedToast && (
                <span className="text-xs font-bold text-emerald-300 bg-emerald-500/15 border border-emerald-400/30 px-2.5 py-1 rounded-full flex items-center gap-1">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  {tx(isAr ? 'تم الحفظ' : 'Saved')}
                </span>
              )}
            </div>

            <div className="space-y-3.5">
              <div>
                <div className="flex justify-between text-xs font-semibold text-slate-300 mb-1">
                  <span>{tx(isAr ? 'درجة المزاج (١ - ٥)' : 'Mood Score (1 - 5)')}</span>
                  <span className="text-emerald-300 font-bold">{tx(moodScore)} / 5</span>
                </div>
                <input
                  type="range"
                  min={1}
                  max={5}
                  value={moodScore}
                  onChange={(e) => setMoodScore(Number(e.target.value))}
                  className="w-full accent-emerald-400"
                />
              </div>

              <div>
                <div className="flex justify-between text-xs font-semibold text-slate-300 mb-1">
                  <span>{tx(isAr ? 'مستوى التوتر والضغط (1 - 10)' : 'Stress Level (1 - 10)')}</span>
                  <span className="text-cyan-300 font-bold">{tx(stressLevel)} / 10</span>
                </div>
                <input
                  type="range"
                  min={1}
                  max={10}
                  value={stressLevel}
                  onChange={(e) => setStressLevel(Number(e.target.value))}
                  className="w-full accent-cyan-400"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                  {tx(isAr ? 'الشعور السائد اليوم' : 'Dominant Emotion')}
                </label>
                <select
                  value={emotion}
                  onChange={(e) => setEmotion(e.target.value)}
                  className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3 py-2 text-xs font-semibold text-white"
                >
                  <option value="Focused & Optimistic">
                    {tx(isAr ? 'تركيز وتفاؤل عالٍ' : 'Focused & Optimistic')}
                  </option>
                  <option value="Calm & Balanced">
                    {tx(isAr ? 'هدوء وتوازن نفسي' : 'Calm & Balanced')}
                  </option>
                  <option value="Energetic & Motivated">
                    {tx(isAr ? 'نشاط وحماس للمهمة' : 'Energetic & Motivated')}
                  </option>
                  <option value="Mild Fatigue">
                    {tx(isAr ? 'إجهاد ذهني خفيف' : 'Mild Fatigue')}
                  </option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                  {tx(isAr ? 'ملاحظات إضافية' : 'Personal Notes')}
                </label>
                <input
                  type="text"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder={tx(isAr ? 'اكتب ملاحظاتك حول حالتك الذهنية...' : 'Add brief notes...')}
                  className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3.5 py-2 text-xs text-white placeholder-slate-400"
                />
              </div>
            </div>
          </div>

          <AsyncButton
            type="submit"
            className="w-full py-2.5 px-4 rounded-xl bg-gradient-to-r from-emerald-600 to-cyan-500 hover:from-emerald-500 hover:to-cyan-400 text-white font-bold text-sm shadow-lg transition-all flex items-center justify-center gap-2"
          >
            <Plus className="w-4 h-4" />
            <span>{tx(isAr ? 'حفظ التقييم النفسي' : 'Save Mood Check-In')}</span>
          </AsyncButton>
        </AsyncForm>

        {/* Psychological History Log */}
        <div className="lg:col-span-7 glass-panel rounded-[22px] p-5 flex flex-col justify-between gap-4">
          <div>
            <div className="flex items-center gap-2.5 mb-4">
              <div className="w-9 h-9 rounded-xl bg-cyan-500/20 border border-cyan-400/30 flex items-center justify-center text-cyan-300">
                <Clock className="w-4 h-4" />
              </div>
              <div>
                <h3 className="text-base font-bold text-white">
                  {tx(isAr ? 'سجل التقييمات النفسية السابقة' : 'Psychological Check-In History')}
                </h3>
                <p className="text-xs text-slate-300">
                  {tx(isAr
                    ? `إجمالي التقييمات المسجلة: ${moodEntries.length}`
                    : `Total recorded check-ins: ${moodEntries.length}`)}
                </p>
              </div>
            </div>

            <div className="space-y-2.5 max-h-[280px] overflow-y-auto pr-1">
              {moodEntries.map((me) => (
                <div
                  key={me.id}
                  className="p-3.5 rounded-xl bg-slate-900/55 border border-white/10 flex items-center justify-between gap-3"
                >
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-bold text-white">{tx(me.dominantEmotion)}</span>
                      <span className="text-[11px] font-bold px-2.5 py-0.5 rounded-full bg-emerald-500/15 text-emerald-300 border border-emerald-400/30">
                        {tx(isAr ? `المزاج ${me.moodScore}/${me.moodScaleMaximum || 10}` : `Mood ${me.moodScore}/${me.moodScaleMaximum || 10}`)}
                      </span>
                    </div>
                    <p className="text-xs text-slate-300 mt-1">{tx(me.notes)}</p>
                  </div>
                  <div className="text-end shrink-0">
                    <span className="text-xs font-bold text-cyan-300" dir="ltr">
                      {tx(isAr ? `التوتر: ${me.stressLevel}/10` : `Stress: ${me.stressLevel}/10`)}
                    </span>
                    <p className="text-[11px] text-slate-400 mt-0.5">
                      {tx(new Date(me.recordedAt).toLocaleTimeString(isAr ? 'ar-EG' : 'en-US', {
                        hour: '2-digit',
                        minute: '2-digit',
                      }))}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="p-3 rounded-xl bg-slate-950/55 border border-white/10 flex items-center gap-2.5 text-xs text-slate-300">
            <HeartHandshake className="w-4 h-4 text-emerald-400 shrink-0" />
            <span>
              {tx(isAr
                ? 'التواصل اليومي مع أفراد الطاقم وممارسة تمارين الاسترخاء يعززان الاستقرار النفسي خلال المهام الطويلة.'
                : 'Daily social connection with crewmates and relaxation routines support mental resilience.')}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};

