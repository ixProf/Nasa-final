import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import { AsyncForm } from "../AsyncForm";
import React, { useState } from 'react';
import {
  ShieldAlert,
  CheckCircle2,
  AlertTriangle,
  PlusCircle,
  Siren,
  Wind,
  Flame,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';

interface EmergencyProtocol {
  code: string;
  titleAr: string;
  titleEn: string;
  triggerAr: string;
  triggerEn: string;
  stepsAr: string[];
  stepsEn: string[];
  escalationAr: string;
  escalationEn: string;
}

const emergencyProcedures: EmergencyProtocol[] = [
  {
    code: 'EP-HYPOXIA-01',
    titleAr: 'بروتوكول الاستجابة لنقص الأكسجين الحاد (SpO2 < 90%)',
    titleEn: 'Suspected Hypoxia & Low Oxygen Response Protocol (SpO2 < 90%)',
    triggerAr: 'يفعل فوراً عند انخفاض تشبع الأكسجين عن 90% أو الشعور بضيق تنفس حاد أو زرقة.',
    triggerEn: 'Triggered immediately when SpO2 drops below 90% or acute dyspnea/cyanosis occurs.',
    stepsAr: [
      '1. إيقاف أي مجهود بدني فوراً وتثبيت رائد الفضاء في وضعية مستقرة.',
      '2. ارتداء قناع الأكسجين المحمول (PBA) وتزويد الأكسجين بنسبة 100% بمعدل تدفق عالٍ.',
      '3. التحقق من ضغط المقصورة ومستوى الأكسجين الجزئي (ppO2) عبر لوحة دعم الحياة.',
      '4. إعادة قياس الأكسجين ونبض القلب بعد دقيقتين ومراقبة مستوى الوعي.',
    ],
    stepsEn: [
      '1. Cease all physical activity immediately and secure astronaut in a stable posture.',
      '2. Don Portable Breathing Apparatus (PBA) delivering 100% O2 at high flow.',
      '3. Verify cabin total pressure and partial pressure of O2 (ppO2) on ECLSS console.',
      '4. Re-measure SpO2 and Heart Rate after 2 minutes and monitor consciousness.',
    ],
    escalationAr:
      'فتح قناة طوارئ مباشرة مع طبيب الرحلة وقائد المهمة إذا لم يرتفع الأكسجين فوق 94% خلال 3 دقائق.',
    escalationEn:
      'Establish immediate emergency voice/chat link with Flight Surgeon if SpO2 stays below 94% after 3 minutes.',
  },
  {
    code: 'EP-CO2-02',
    titleAr: 'بروتوكول التعامل مع ارتفاع ثاني أكسيد الكربون وصداع المقصورة',
    titleEn: 'Elevated Cabin CO2 & Hypercapnia Emergency Protocol',
    triggerAr: 'يفعل عند تجاوز ثاني أكسيد الكربون 4 mmHg أو ظهور صداع جماعي وتسارع في التنفس.',
    triggerEn: 'Triggered when cabin ppCO2 exceeds 4 mmHg or crew experiences headache and tachypnea.',
    stepsAr: [
      '1. الانتقال الفوري من المناطق المغلقة إلى وحدة ذات تهوية عالية وتشغيل المراوح الشخصية.',
      '2. تفعيل وحدة تنقية ثاني أكسيد الكربون الاحتياطية (CDRA / LiOH canisters).',
      '3. تسجيل معدل التنفس (RESP) ومستوى الأكسجين وضغط الدم لجميع أفراد الطاقم.',
      '4. استخدام الأكسجين المساعد في حال وجود دوار أو تشوش ذهني.',
    ],
    stepsEn: [
      '1. Relocate immediately to a high-airflow module and activate personal ventilation fans.',
      '2. Switch on backup CO2 scrubbing system (CDRA / LiOH canisters).',
      '3. Log Respiratory Rate (RESP), SpO2, and Blood Pressure for all crew members.',
      '4. Administer supplemental O2 if dizziness or confusion is present.',
    ],
    escalationAr:
      'إبلاغ مركز التحكم الأرضي وطبيب الرحلة فوراً при ظهور تشوش ذهني أو معدل تنفس يتجاوز 24 نفس/دقيقة.',
    escalationEn:
      'Alert Mission Control and Flight Surgeon immediately if confusion occurs or respiratory rate exceeds 24 breaths/min.',
  },
];

export const AlertsEmergencyPage: React.FC = () => {
  const tx = useUiText();
  const { language } = useLanguage();
  const { alerts, acknowledgeAlert, resolveAlert, triggerManualAlert, currentRole } =
    useTelemetry();
  const isAr = language === 'ar';

  const [alertFilter, setAlertFilter] = useState<'ALL' | 'Active' | 'Acknowledged' | 'Resolved'>('ALL');
  const [alertTitle, setAlertTitle] = useState('');
  const [alertMsg, setAlertMsg] = useState('');
  const [alertSeverity, setAlertSeverity] = useState<'MedicalAttention' | 'Emergency'>('MedicalAttention');

  const handleManualAlert = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!alertTitle.trim()) return;
    await triggerManualAlert(
      alertTitle.trim(),
      alertMsg.trim() || (isAr ? 'طلب متابعة طبية عاجلة' : 'Urgent medical assistance requested'),
      alertSeverity
    );
    setAlertTitle('');
    setAlertMsg('');
  };

  const filteredAlerts =
    alertFilter === 'ALL' ? alerts : alerts.filter((a) => a.status === alertFilter);

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-3.5 animate-fade-in">
      {/* Left Column: Health Alerts & Manual Alert Trigger (/api/alerts) */}
      <div className="lg:col-span-6 glass-panel rounded-[22px] p-5 flex flex-col justify-between gap-4">
        <div>
          <div className="flex flex-wrap items-center justify-between gap-2 mb-4">
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-xl bg-amber-500/20 border border-amber-400/30 flex items-center justify-center text-amber-300">
                <ShieldAlert className="w-4 h-4" />
              </div>
              <div>
                <h3 className="text-base font-bold text-white">
                  {tx(isAr ? 'مركز التنبيهات الصحية' : 'Health Alerts Center')}
                </h3>
                <p className="text-xs text-slate-300">
                  {tx(isAr
                    ? 'متابعة التنبيهات النشطة وإرسال نداء طبي عاجل'
                    : 'Monitor active telemetry alerts and dispatch urgent medical notifications')}
                </p>
              </div>
            </div>

            <div className="flex items-center gap-1 bg-slate-900/70 p-1 rounded-xl border border-white/10">
              {(
                [
                  { key: 'ALL', ar: 'الكل', en: 'All' },
                  { key: 'Active', ar: 'نشط', en: 'Active' },
                  { key: 'Acknowledged', ar: 'مؤكد', en: 'Ack' },
                  { key: 'Resolved', ar: 'محلول', en: 'Resolved' },
                ] as const
              ).map((tab) => (
                <AsyncButton
                  key={tab.key}
                  type="button"
                  onClick={() => setAlertFilter(tab.key)}
                  className={`px-2.5 py-1 rounded-lg text-[11px] font-bold transition-all ${
                    alertFilter === tab.key
                      ? 'bg-amber-500 text-slate-950'
                      : 'text-slate-300 hover:text-white'
                  }`}
                >
                  {tx(isAr ? tab.ar : tab.en)}
                </AsyncButton>
              ))}
            </div>
          </div>

          <div className="space-y-2.5 max-h-[250px] overflow-y-auto pr-1 mb-4">
            {filteredAlerts.length === 0 ? (
              <div className="p-6 rounded-xl bg-slate-900/45 border border-white/10 text-center">
                <CheckCircle2 className="w-8 h-8 text-emerald-400 mx-auto mb-2" />
                <p className="text-sm font-bold text-white">
                  {tx(isAr ? 'جميع المؤشرات مستقرة' : 'All Vitals Are Normal')}
                </p>
                <p className="text-xs text-slate-300 mt-0.5">
                  {tx(isAr
                    ? 'لا توجد أي تنبيهات صحية ضمن هذا التصنيف'
                    : 'No health alerts in this category')}
                </p>
              </div>
            ) : (
              filteredAlerts.map((al) => (
                <div
                  key={al.id}
                  className="p-3.5 rounded-xl bg-slate-900/60 border border-amber-400/30 flex items-start justify-between gap-3"
                >
                  <div>
                    <div className="flex items-center gap-2">
                      <AlertTriangle className="w-4 h-4 text-amber-400 shrink-0" />
                      <h4 className="text-xs font-bold text-white">{tx(al.title)}</h4>
                    </div>
                    <p className="text-xs text-slate-300 mt-1">{tx(al.message)}</p>
                  </div>
                  <div className="flex items-center gap-1.5 shrink-0">
                    {currentRole === 'Monitor' ? (
                      <>
                        {al.status === 'Active' && (
                          <AsyncButton
                            type="button"
                            onClick={() => acknowledgeAlert(al.id)}
                            className="px-2.5 py-1 rounded-lg bg-amber-500/20 hover:bg-amber-500/30 text-amber-200 text-[11px] font-bold"
                          >
                            {tx(isAr ? 'تأكيد' : 'Ack')}
                          </AsyncButton>
                        )}
                        {al.status !== 'Resolved' && (
                          <AsyncButton
                            type="button"
                            onClick={() =>
                              resolveAlert(
                                al.id,
                                isAr ? 'تمت المراجعة واستقرار الحالة' : 'Reviewed and stabilized'
                              )
                            }
                            className="px-2.5 py-1 rounded-lg bg-emerald-500/20 hover:bg-emerald-500/30 text-emerald-200 text-[11px] font-bold"
                          >
                            {tx(isAr ? 'إنهاء' : 'Resolve')}
                          </AsyncButton>
                        )}
                      </>
                    ) : (
                      <span className="px-2.5 py-1 rounded-lg bg-white/5 border border-white/10 text-slate-300 text-[10px] font-bold">
                        {tx(al.status === 'Active'
                          ? isAr
                            ? 'قيد مراجعة الطبيب'
                            : 'Pending Surgeon'
                          : al.status === 'Acknowledged'
                          ? isAr
                            ? 'تم استلام الطبيب'
                            : 'Acknowledged'
                          : isAr
                          ? 'تم الحل'
                          : 'Resolved')}
                      </span>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Manual Alert Form (POST /api/alerts/manual) */}
        <AsyncForm
          onSubmit={handleManualAlert}
          className="p-3.5 rounded-xl bg-slate-900/55 border border-white/10 space-y-2.5"
        >
          <p className="text-xs font-bold text-white flex items-center gap-1.5">
            <PlusCircle className="w-3.5 h-3.5 text-rose-400" />
            <span>{tx(isAr ? 'إرسال تنبيه طبي يدوي' : 'Trigger Manual Medical Alert')}</span>
          </p>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
            <input
              type="text"
              required
              value={alertTitle}
              onChange={(e) => setAlertTitle(e.target.value)}
              placeholder={tx(isAr ? 'عنوان التنبيه...' : 'Alert title...')}
              className="sm:col-span-2 rounded-xl bg-slate-950/70 border border-white/15 px-3 py-1.5 text-xs text-white placeholder-slate-400"
            />
            <select
              value={alertSeverity}
              onChange={(e) =>
                setAlertSeverity(e.target.value as 'MedicalAttention' | 'Emergency')
              }
              className="rounded-xl bg-slate-950/70 border border-white/15 px-2.5 py-1.5 text-xs font-semibold text-white"
            >
              <option value="MedicalAttention">{tx(isAr ? 'متابعة طبية' : 'Attention')}</option>
              <option value="Emergency">{tx(isAr ? 'طوارئ' : 'Emergency')}</option>
            </select>
          </div>
          <div className="flex gap-2">
            <input
              type="text"
              value={alertMsg}
              onChange={(e) => setAlertMsg(e.target.value)}
              placeholder={tx(isAr ? 'تفاصيل الحالة باختصار...' : 'Brief description...')}
              className="flex-1 rounded-xl bg-slate-950/70 border border-white/15 px-3 py-1.5 text-xs text-white placeholder-slate-400"
            />
            <AsyncButton
              type="submit"
              className="px-4 py-1.5 rounded-xl bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold transition-all"
            >
              {tx(isAr ? 'إرسال' : 'Send')}
            </AsyncButton>
          </div>
        </AsyncForm>
      </div>

      {/* Right Column: Standard Emergency Procedures (EP-HYPOXIA-01 & EP-CO2-02) */}
      <div className="lg:col-span-6 glass-panel rounded-[22px] p-5 flex flex-col justify-between gap-4">
        <div>
          <div className="flex items-center gap-2.5 mb-4">
            <div className="w-9 h-9 rounded-xl bg-rose-500/20 border border-rose-400/30 flex items-center justify-center text-rose-300">
              <Siren className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                {tx(isAr ? 'بروتوكولات الطوارئ الطبية القياسية' : 'Standard Emergency Procedures')}
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? 'خطوات الاستجابة الفورية لحالات نقص الأكسجين وارتفاع ثاني أكسيد الكربون'
                  : 'Step-by-step response checklists for acute cabin and physiological emergencies')}
              </p>
            </div>
          </div>

          <div className="space-y-3.5">
            {emergencyProcedures.map((ep, idx) => (
              <div
                key={ep.code}
                className="p-4 rounded-2xl bg-slate-900/60 border border-rose-400/25 space-y-2.5"
              >
                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2">
                    {idx === 0 ? (
                      <Wind className="w-4 h-4 text-cyan-400 shrink-0" />
                    ) : (
                      <Flame className="w-4 h-4 text-amber-400 shrink-0" />
                    )}
                    <h4 className="text-sm font-bold text-white">
                      {tx(isAr ? ep.titleAr : ep.titleEn)}
                    </h4>
                  </div>
                  <span className="text-[10px] font-bold px-2.5 py-0.5 rounded-full bg-rose-500/20 text-rose-300 border border-rose-400/30">
                    {tx(ep.code)}
                  </span>
                </div>

                <p className="text-xs text-amber-200/90 leading-relaxed">
                  {tx(isAr ? ep.triggerAr : ep.triggerEn)}
                </p>

                <div className="space-y-1.5 pt-1">
                  {(isAr ? ep.stepsAr : ep.stepsEn).map((step, sIdx) => (
                    <div
                      key={sIdx}
                      className="p-2 rounded-xl bg-slate-950/60 border border-white/5 text-xs text-slate-200 leading-relaxed"
                    >
                      {tx(step)}
                    </div>
                  ))}
                </div>

                <p className="text-xs text-rose-300 font-semibold pt-1">
                  {tx(isAr ? ep.escalationAr : ep.escalationEn)}
                </p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

