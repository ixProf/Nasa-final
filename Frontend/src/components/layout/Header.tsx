import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import React, { useState, useRef, useEffect } from 'react';
import {
  Bell,
  Globe,
  LogOut,
  AlertCircle,
  AlertTriangle,
  CheckCircle2,
  ExternalLink,
  Check,
  Sparkles,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { ActivePage } from '../../types/telemetry';

interface HeaderProps {
  activePage: ActivePage;
  setActivePage: (page: ActivePage) => void;
}

export const Header: React.FC<HeaderProps> = ({ activePage, setActivePage }) => {
  const tx = useUiText();
  const { t, language, toggleLanguage } = useLanguage();
  const {
    unreadAlertsCount,
    alerts,
    acknowledgeAlert,
    acknowledgeAllAlerts,
    isOnline,
    currentRole,
    currentUserName,
  } = useTelemetry();
  const isAr = language === 'ar';

  const [showNotifications, setShowNotifications] = useState(false);
  const popoverRef = useRef<HTMLDivElement>(null);

  // Close notifications popover on click outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (popoverRef.current && !popoverRef.current.contains(event.target as Node)) {
        setShowNotifications(false);
      }
    };
    if (showNotifications) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [showNotifications]);

  const activeAlerts = alerts.filter((a) => a.status === 'Active');

  const handleNavigateToAlerts = () => {
    setActivePage('alerts');
    setShowNotifications(false);
  };

  return (
    <header className="w-full flex items-center justify-between py-1 px-1 relative z-30">
      {/* Title & Subtitle */}
      <div>
        <h1 className="text-2xl md:text-3xl font-extrabold text-white tracking-tight drop-shadow-[0_2px_10px_rgba(0,0,0,0.6)]">
          {tx(t.header.title)}
        </h1>
        <p className="text-xs md:text-sm text-slate-300/90 font-medium mt-1">
          {tx(t.header.subtitle)}
        </p>
      </div>

      {/* Controls & Profile */}
      <div className="flex items-center gap-2.5 md:gap-3.5">
        {/* Language Toggle */}
        <AsyncButton
          onClick={toggleLanguage}
          className="h-10 px-3.5 rounded-2xl glass-panel-subtle hover:border-cyan-400/50 text-xs font-bold text-cyan-200 hover:text-white flex items-center gap-1.5 transition-all duration-300 cursor-pointer"
        >
          <Globe className="w-4 h-4 text-cyan-400" />
          <span>{tx(t.header.switchLang)}</span>
        </AsyncButton>

        {/* Notifications Bell Container */}
        <div className="relative" ref={popoverRef}>
          <AsyncButton
            onClick={() => setShowNotifications(!showNotifications)}
            title={tx(isAr ? 'مركز الإشعارات والتنبيهات الطبية' : 'Medical Alerts & Notifications')}
            className={`relative w-10 h-10 md:w-11 md:h-11 rounded-full glass-panel flex items-center justify-center transition-all duration-300 cursor-pointer ${
              showNotifications || activePage === 'alerts'
                ? 'border-cyan-400 text-cyan-300 shadow-[0_0_20px_rgba(0,210,255,0.4)]'
                : 'text-slate-100 hover:text-white'
            }`}
          >
            <Bell className="w-5 h-5 stroke-[2.1]" />
            {unreadAlertsCount > 0 && (
              <span className="absolute -top-1 -right-1 min-w-[20px] h-5 px-1 rounded-full bg-rose-600 text-white text-[10px] font-black flex items-center justify-center border-2 border-[#020611] shadow-[0_0_12px_rgba(244,63,94,0.7)] animate-pulse">
                {tx(unreadAlertsCount > 9 ? '9+' : unreadAlertsCount)}
              </span>
            )}
          </AsyncButton>

          {/* ── Interactive Notification Popover Drawer ── */}
          {showNotifications && (
            <div
              className={`absolute top-full mt-2.5 w-[330px] sm:w-[380px] rounded-3xl glass-panel border border-cyan-400/30 bg-[#061129]/95 backdrop-blur-xl shadow-2xl p-4 flex flex-col gap-3 z-50 animate-fade-in ${
                isAr ? 'left-0 sm:-left-12' : 'right-0 sm:-right-12'
              }`}
            >
              {/* Popover Header */}
              <div className="flex items-center justify-between pb-2.5 border-b border-white/10">
                <div className="flex items-center gap-2">
                  <div className="w-7 h-7 rounded-lg bg-cyan-500/20 border border-cyan-400/40 flex items-center justify-center text-cyan-300">
                    <Bell className="w-3.5 h-3.5" />
                  </div>
                  <div>
                    <h3 className="text-xs font-extrabold text-white">
                      {tx(isAr ? 'مركز الإشعارات والإنذارات الحية' : 'Live Medical Notification Center')}
                    </h3>
                    <p className="text-[10px] text-cyan-300/80 font-mono">
                      {tx(unreadAlertsCount > 0
                        ? `${unreadAlertsCount} ${isAr ? 'تنبيهات نشطة تتطلب الانتباه' : 'active alerts'}`
                        : (isAr ? 'جميع المؤشرات مستقرة' : 'All systems nominal'))}
                    </p>
                  </div>
                </div>

                {unreadAlertsCount > 0 && (
                  <AsyncButton
                    type="button"
                    onClick={acknowledgeAllAlerts}
                    className="text-[10px] font-bold px-2 py-1 rounded-lg bg-white/5 hover:bg-white/10 text-cyan-200 hover:text-white transition-all cursor-pointer"
                  >
                    {tx(isAr ? 'تأكيد الكل ✓' : 'Acknowledge All')}
                  </AsyncButton>
                )}
              </div>

              {/* Popover Body: Alerts List */}
              <div className="max-h-[280px] overflow-y-auto no-scrollbar flex flex-col gap-2">
                {activeAlerts.length === 0 ? (
                  <div className="py-6 flex flex-col items-center justify-center text-center gap-2">
                    <div className="w-10 h-10 rounded-full bg-emerald-500/15 border border-emerald-400/30 flex items-center justify-center text-emerald-400">
                      <CheckCircle2 className="w-5 h-5" />
                    </div>
                    <p className="text-xs font-bold text-white">
                      {tx(isAr ? 'لا توجد إنذارات طبية نشطة' : 'No Active Medical Alerts')}
                    </p>
                    <p className="text-[11px] text-slate-300 max-w-[240px]">
                      {tx(isAr
                        ? 'كافة القياسات الحيوية والتوصيات المجدولة ضمن النطاق الطبيعي المستقر.'
                        : 'All telemetry metrics and scheduled recommendations are within safe parameters.')}
                    </p>
                  </div>
                ) : (
                  activeAlerts.slice(0, 4).map((alert) => {
                    const isEmergency = alert.severity === 'Emergency';
                    return (
                      <div
                        key={alert.id}
                        className={`p-3 rounded-2xl border text-xs flex flex-col gap-1.5 transition-all ${
                          isEmergency
                            ? 'bg-rose-950/40 border-rose-500/40 shadow-[0_0_12px_rgba(244,63,94,0.15)]'
                            : 'bg-amber-950/30 border-amber-400/30'
                        }`}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div className="flex items-center gap-1.5">
                            {isEmergency ? (
                              <AlertCircle className="w-4 h-4 text-rose-400 animate-pulse shrink-0" />
                            ) : (
                              <AlertTriangle className="w-4 h-4 text-amber-400 shrink-0" />
                            )}
                            <span className="font-extrabold text-white text-[11px] leading-tight">
                              {tx(alert.title)}
                            </span>
                          </div>
                          <span
                            className={`text-[9px] font-extrabold px-1.5 py-0.5 rounded-full uppercase border shrink-0 ${
                              isEmergency
                                ? 'bg-rose-500/20 text-rose-300 border-rose-400/40'
                                : 'bg-amber-500/20 text-amber-300 border-amber-400/40'
                            }`}
                          >
                            {tx(alert.severity === 'Emergency' ? (isAr ? 'طوارئ' : 'Emergency') : (isAr ? 'تنبيه' : 'Attention'))}
                          </span>
                        </div>

                        <p className="text-[11px] text-slate-200 leading-snug line-clamp-2">
                          {tx(alert.message)}
                        </p>

                        <div className="flex items-center justify-between pt-1 border-t border-white/10 text-[10px]">
                          <span className="text-slate-400 font-mono">
                            {tx(new Date(alert.triggeredAt).toLocaleTimeString(isAr ? 'ar-EG' : 'en-GB', { hour: '2-digit', minute: '2-digit' }))}
                          </span>
                          <AsyncButton
                            type="button"
                            onClick={() => acknowledgeAlert(alert.id)}
                            className="text-cyan-300 hover:text-white font-bold flex items-center gap-0.5 transition-all cursor-pointer"
                          >
                            <Check className="w-3 h-3" />
                            <span>{tx(isAr ? 'تأكيد الاستلام' : 'Acknowledge')}</span>
                          </AsyncButton>
                        </div>
                      </div>
                    );
                  })
                )}
              </div>

              {/* Popover Footer: Direct Link to Full Alerts Page */}
              <div className="pt-2 border-t border-white/10 flex flex-col gap-2">
                <AsyncButton
                  type="button"
                  onClick={handleNavigateToAlerts}
                  className="w-full py-2.5 px-3 rounded-xl bg-gradient-to-r from-cyan-600 to-blue-600 hover:from-cyan-500 hover:to-blue-500 text-white text-xs font-extrabold flex items-center justify-center gap-2 shadow-[0_0_15px_rgba(34,211,238,0.25)] transition-all cursor-pointer"
                >
                  <span>{tx(isAr ? 'عرض كافة التنبيهات وإدارة الطوارئ الطبية' : 'Open Full Emergency Desk')}</span>
                  <ExternalLink className="w-3.5 h-3.5" />
                </AsyncButton>
              </div>
            </div>
          )}
        </div>

        {/* Divider */}
        <div className="h-7 w-[1px] bg-white/15 hidden sm:block" />

        {/* Active User Profile Badge (Click to Switch Account or Logout) */}
        <div className="flex items-center gap-2.5">
          <AsyncButton
            onClick={() => setActivePage('login')}
            title={tx(isAr ? 'إدارة الحساب أو تبديل المستخدم' : 'Account & Session Settings')}
            className="flex items-center gap-3 py-1 px-1.5 rounded-2xl hover:bg-white/5 transition-all text-start cursor-pointer"
          >
            <div className="hidden sm:block text-end">
              <div className="text-sm font-bold text-white leading-tight">
                {tx(t.header.welcome)} {tx(currentUserName || (isAr ? 'مورغان فانس' : 'Morgan Vance'))}
              </div>
              <div className="text-xs text-cyan-300/90 font-semibold mt-0.5">
                {tx(currentRole === 'Monitor' ? t.header.monitorRole : t.header.role)}
              </div>
            </div>

            <div className="relative">
              <div className="w-10 h-10 md:w-11 md:h-11 rounded-full overflow-hidden border-2 border-cyan-300/60 shadow-[0_0_16px_rgba(56,189,248,0.3)] bg-slate-900 flex items-center justify-center">
                <svg
                  viewBox="0 0 100 100"
                  className="w-full h-full object-cover"
                  fill="none"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <circle cx="50" cy="50" r="48" fill="#091532" />
                  <circle
                    cx="50"
                    cy="46"
                    r="30"
                    fill="#f1f5f9"
                    stroke="#94a3b8"
                    strokeWidth="2.5"
                  />
                  <rect
                    x="27"
                    y="31"
                    width="46"
                    height="27"
                    rx="13.5"
                    fill="#0f2942"
                    stroke="#38bdf8"
                    strokeWidth="1.5"
                  />
                  <ellipse cx="44" cy="40" rx="13" ry="7" fill="#38bdf8" opacity="0.45" />
                  <path
                    d="M20 86 C20 67 80 67 80 86"
                    fill="#e2e8f0"
                    stroke="#64748b"
                    strokeWidth="2.5"
                  />
                </svg>
              </div>
              <span
                title={tx(isOnline ? t.header.onlineStatus : t.header.offlineStatus)}
                className={`absolute bottom-0 right-0 w-3 h-3 rounded-full border-2 border-slate-950 ${
                  isOnline ? 'bg-emerald-400 shadow-[0_0_8px_#34d399]' : 'bg-amber-400'
                }`}
              />
            </div>
          </AsyncButton>

          {/* Subtle Logout / Switch Account Icon Button */}
          <AsyncButton
            onClick={() => setActivePage('login')}
            title={tx(isAr ? 'تبديل الحساب / تسجيل الخروج' : 'Switch Account / Sign Out')}
            className="w-9 h-9 rounded-xl glass-panel-subtle hover:border-rose-400/40 text-slate-300 hover:text-rose-300 flex items-center justify-center transition-all cursor-pointer"
          >
            <LogOut className="w-4 h-4" />
          </AsyncButton>
        </div>
      </div>
    </header>
  );
};
