import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import { AsyncForm } from "../AsyncForm";
import React, { useRef, useState } from 'react';
import { Moon, Plus, Clock, Sparkles, CheckCircle2 } from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { SleepQuality } from '../../types/telemetry';

export const SleepTrackingPage: React.FC = () => {
  const tx = useUiText();
  const { language } = useLanguage();
  const { sleepRecords, recordSleep } = useTelemetry();
  const isAr = language === 'ar';

  const [durationHours, setDurationHours] = useState('7.75');
  const [quality, setQuality] = useState<SleepQuality>('Excellent');
  const [deepMin, setDeepMin] = useState('115');
  const [remMin, setRemMin] = useState('105');
  const [lightMin, setLightMin] = useState('225');
  const [awakeMin, setAwakeMin] = useState('20');
  const [efficiency, setEfficiency] = useState('95.7');
  const [notes, setNotes] = useState('');
  const [savedToast, setSavedToast] = useState(false);
  const sleepWindow = useRef<{ key: string; start: string; end: string } | null>(null);

  const latestSleep = sleepRecords[0];

  const handleSaveSleep = async (e: React.FormEvent) => {
    e.preventDefault();
    setSavedToast(false);
    const hrs = parseFloat(durationHours) || 7.5;
    const totalMin = Math.round(hrs * 60);
    const key = JSON.stringify([durationHours, quality, deepMin, remMin, lightMin, awakeMin, efficiency, notes]);
    if (sleepWindow.current?.key !== key) sleepWindow.current = {
      key, start: new Date(Date.now() - totalMin * 60000).toISOString(), end: new Date().toISOString()
    };
    await recordSleep({
      startTime: sleepWindow.current.start,
      endTime: sleepWindow.current.end,
      durationHours: hrs,
      quality,
      deepSleepMinutes: parseInt(deepMin, 10) || 110,
      remSleepMinutes: parseInt(remMin, 10) || 100,
      lightSleepMinutes: parseInt(lightMin, 10) || 220,
      awakeMinutes: parseInt(awakeMin, 10) || 15,
      sleepEfficiencyPercentage: parseFloat(efficiency) || 95,
      notes: notes.trim() || (isAr ? 'نوم عميق ومنتظم في كبسولة النوم' : 'Restorative orbital sleep cycle'),
    });
    setNotes('');
    sleepWindow.current = null;
    setSavedToast(true);
    setTimeout(() => setSavedToast(false), 3000);
  };

  const qualityLabel = (q: SleepQuality) => {
    const map: Record<SleepQuality, { ar: string; en: string; cls: string }> = {
      Excellent: {
        ar: 'ممتاز',
        en: 'Excellent',
        cls: 'bg-emerald-500/15 text-emerald-300 border-emerald-400/30',
      },
      Good: {
        ar: 'جيد جداً',
        en: 'Good',
        cls: 'bg-cyan-500/15 text-cyan-300 border-cyan-400/30',
      },
      Fair: {
        ar: 'متوسط',
        en: 'Fair',
        cls: 'bg-amber-500/15 text-amber-300 border-amber-400/30',
      },
      Poor: {
        ar: 'ضعيف',
        en: 'Poor',
        cls: 'bg-rose-500/15 text-rose-300 border-rose-400/30',
      },
    };
    return map[q] || map.Good;
  };

  return (
    <div className="flex flex-col gap-3.5 animate-fade-in">
      {/* Top Summary Metrics Row */}
      {latestSleep && (
        <div className="grid grid-cols-2 sm:grid-cols-5 gap-3">
          <div className="glass-panel rounded-2xl p-3.5 text-center">
            <p className="text-xs text-slate-300 mb-1">
              {tx(isAr ? 'إجمالي ساعات النوم' : 'Total Duration')}
            </p>
            <p className="text-xl font-extrabold text-white" dir="ltr">
              {tx(latestSleep.durationHours)}{tx("h ")}</p>
          </div>
          <div className="glass-panel rounded-2xl p-3.5 text-center">
            <p className="text-xs text-slate-300 mb-1">
              {tx(isAr ? 'كفاءة النوم' : 'Sleep Efficiency')}
            </p>
            <p className="text-xl font-extrabold text-cyan-300" dir="ltr">
              {tx(latestSleep.sleepEfficiencyPercentage)}%
            </p>
          </div>
          <div className="glass-panel rounded-2xl p-3.5 text-center">
            <p className="text-xs text-slate-300 mb-1">
              {tx(isAr ? 'النوم العميق' : 'Deep Sleep')}
            </p>
            <p className="text-xl font-extrabold text-indigo-300" dir="ltr">
              {tx(latestSleep.deepSleepMinutes)} {tx(" min ")}</p>
          </div>
          <div className="glass-panel rounded-2xl p-3.5 text-center">
            <p className="text-xs text-slate-300 mb-1">
              {tx(isAr ? 'حركة العين السريعة (REM)' : 'REM Sleep')}
            </p>
            <p className="text-xl font-extrabold text-blue-300" dir="ltr">
              {tx(latestSleep.remSleepMinutes)} {tx(" min ")}</p>
          </div>
          <div className="glass-panel rounded-2xl p-3.5 text-center col-span-2 sm:col-span-1">
            <p className="text-xs text-slate-300 mb-1">
              {tx(isAr ? 'النوم الخفيف' : 'Light Sleep')}
            </p>
            <p className="text-xl font-extrabold text-emerald-300" dir="ltr">
              {tx(latestSleep.lightSleepMinutes)} {tx(" min ")}</p>
          </div>
        </div>
      )}

      {/* Main Content: Record Sleep Form + Sleep History */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-3.5">
        <AsyncForm
          onSubmit={handleSaveSleep}
          className="lg:col-span-5 glass-panel rounded-[22px] p-5 flex flex-col justify-between gap-4"
        >
          <div>
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2.5">
                <div className="w-9 h-9 rounded-xl bg-indigo-500/20 border border-indigo-400/30 flex items-center justify-center text-indigo-300">
                  <Moon className="w-4 h-4" />
                </div>
                <div>
                  <h3 className="text-base font-bold text-white">
                    {tx(isAr ? 'تسجيل جلسة نوم جديدة' : 'Log New Sleep Session')}
                  </h3>
                  <p className="text-xs text-slate-300">
                    {tx(isAr ? 'متابعة انتظام الساعة البيولوجية ومراحل النوم' : 'Track circadian rhythm and sleep stages')}
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

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">
                  {tx(isAr ? 'مدة النوم (ساعات)' : 'Duration (Hours)')}
                </label>
                <input
                  type="number"
                  step="0.25"
                  value={durationHours}
                  onChange={(e) => setDurationHours(e.target.value)}
                  className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3 py-2 text-xs font-bold text-white"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">
                  {tx(isAr ? 'جودة النوم' : 'Sleep Quality')}
                </label>
                <select
                  value={quality}
                  onChange={(e) => setQuality(e.target.value as SleepQuality)}
                  className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3 py-2 text-xs font-semibold text-white"
                >
                  <option value="Excellent">{tx(isAr ? 'ممتاز' : 'Excellent')}</option>
                  <option value="Good">{tx(isAr ? 'جيد جداً' : 'Good')}</option>
                  <option value="Fair">{tx(isAr ? 'متوسط' : 'Fair')}</option>
                  <option value="Poor">{tx(isAr ? 'ضعيف' : 'Poor')}</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">
                  {tx(isAr ? 'النوم العميق (دقيقة)' : 'Deep Sleep (min)')}
                </label>
                <input
                  type="number"
                  value={deepMin}
                  onChange={(e) => setDeepMin(e.target.value)}
                  className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3 py-2 text-xs font-bold text-white"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">
                  {tx(isAr ? 'نوم الأحلام REM (دقيقة)' : 'REM Sleep (min)')}
                </label>
                <input
                  type="number"
                  value={remMin}
                  onChange={(e) => setRemMin(e.target.value)}
                  className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3 py-2 text-xs font-bold text-white"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">
                  {tx(isAr ? 'النوم الخفيف (دقيقة)' : 'Light Sleep (min)')}
                </label>
                <input
                  type="number"
                  value={lightMin}
                  onChange={(e) => setLightMin(e.target.value)}
                  className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3 py-2 text-xs font-bold text-white"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">
                  {tx(isAr ? 'كفاءة النوم %' : 'Efficiency %')}
                </label>
                <input
                  type="number"
                  step="0.1"
                  value={efficiency}
                  onChange={(e) => setEfficiency(e.target.value)}
                  className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3 py-2 text-xs font-bold text-white"
                />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-semibold text-slate-300 mb-1">
                  {tx(isAr ? 'ملاحظات حول النوم' : 'Sleep Notes')}
                </label>
                <input
                  type="text"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder={tx(isAr ? 'أضف ملاحظة حول راحة النوم...' : 'Add observation...')}
                  className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3 py-2 text-xs text-white placeholder-slate-400"
                />
              </div>
            </div>
          </div>

          <AsyncButton
            type="submit"
            className="w-full py-2.5 px-4 rounded-xl bg-gradient-to-r from-indigo-600 to-cyan-500 hover:from-indigo-500 hover:to-cyan-400 text-white font-bold text-sm shadow-lg transition-all flex items-center justify-center gap-2"
          >
            <Plus className="w-4 h-4" />
            <span>{tx(isAr ? 'حفظ سجل النوم' : 'Save Sleep Record')}</span>
          </AsyncButton>
        </AsyncForm>

        {/* Sleep Sessions History */}
        <div className="lg:col-span-7 glass-panel rounded-[22px] p-5 flex flex-col justify-between gap-4">
          <div>
            <div className="flex items-center gap-2.5 mb-4">
              <div className="w-9 h-9 rounded-xl bg-cyan-500/20 border border-cyan-400/30 flex items-center justify-center text-cyan-300">
                <Clock className="w-4 h-4" />
              </div>
              <div>
                <h3 className="text-base font-bold text-white">
                  {tx(isAr ? 'سجل جلسات النوم السابقة' : 'Sleep Sessions History')}
                </h3>
                <p className="text-xs text-slate-300">
                  {tx(isAr
                    ? `إجمالي الجلسات المسجلة: ${sleepRecords.length}`
                    : `Total logged sleep sessions: ${sleepRecords.length}`)}
                </p>
              </div>
            </div>

            <div className="space-y-2.5 max-h-[300px] overflow-y-auto pr-1">
              {sleepRecords.map((sr) => {
                const qBadge = qualityLabel(sr.quality);
                return (
                  <div
                    key={sr.id}
                    className="p-3.5 rounded-xl bg-slate-900/55 border border-white/10 flex flex-col gap-2"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-extrabold text-white" dir="ltr">
                          {tx(sr.durationHours)}{tx("h ")}</span>
                        <span
                          className={`text-[11px] font-bold px-2.5 py-0.5 rounded-full border ${qBadge.cls}`}
                        >
                          {tx(isAr ? qBadge.ar : qBadge.en)}
                        </span>
                      </div>
                      <span className="text-xs font-bold text-cyan-300" dir="ltr">
                        {tx(sr.sleepEfficiencyPercentage)}% {tx(isAr ? 'كفاءة' : 'Efficiency')}
                      </span>
                    </div>
                    <p className="text-xs text-slate-300">{tx(sr.notes)}</p>
                    <div className="flex flex-wrap items-center gap-3 text-[11px] text-slate-400 pt-1 border-t border-white/5" dir="ltr">
                      <span>{tx("Deep: ")}{tx(sr.deepSleepMinutes)}{tx("m")}</span>
                      <span>{tx("REM: ")}{tx(sr.remSleepMinutes)}{tx("m")}</span>
                      <span>{tx("Light: ")}{tx(sr.lightSleepMinutes)}{tx("m")}</span>
                      <span>{tx("Awake: ")}{tx(sr.awakeMinutes)}{tx("m")}</span>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="p-3 rounded-xl bg-slate-950/55 border border-white/10 flex items-center gap-2.5 text-xs text-slate-300">
            <Sparkles className="w-4 h-4 text-indigo-400 shrink-0" />
            <span>
              {tx(isAr
                ? 'ينصح بالحفاظ على 7 إلى 8 ساعات نوم يومياً مع تقليل الإضاءة الزرقاء قبل النوم بـ 90 دقيقة.'
                : 'Maintain 7–8 hours of sleep daily and dim blue-spectrum cabin lighting 90 minutes before sleep.')}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};

