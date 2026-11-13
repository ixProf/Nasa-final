import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import React from 'react';
import {
  Home,
  ShieldCheck,
  Activity,
  BookOpen,
  Moon,
  Smile,
  ClipboardCheck,
  Stethoscope,
  ShieldAlert,
  MessageSquareText,
  Users,
  Rocket,
  FileSpreadsheet,
  Radio,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { ActivePage } from '../../types/telemetry';

interface SidebarProps {
  activePage: ActivePage;
  setActivePage: (page: ActivePage) => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ activePage, setActivePage }) => {
  const tx = useUiText();
  const { language, t } = useLanguage();
  const {
    isOnline,
    toggleOnlineStatus,
    unreadAlertsCount,
    unreadChatCount,
    currentRole,
  } = useTelemetry();

  const astronautNavItems: {
    id: ActivePage;
    label: string;
    icon: any;
    badge?: number;
    badgeColor?: string;
  }[] = [
    { id: 'dashboard', label: t.nav.dashboard, icon: Home },
    { id: 'vitals', label: t.nav.vitals, icon: Activity },
    { id: 'vital-guides', label: t.nav.vitalGuides, icon: BookOpen },
    { id: 'sleep', label: t.nav.sleep, icon: Moon },
    { id: 'mood', label: t.nav.mood, icon: Smile },
    { id: 'recommendations', label: t.nav.recommendations, icon: ClipboardCheck },
    { id: 'symptoms', label: t.nav.symptoms, icon: Stethoscope },
    {
      id: 'alerts',
      label: t.nav.alerts,
      icon: ShieldAlert,
      badge: unreadAlertsCount,
      badgeColor: 'bg-red-500 shadow-red-500/50',
    },
    {
      id: 'chat',
      label: t.nav.chat,
      icon: MessageSquareText,
      badge: unreadChatCount,
      badgeColor: 'bg-cyan-500 shadow-cyan-500/50',
    },
    { id: 'reports-sync', label: t.nav.reportsSync, icon: FileSpreadsheet },
  ];

  const monitorNavItems: {
    id: ActivePage;
    label: string;
    icon: any;
    badge?: number;
    badgeColor?: string;
  }[] = [
    { id: 'monitor-dashboard', label: t.nav.monitorDashboard, icon: ShieldCheck },
    { id: 'crew', label: t.nav.crew, icon: Users },
    { id: 'missions', label: t.nav.missions, icon: Rocket },
    { id: 'recommendations', label: t.nav.recommendations, icon: ClipboardCheck },
    {
      id: 'alerts',
      label: t.nav.alerts,
      icon: ShieldAlert,
      badge: unreadAlertsCount,
      badgeColor: 'bg-red-500 shadow-red-500/50',
    },
    {
      id: 'chat',
      label: t.nav.chat,
      icon: MessageSquareText,
      badge: unreadChatCount,
      badgeColor: 'bg-cyan-500 shadow-cyan-500/50',
    },
    { id: 'symptoms', label: t.nav.symptoms, icon: Stethoscope },
    { id: 'vital-guides', label: t.nav.vitalGuides, icon: BookOpen },
  ];

  const navItems = currentRole === 'Monitor' ? monitorNavItems : astronautNavItems;
  const homePage: ActivePage = currentRole === 'Monitor' ? 'monitor-dashboard' : 'dashboard';

  return (
    <>
      {/* Layout Spacer so Main Content Never Overlaps the Fixed Sidebar */}
      <div className="w-[88px] md:w-[98px] shrink-0" aria-hidden="true" />

      {/* Fixed Full-Viewport Glass Navigation Dock */}
      <aside className="fixed top-2.5 bottom-2.5 md:top-4 md:bottom-4 ltr:left-2.5 ltr:md:left-4 rtl:right-2.5 rtl:md:right-4 w-[88px] md:w-[98px] glass-dock rounded-[28px] flex flex-col items-center py-3 px-1.5 justify-between z-40 select-none">
        {/* Top: Official NASA Emblem Badge (Routes to Role-Specific Home Dashboard) */}
        <div className="flex flex-col items-center shrink-0 pb-1.5 border-b border-white/10 w-full">
          <AsyncButton
            onClick={() => setActivePage(homePage)}
            className="w-11 h-11 rounded-full bg-gradient-to-br from-blue-600 via-blue-800 to-slate-950 p-[2px] shadow-[0_0_20px_rgba(37,99,235,0.45)] group cursor-pointer hover:scale-105 transition-transform duration-300"
          >
            <div className="w-full h-full rounded-full bg-[#091b46] flex items-center justify-center relative overflow-hidden border border-sky-300/35">
              <span className="font-black text-[10px] tracking-wider text-white font-mono z-10 drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]">
                {tx(" NASA ")}</span>
              <div className="absolute w-10 h-4 border-b-[2px] border-red-500 rounded-[100%] rotate-[-25deg] pointer-events-none" />
              <div className="absolute w-9 h-3.5 border border-white/40 rounded-[100%] rotate-[28deg] pointer-events-none" />
            </div>
          </AsyncButton>
        </div>

        {/* Center: Evenly Distributed Role-Filtered Navigation Items (Zero Top/Bottom Gaps) */}
        <nav className="flex-1 w-full overflow-y-auto no-scrollbar flex flex-col items-center justify-between gap-1 py-2 px-0.5">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = activePage === item.id;
            return (
              <AsyncButton
                key={item.id}
                onClick={() => setActivePage(item.id)}
                title={tx(item.label)}
                className={`w-full flex-1 min-h-[42px] max-h-[62px] px-1 rounded-2xl flex flex-col items-center justify-center gap-1 transition-all duration-200 relative group ${
                  isActive
                    ? 'glass-action-primary text-white shadow-md'
                    : 'text-slate-300/85 hover:text-white hover:bg-white/10 border border-transparent'
                }`}
              >
                <div className="relative flex items-center justify-center">
                  <Icon className="w-4 h-4 stroke-[2.2]" />
                  {item.badge !== undefined && item.badge > 0 && (
                    <span
                      className={`absolute -top-1.5 -right-2.5 w-3.5 h-3.5 rounded-full text-[9px] font-extrabold text-white flex items-center justify-center shadow ${item.badgeColor}`}
                    >
                      {tx(item.badge)}
                    </span>
                  )}
                </div>
                <span className="text-[10px] font-bold leading-tight text-center line-clamp-1 w-full px-0.5">
                  {tx(item.label)}
                </span>
              </AsyncButton>
            );
          })}
        </nav>

        {/* Bottom: Connection Status Only (Online / Offline) */}
        <div className="flex items-center justify-center pt-2 border-t border-white/10 w-full px-1 shrink-0">
          <AsyncButton
            onClick={toggleOnlineStatus}
            title={tx(isOnline ? t.header.onlineStatus : t.header.offlineStatus)}
            className={`w-full py-1.5 px-2 rounded-xl glass-panel-subtle transition-all flex items-center justify-center gap-1.5 text-[10px] font-bold ${
              isOnline
                ? 'text-emerald-300 hover:border-emerald-400/50'
                : 'text-amber-300 hover:border-amber-400/50'
            }`}
          >
            <span className="relative flex items-center justify-center">
              <Radio className={`w-3.5 h-3.5 ${isOnline ? 'text-emerald-400' : 'text-amber-400'}`} />
              <span
                className={`absolute -top-0.5 -right-0.5 w-1.5 h-1.5 rounded-full ${
                  isOnline ? 'bg-emerald-400' : 'bg-amber-400 animate-ping'
                }`}
              />
            </span>
            <span>
              {tx(isOnline
                ? language === 'ar'
                  ? 'متصل'
                  : 'Online'
                : language === 'ar'
                ? 'أوفلاين'
                : 'Offline')}
            </span>
          </AsyncButton>
        </div>
      </aside>
    </>
  );
};
