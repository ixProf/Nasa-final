import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import { reportError } from "../../services/errors";
import React, { useState, useEffect } from 'react';
import {
  ShieldCheck,
  Users,
  Rocket,
  ShieldAlert,
  AlertTriangle,
  Activity,
  Eye,
  Heart,
  Wind,
  Droplets,
  Thermometer,
  Moon,
  Smile,
  CheckCircle2,
  MessageSquareText,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { api } from '../../services/api';
import { ActivePage } from '../../types/telemetry';

interface MonitorDashboardPageProps {
  onNavigate?: (page: ActivePage) => void;
}

export const MonitorDashboardPage: React.FC<MonitorDashboardPageProps> = ({ onNavigate }) => {
  const tx = useUiText();
  const { language } = useLanguage();
  const { crewMembers, missions, alerts, acknowledgeAlert, resolveAlert } = useTelemetry();
  const isAr = language === 'ar';

  const [monitorData, setMonitorData] = useState<any | null>(null);
  const [selectedAstronautId, setSelectedAstronautId] = useState<number>(1);
  const [snapshot, setSnapshot] = useState<any | null>(null);

  useEffect(() => {
    api
      .getMonitorDashboard()
      .then((res) => {
        if (res) setMonitorData(res);
      })
      .catch(reportError);
  }, []);

  useEffect(() => {
    api
      .getAstronautSnapshot(selectedAstronautId)
      .then((res) => {
        if (res) setSnapshot(res);
      })
      .catch(() => {
        const localCrew =
          crewMembers.find((c) => c.id === selectedAstronautId) || crewMembers[0];
        setSnapshot({
          astronautId: localCrew?.id || 1,
          fullName: isAr ? localCrew?.fullNameAr : localCrew?.fullName,
          astronautCode: localCrew?.astronautCode || 'AST-001',
          missionName: localCrew?.missionName || 'Ares III Expedition',
          overallStatus: localCrew?.overallStatus || 'Routine',
          latestSleep: { durationHours: 7.75, sleepEfficiencyPercentage: 95.7, quality: 'Excellent' },
          latestMood: { moodScore: 9, stressLevel: 2, dominantEmotion: 'Focused & Optimistic' },
          unreadChatMessagesCount: 1,
        });
      });
  }, [selectedAstronautId, crewMembers, isAr]);

  const totalMonitored = monitorData?.totalMonitoredAstronauts ?? crewMembers.length;
  const activeAstronauts =
    monitorData?.activeAstronautsCount ?? crewMembers.filter((c) => c.isActive).length;
  const deactivatedAstronauts =
    monitorData?.deactivatedAstronautsCount ?? crewMembers.filter((c) => !c.isActive).length;
  const activeMissionsCount =
    monitorData?.activeMissionsCount ?? missions.filter((m) => m.status === 'Active').length;
  const activeAlertsCount =
    monitorData?.activeAlertsCount ?? alerts.filter((a) => a.status === 'Active').length;
  const emergencyAlertsCount =
    monitorData?.emergencyAlertsCount ??
    alerts.filter((a) => a.status === 'Active' && a.severity === 'Emergency').length;
  const attentionAlertsCount =
    monitorData?.medicalAttentionAlertsCount ??
    alerts.filter((a) => a.status === 'Active' && a.severity === 'MedicalAttention').length;

  const statusBadge = (status: string) => {
    if (status === 'Emergency') {
      return {
        label: isAr ? 'طوارئ حرجة' : 'Emergency',
        cls: 'bg-rose-500/20 text-rose-300 border-rose-400/40',
      };
    }
    if (status === 'MedicalAttention') {
      return {
        label: isAr ? 'تحت الملاحظة' : 'Medical Attention',
        cls: 'bg-amber-500/20 text-amber-300 border-amber-400/40',
      };
    }
    return {
      label: isAr ? 'مستقر' : 'Routine',
      cls: 'bg-emerald-500/20 text-emerald-300 border-emerald-400/40',
    };
  };

  const selectedCrewLocal =
    crewMembers.find((c) => c.id === selectedAstronautId) || crewMembers[0];

  return (
    <div className="flex flex-col gap-3.5 animate-fade-in">
      {/* Ground Medical Monitor Banner & 6 KPI Summary Cards (/api/dashboard/monitor) */}
      <div className="glass-panel rounded-[22px] p-5">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
          <div className="flex items-center gap-2.5">
            <div className="w-10 h-10 rounded-xl bg-cyan-500/20 border border-cyan-400/40 flex items-center justify-center text-cyan-300">
              <ShieldCheck className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                {tx(isAr
                  ? 'لوحة التحكم المركزية لطبيب الرحلة (Ground Medical Monitor)'
                  : 'Flight Surgeon Ground Control Dashboard (Monitor Role)')}
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? 'إشراف طبي شامل على جميع رواد الفضاء، المهمات النشطة، والتنبيهات الحرجة'
                  : 'Real-time multi-astronaut telemetry oversight, active missions, and critical alerts')}
              </p>
            </div>
          </div>

          <span className="px-3 py-1 rounded-full bg-cyan-500/15 border border-cyan-400/30 text-xs font-bold text-cyan-200">
            {tx(isAr ? 'صلاحية: طبيب الرحلة (MonitorOnly)' : 'Policy: MonitorOnly')}
          </span>
        </div>

        {/* 6 KPI Metrics from MonitorDashboardSummaryDto */}
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-2.5">
          <div className="p-3 rounded-xl bg-slate-900/60 border border-white/10">
            <p className="text-[11px] text-slate-400">
              {tx(isAr ? 'إجمالي الطاقم' : 'Total Monitored')}
            </p>
            <p className="text-xl font-extrabold text-white mt-0.5">{totalMonitored}</p>
          </div>
          <div className="p-3 rounded-xl bg-slate-900/60 border border-emerald-400/25">
            <p className="text-[11px] text-slate-400">
              {tx(isAr ? 'الرواد النشطون' : 'Active Crew')}
            </p>
            <p className="text-xl font-extrabold text-emerald-300 mt-0.5">
              {activeAstronauts}{tx(' ')}
              <span className="text-[10px] font-normal text-slate-400">
                ({tx(isAr ? `متوقف: ${deactivatedAstronauts}` : `Inactive: ${deactivatedAstronauts}`)})
              </span>
            </p>
          </div>
          <div className="p-3 rounded-xl bg-slate-900/60 border border-cyan-400/25">
            <p className="text-[11px] text-slate-400">
              {tx(isAr ? 'المهمات النشطة' : 'Active Missions')}
            </p>
            <p className="text-xl font-extrabold text-cyan-300 mt-0.5">{activeMissionsCount}</p>
          </div>
          <div className="p-3 rounded-xl bg-slate-900/60 border border-white/10">
            <p className="text-[11px] text-slate-400">
              {tx(isAr ? 'إجمالي التنبيهات' : 'Active Alerts')}
            </p>
            <p className="text-xl font-extrabold text-white mt-0.5">{activeAlertsCount}</p>
          </div>
          <div className="p-3 rounded-xl bg-slate-900/60 border border-amber-400/30">
            <p className="text-[11px] text-slate-400">
              {tx(isAr ? 'تحت الملاحظة' : 'Medical Attention')}
            </p>
            <p className="text-xl font-extrabold text-amber-300 mt-0.5">{attentionAlertsCount}</p>
          </div>
          <div className="p-3 rounded-xl bg-slate-900/60 border border-rose-400/30">
            <p className="text-[11px] text-slate-400">
              {tx(isAr ? 'تنبيهات الطوارئ' : 'Emergency Alerts')}
            </p>
            <p className="text-xl font-extrabold text-rose-300 mt-0.5">{emergencyAlertsCount}</p>
          </div>
        </div>
      </div>

      {/* Middle Row: Crew Status Summaries + Deep Astronaut Telemetry Inspector */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-3.5">
        {/* CrewSummaries List */}
        <div className="lg:col-span-6 glass-panel rounded-[22px] p-5 flex flex-col justify-between gap-4">
          <div>
            <div className="flex items-center justify-between mb-3.5">
              <div className="flex items-center gap-2">
                <Users className="w-4 h-4 text-blue-400" />
                <h4 className="text-sm font-bold text-white">
                  {tx(isAr
                    ? 'ملخص الحالة الصحية للطاقم (انقر للفحص الدقيق)'
                    : 'Crew Status Summaries (Click to Inspect)')}
                </h4>
              </div>
              {onNavigate && (
                <AsyncButton
                  type="button"
                  onClick={() => onNavigate('crew')}
                  className="text-xs font-bold text-cyan-300 hover:text-cyan-200"
                >
                  {tx(isAr ? 'إدارة الملفات ←' : 'Manage Profiles →')}
                </AsyncButton>
              )}
            </div>

            <div className="space-y-2.5">
              {crewMembers.map((crew) => {
                const badge = statusBadge(crew.overallStatus);
                const selected = crew.id === selectedAstronautId;
                return (
                  <AsyncButton
                    key={crew.id}
                    type="button"
                    onClick={() => setSelectedAstronautId(crew.id)}
                    className={`w-full p-3.5 rounded-2xl border text-start transition-all flex flex-col gap-2.5 ${
                      selected
                        ? 'bg-cyan-500/15 border-cyan-400/60 shadow-[0_0_20px_rgba(34,211,238,0.15)]'
                        : 'bg-slate-900/55 border-white/10 hover:border-white/25'
                    }`}
                  >
                    <div className="flex items-center justify-between w-full">
                      <div>
                        <span className="text-sm font-bold text-white">
                          {tx(isAr ? crew.fullNameAr : crew.fullName)}
                        </span>
                        <span className="text-xs text-slate-400 ms-2">
                          ({tx(crew.astronautCode)})
                        </span>
                      </div>
                      <span
                        className={`text-[10px] font-bold px-2.5 py-0.5 rounded-full border ${badge.cls}`}
                      >
                        {tx(badge.label)}
                      </span>
                    </div>

                    <div className="grid grid-cols-4 gap-2 w-full text-xs" dir="ltr">
                      <div className="p-2 rounded-xl bg-slate-950/60 border border-white/5 flex items-center gap-1.5">
                        <Heart className="w-3.5 h-3.5 text-rose-400 shrink-0" />
                        <span className="font-bold text-white">{tx(crew.hr)} {tx(" bpm")}</span>
                      </div>
                      <div className="p-2 rounded-xl bg-slate-950/60 border border-white/5 flex items-center gap-1.5">
                        <Wind className="w-3.5 h-3.5 text-cyan-400 shrink-0" />
                        <span className="font-bold text-white">{tx(crew.spo2)}%</span>
                      </div>
                      <div className="p-2 rounded-xl bg-slate-950/60 border border-white/5 flex items-center gap-1.5">
                        <Droplets className="w-3.5 h-3.5 text-blue-400 shrink-0" />
                        <span className="font-bold text-white">{tx(crew.bp)}</span>
                      </div>
                      <div className="p-2 rounded-xl bg-slate-950/60 border border-white/5 flex items-center gap-1.5">
                        <Thermometer className="w-3.5 h-3.5 text-amber-400 shrink-0" />
                        <span className="font-bold text-white">{tx(crew.temp)}{tx("°C")}</span>
                      </div>
                    </div>
                  </AsyncButton>
                );
              })}
            </div>
          </div>
        </div>

        {/* Selected Astronaut Full Snapshot (/api/dashboard/astronauts/{id}) */}
        <div className="lg:col-span-6 glass-panel rounded-[22px] p-5 flex flex-col justify-between gap-4">
          <div>
            <div className="flex items-center justify-between mb-3.5">
              <div className="flex items-center gap-2">
                <Eye className="w-4 h-4 text-cyan-400" />
                <div>
                  <h4 className="text-sm font-bold text-white">
                    {tx(isAr ? 'الفحص التفصيلي لرائد الفضاء: ' : 'Detailed Telemetry Snapshot: ')}
                    <span className="text-cyan-300">
                      {tx(isAr ? selectedCrewLocal?.fullNameAr : selectedCrewLocal?.fullName)}
                    </span>
                  </h4>
                  <p className="text-[11px] text-slate-400">
                    {tx(selectedCrewLocal?.astronautCode)} • {tx(selectedCrewLocal?.missionName)}
                  </p>
                </div>
              </div>

              {onNavigate && (
                <AsyncButton
                  type="button"
                  onClick={() => onNavigate('chat')}
                  className="px-3 py-1.5 rounded-xl bg-cyan-600/30 hover:bg-cyan-600/50 border border-cyan-400/40 text-xs font-bold text-cyan-200 flex items-center gap-1.5"
                >
                  <MessageSquareText className="w-3.5 h-3.5" />
                  <span>{tx(isAr ? 'مراسلة الرائد' : 'Message Astronaut')}</span>
                </AsyncButton>
              )}
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5 mb-3.5">
              <div className="p-3 rounded-xl bg-slate-900/60 border border-white/10">
                <div className="flex items-center gap-1.5 text-xs text-slate-400 mb-1">
                  <Activity className="w-3.5 h-3.5 text-emerald-400" />
                  <span>{tx(isAr ? 'التقييم العام' : 'Overall Status')}</span>
                </div>
                <p className="text-sm font-extrabold text-emerald-300">
                  {tx(selectedCrewLocal?.overallStatus === 'Routine'
                    ? isAr
                      ? 'مستقر (Routine)'
                      : 'Routine'
                    : isAr
                    ? 'تحت الملاحظة'
                    : 'Medical Attention')}
                </p>
              </div>

              <div className="p-3 rounded-xl bg-slate-900/60 border border-white/10">
                <div className="flex items-center gap-1.5 text-xs text-slate-400 mb-1">
                  <Moon className="w-3.5 h-3.5 text-indigo-400" />
                  <span>{tx(isAr ? 'آخر دورة نوم' : 'Latest Sleep')}</span>
                </div>
                <p className="text-sm font-extrabold text-white" dir="ltr">
                  {snapshot?.latestSleep?.durationHours ?? 7.75}{tx("h ( ")}{snapshot?.latestSleep?.sleepEfficiencyPercentage ?? 95}%)
                </p>
              </div>

              <div className="p-3 rounded-xl bg-slate-900/60 border border-white/10">
                <div className="flex items-center gap-1.5 text-xs text-slate-400 mb-1">
                  <Smile className="w-3.5 h-3.5 text-cyan-400" />
                  <span>{tx(isAr ? 'المزاج والتوتر' : 'Mood & Stress')}</span>
                </div>
                <p className="text-sm font-extrabold text-cyan-300" dir="ltr">
                  {snapshot?.latestMood?.moodScore ?? 9}/10 •{tx(' ')}
                  {tx(isAr ? 'توتر' : 'Stress')} {snapshot?.latestMood?.stressLevel ?? 2}/10
                </p>
              </div>
            </div>

            {/* Active / Critical Alerts for Ground Monitor */}
            <h5 className="text-xs font-bold text-slate-300 mb-2 flex items-center gap-1.5">
              <ShieldAlert className="w-3.5 h-3.5 text-amber-400" />
              <span>
                {tx(isAr
                  ? 'التنبيهات النشطة والحرجة عبر المهمة'
                  : 'Active & Critical Mission Alerts')}
              </span>
            </h5>
            <div className="space-y-2 max-h-[145px] overflow-y-auto pr-1">
              {alerts.length === 0 ? (
                <div className="p-3 rounded-xl bg-slate-900/45 border border-white/10 flex items-center gap-2 text-xs text-emerald-300">
                  <CheckCircle2 className="w-4 h-4" />
                  <span>
                    {tx(isAr
                      ? 'جميع رواد الفضاء ضمن النطاقات الحيوية الآمنة'
                      : 'All astronauts are within nominal physiological limits')}
                  </span>
                </div>
              ) : (
                alerts.slice(0, 3).map((al) => (
                  <div
                    key={al.id}
                    className="p-3 rounded-xl bg-slate-900/60 border border-amber-400/30 flex items-center justify-between gap-2 text-xs"
                  >
                    <div className="min-w-0">
                      <p className="font-bold text-white truncate flex items-center gap-1.5">
                        <AlertTriangle className="w-3.5 h-3.5 text-amber-400 shrink-0" />
                        {tx(al.title)}
                      </p>
                      <p className="text-slate-300 truncate mt-0.5">{tx(al.message)}</p>
                    </div>
                    <div className="flex items-center gap-1 shrink-0">
                      {al.status === 'Active' && (
                        <AsyncButton
                          type="button"
                          onClick={() => acknowledgeAlert(al.id)}
                          className="px-2 py-1 rounded-lg bg-amber-500/20 text-amber-200 font-bold"
                        >
                          {tx(isAr ? 'تأكيد' : 'Ack')}
                        </AsyncButton>
                      )}
                      {al.status !== 'Resolved' && (
                        <AsyncButton
                          type="button"
                          onClick={() => resolveAlert(al.id, 'Resolved by Flight Surgeon')}
                          className="px-2 py-1 rounded-lg bg-emerald-500/20 text-emerald-200 font-bold"
                        >
                          {tx(isAr ? 'إنهاء' : 'Resolve')}
                        </AsyncButton>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          {/* Active Missions Summary Bar */}
          <div className="pt-3 border-t border-white/10 flex flex-wrap items-center justify-between gap-2 text-xs">
            <div className="flex items-center gap-2 text-slate-300">
              <Rocket className="w-4 h-4 text-cyan-400" />
              <span>
                {tx(isAr ? 'المهمة الفضائية النشطة:' : 'Active Mission:')}{tx(' ')}
                <strong className="text-white">
                  {tx(missions[0] ? (isAr ? missions[0].nameAr : missions[0].name) : 'ARES-III')}
                </strong>
              </span>
            </div>
            {onNavigate && (
              <AsyncButton
                type="button"
                onClick={() => onNavigate('recommendations')}
                className="text-cyan-300 hover:text-cyan-200 font-bold"
              >
                {tx(isAr ? 'إصدار توصية طبية للطاقم ←' : 'Issue Medical Recommendation →')}
              </AsyncButton>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

