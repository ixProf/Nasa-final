import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import React, { useState } from 'react';
import {
  KeyRound,
  UserCheck,
  Stethoscope,
  ShieldCheck,
  LogOut,
  CheckCircle2,
  AlertCircle,
  ArrowRight,
  Lock,
  Unlock,
  Radio,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';
import { ActivePage } from '../../types/telemetry';

interface LoginPageProps {
  onSuccessNavigate?: (page: ActivePage) => void;
}

export const LoginPage: React.FC<LoginPageProps> = ({ onSuccessNavigate }) => {
  const tx = useUiText();
  const { language } = useLanguage();
  const {
    activeAccount,
    isAuthenticated,
    currentRole,
    currentUserName,
    currentUserCode,
    login,
    logout,
  } = useTelemetry();
  const isAr = language === 'ar';

  const [username, setUsername] = useState('astro.vance');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [statusMsg, setStatusMsg] = useState<{
    type: 'success' | 'error' | 'warning';
    text: string;
  } | null>(null);

  const seededAccounts = [
    {
      username: 'astro.vance',
      password: '',
      fullNameAr: 'مورغان فانس',
      fullNameEn: 'Morgan Vance',
      code: 'AST-001',
      role: 'Astronaut' as const,
      roleLabelAr: 'رائد فضاء — قائد المهمة',
      roleLabelEn: 'Astronaut — Mission Commander',
      email: 'morgan.vance@mission.space',
      targetPage: 'dashboard' as ActivePage,
    },
    {
      username: 'astro.lee',
      password: '',
      fullNameAr: 'جوردان لي',
      fullNameEn: 'Jordan Lee',
      code: 'AST-002',
      role: 'Astronaut' as const,
      roleLabelAr: 'رائد فضاء — مهندس طيران',
      roleLabelEn: 'Astronaut — Flight Engineer',
      email: 'jordan.lee@mission.space',
      targetPage: 'dashboard' as ActivePage,
    },
    {
      username: 'astro.quinn',
      password: '',
      fullNameAr: 'تايلور كوين',
      fullNameEn: 'Taylor Quinn',
      code: 'AST-003',
      role: 'Astronaut' as const,
      roleLabelAr: 'رائد فضاء — أخصائي حمولة',
      roleLabelEn: 'Astronaut — Payload Specialist',
      email: 'taylor.quinn@mission.space',
      targetPage: 'dashboard' as ActivePage,
    },
    {
      username: 'monitor.rivera',
      password: '',
      fullNameAr: 'د. كيسي ريفيرا',
      fullNameEn: 'Dr. Casey Rivera',
      code: 'MED-CONTROL',
      role: 'Monitor' as const,
      roleLabelAr: 'طبيب الرحلة — المراقب الطبي الأرضي',
      roleLabelEn: 'Flight Surgeon — Ground Medical Monitor',
      email: 'casey.rivera@missioncontrol.space',
      targetPage: 'monitor-dashboard' as ActivePage,
    },
  ];

  const handleManualLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!username.trim()) return;

    if (isAuthenticated) {
      setStatusMsg({
        type: 'warning',
        text: isAr
          ? 'يجب تسجيل الخروج من الجلسة الحالية أولاً قبل تسجيل الدخول بحساب جديد.'
          : 'You must log out of the current active session before signing into a new account.',
      });
      return;
    }

    setLoading(true);
    setStatusMsg(null);

    const res = await login(username.trim(), password);
    setLoading(false);

    if (res.success) {
      const matchedSeed = seededAccounts.find(
        (a) => a.username.toLowerCase() === username.trim().toLowerCase()
      );
      setStatusMsg({
        type: 'success',
        text: isAr
          ? `تم تسجيل الدخول بنجاح بحساب: ${matchedSeed?.fullNameAr || username}`
          : `Signed in successfully as ${matchedSeed?.fullNameEn || username}`,
      });
      if (onSuccessNavigate) {
        const dest =
          matchedSeed?.targetPage || (currentRole === 'Monitor' ? 'monitor-dashboard' : 'dashboard');
        setTimeout(() => onSuccessNavigate(dest), 600);
      }
    } else {
      setStatusMsg({
        type: 'error',
        text: isAr
          ? 'بيانات الدخول غير صحيحة. يرجى التأكد من اسم المستخدم وكلمة المرور أو اختيار أحد الحسابات المعتمدة.'
          : (res.message || 'Unable to sign in.'),
      });
    }
  };

  const handleAccountCardClick = (acc: (typeof seededAccounts)[number]) => {
    setUsername(acc.username);
    setPassword('');

    if (isAuthenticated) {
      if (activeAccount === acc.username) {
        setStatusMsg({
          type: 'warning',
          text: isAr
            ? `أنت مسجل بالفعل بحساب ${acc.fullNameAr}.`
            : `You are already signed in as ${acc.fullNameEn}.`,
        });
      } else {
        setStatusMsg({
          type: 'warning',
          text: isAr
            ? `أنت مسجل حالياً بحساب ${currentUserName}. يمكنك الضغط على "تبديل ودخول مباشر" للتحويل إلى حساب ${acc.fullNameAr} فوراً دون أي تداخل في الجلسات.`
            : `You are signed in as ${currentUserName}. Click "Switch & Sign In" to switch to ${acc.fullNameEn} safely.`,
        });
      }
      return;
    }

    setStatusMsg({
      type: 'success',
      text: isAr
        ? `تم تحديد ${acc.fullNameAr} (${acc.roleLabelAr}). اضغط الآن على "تسجيل الدخول إلى النظام" أو زر الدخول الفوري بالبطاقة.`
        : `Selected ${acc.fullNameEn} (${acc.roleLabelEn}). Click "Sign In to Console" or the Quick Login button.`,
    });
  };

  const handleQuickLogin = async (acc: (typeof seededAccounts)[number]) => {
    if (isAuthenticated) await logout();
    setUsername(acc.username); setPassword('');
    setStatusMsg({type:'warning',text:'Enter the password for '+acc.username+' to sign in.'});
  };

  const handleLogoutClick = async () => {
    setLoading(true);
    await logout();
    setLoading(false);
    setStatusMsg({
      type: 'success',
      text: isAr
        ? 'تم تسجيل الخروج.'
        : 'Signed out.',
    });
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 animate-fade-in">
      {/* Left Column: Session Portal & Credentials Form */}
      <div className="lg:col-span-5 glass-panel rounded-[22px] p-5 flex flex-col justify-between gap-4">
        <div>
          {/* Header */}
          <div className="flex items-center gap-2.5 pb-3.5 mb-4 border-b border-white/10">
            <div className="w-10 h-10 rounded-xl bg-cyan-500/20 border border-cyan-400/30 flex items-center justify-center text-cyan-300">
              <KeyRound className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                {tx(isAr ? 'بوابة تسجيل الدخول والجلسات' : 'Mission Authentication Portal')}
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? 'إدارة جلسة العمل وعزل الصلاحيات بدقة'
                  : 'Manage active session and access boundaries')}
              </p>
            </div>
          </div>

          {/* Active Session Card OR Logged Out Notice */}
          {isAuthenticated ? (
            <div className="mb-4 p-4 rounded-2xl bg-slate-900/80 border border-emerald-400/40 shadow-[0_0_20px_rgba(16,185,129,0.15)] flex flex-col gap-3">
              <div className="flex items-start justify-between gap-2">
                <div className="flex items-center gap-2.5">
                  <div className="w-9 h-9 rounded-xl bg-emerald-500/20 border border-emerald-400/40 flex items-center justify-center text-emerald-300">
                    <ShieldCheck className="w-5 h-5" />
                  </div>
                  <div>
                    <span className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-extrabold bg-emerald-500/20 text-emerald-300 border border-emerald-400/30">
                      <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
                      {tx(isAr ? 'جلسة نشطة حالياً' : 'Active Session')}
                    </span>
                    <p className="text-sm font-extrabold text-white mt-1">
                      {tx(currentUserName)}
                    </p>
                    <p className="text-xs text-cyan-300/90 font-medium">
                      {tx(currentRole === 'Monitor'
                        ? isAr
                          ? 'طبيب الرحلة (المراقب الطبي الأرضي)'
                          : 'Flight Surgeon (Ground Medical Monitor)'
                        : isAr
                        ? `رائد فضاء (${currentUserCode || 'AST-001'})`
                        : `Astronaut (${currentUserCode || 'AST-001'})`)}
                    </p>
                  </div>
                </div>

                <AsyncButton
                  type="button"
                  onClick={handleLogoutClick}
                  disabled={loading}
                  title={tx(isAr ? 'تسجيل الخروج' : 'Sign Out')}
                  className="px-3 py-1.5 rounded-xl bg-rose-500/20 hover:bg-rose-500/30 border border-rose-400/40 text-rose-200 text-xs font-bold flex items-center gap-1.5 transition-all shadow cursor-pointer disabled:opacity-50"
                >
                  <LogOut className="w-3.5 h-3.5" />
                  <span>{tx(isAr ? 'تسجيل الخروج' : 'Log Out')}</span>
                </AsyncButton>
              </div>

              <div className="p-2.5 rounded-xl bg-amber-500/10 border border-amber-400/25 text-[11px] text-amber-200 leading-relaxed">
                <p>
                  {tx(isAr
                    ? 'سجّل الخروج للدخول بحساب آخر.'
                    : 'Sign out to use another account.')}
                </p>
              </div>
            </div>
          ) : (
            <div className="mb-4 p-3.5 rounded-2xl bg-slate-900/60 border border-cyan-400/25 flex items-center gap-2.5">
              <Unlock className="w-5 h-5 text-cyan-400 shrink-0" />
              <div>
                <p className="text-xs font-bold text-white">
                  {tx(isAr ? 'لا توجد جلسة نشطة حالياً' : 'No Active Session')}
                </p>
                <p className="text-[11px] text-slate-300">
                  {tx(isAr
                    ? 'اختر أحد الحسابات المعتمدة من اليمين أو اكتب اسم المستخدم للدخول.'
                    : 'Select an authorized account on the right or type credentials to sign in.')}
                </p>
              </div>
            </div>
          )}

          {/* Login Form */}
          <form onSubmit={handleManualLogin} className="space-y-3">
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                {tx(isAr ? 'اسم المستخدم أو المعرّف' : 'Username / Account ID')}
              </label>
              <input
                type="text"
                required
                dir="ltr"
                disabled={isAuthenticated || loading}
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder={tx("astro.vance | monitor.rivera")}
                className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3.5 py-2.5 text-xs font-bold text-white focus:outline-none focus:border-cyan-400 disabled:opacity-50 disabled:cursor-not-allowed"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                {tx(isAr ? 'كلمة المرور' : 'Password')}
              </label>
              <input
                type="password"
                required
                dir="ltr"
                disabled={isAuthenticated || loading}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full rounded-xl bg-slate-900/75 border border-white/15 px-3.5 py-2.5 text-xs font-bold text-white focus:outline-none focus:border-cyan-400 disabled:opacity-50 disabled:cursor-not-allowed"
              />
            </div>

            {statusMsg && (
              <div
                className={`p-3 rounded-xl border text-xs flex items-start gap-2 ${
                  statusMsg.type === 'success'
                    ? 'bg-emerald-500/15 border-emerald-400/30 text-emerald-200'
                    : statusMsg.type === 'warning'
                    ? 'bg-amber-500/15 border-amber-400/30 text-amber-200'
                    : 'bg-rose-500/15 border-rose-400/30 text-rose-200'
                }`}
              >
                {statusMsg.type === 'success' ? (
                  <CheckCircle2 className="w-4 h-4 shrink-0 mt-0.5 text-emerald-400" />
                ) : (
                  <AlertCircle className="w-4 h-4 shrink-0 mt-0.5 text-amber-400" />
                )}
                <span className="leading-relaxed">{tx(statusMsg.text)}</span>
              </div>
            )}

            {!isAuthenticated ? (
              <AsyncButton
                type="submit"
                disabled={loading}
                className="w-full py-2.5 px-4 rounded-xl bg-gradient-to-r from-blue-600 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white font-bold text-sm shadow-lg transition-all flex items-center justify-center gap-2 disabled:opacity-50 cursor-pointer"
              >
                <KeyRound className="w-4 h-4" />
                <span>
                  {tx(loading
                    ? isAr
                      ? 'جاري التحقق وبدء الجلسة...'
                      : 'Authenticating...'
                    : isAr
                    ? 'تسجيل الدخول إلى النظام'
                    : 'Sign In to Console')}
                </span>
              </AsyncButton>
            ) : (
              <AsyncButton
                type="button"
                onClick={handleLogoutClick}
                disabled={loading}
                className="w-full py-2.5 px-4 rounded-xl bg-rose-600/30 hover:bg-rose-600/40 border border-rose-400/50 text-rose-200 font-bold text-sm shadow transition-all flex items-center justify-center gap-2 cursor-pointer"
              >
                <LogOut className="w-4 h-4" />
                <span>
                  {tx(isAr
                    ? 'تسجيل الخروج'
                    : 'Sign out')}
                </span>
              </AsyncButton>
            )}
          </form>
        </div>

        {/* Navigation Shortcut to Active Dashboard */}
        {isAuthenticated && onSuccessNavigate && (
          <AsyncButton
            type="button"
            onClick={() =>
              onSuccessNavigate(currentRole === 'Monitor' ? 'monitor-dashboard' : 'dashboard')
            }
            className="w-full py-2.5 px-3 rounded-xl bg-white/5 hover:bg-white/10 border border-white/15 text-xs font-bold text-cyan-300 flex items-center justify-center gap-1.5 transition-all"
          >
            <span>
              {tx(currentRole === 'Monitor'
                ? isAr
                  ? 'الانتقال إلى لوحة تحكم طبيب الرحلة النشطة'
                  : 'Open Active Flight Surgeon Dashboard'
                : isAr
                ? 'الانتقال إلى لوحة تحكم رائد الفضاء النشطة'
                : 'Open Active Astronaut Dashboard')}
            </span>
            <ArrowRight className="w-3.5 h-3.5" />
          </AsyncButton>
        )}
      </div>

      {/* Right Column: The 4 Seeded Official Accounts */}
      <div className="lg:col-span-7 glass-panel rounded-[22px] p-5 flex flex-col justify-between gap-4">
        <div>
          <div className="mb-4">
            <h3 className="text-base font-bold text-white">
              {tx(isAr
                ? 'الحسابات المعتمدة في النظام (رواد الفضاء + المراقب الطبي)'
                : 'Authorized Mission Accounts (3 Astronauts + 1 Flight Surgeon)')}
            </h3>
            <p className="text-xs text-slate-300 mt-1 leading-relaxed">
              {tx(isAr
                ? 'لكل حساب جلسة مستقلة وقناة محادثة معزولة تماماً. عند تسجيل الدخول بحساب يجب تسجيل الخروج منه أولاً قبل الانتقال لأي حساب آخر لمنع تداخل البيانات.'
                : 'Each account holds an isolated session and dedicated telemedicine chat thread. You must log out of the active account before signing in as another.')}
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {seededAccounts.map((acc) => {
              const isSelected = activeAccount === acc.username && isAuthenticated;
              const isMonitor = acc.role === 'Monitor';
              return (
                <div
                  key={acc.username}
                  onClick={() => handleAccountCardClick(acc)}
                  className={`p-4 rounded-2xl border transition-all flex flex-col justify-between gap-3 cursor-pointer ${
                    isSelected
                      ? isMonitor
                        ? 'bg-cyan-500/20 border-cyan-400/70 shadow-[0_0_24px_rgba(34,211,238,0.25)] ring-1 ring-cyan-400/60'
                        : 'bg-blue-500/20 border-blue-400/70 shadow-[0_0_24px_rgba(59,130,246,0.25)] ring-1 ring-blue-400/60'
                      : isAuthenticated
                      ? 'bg-slate-900/40 border-white/5 opacity-70 hover:opacity-100 hover:border-amber-400/40'
                      : 'bg-slate-900/60 border-white/10 hover:border-cyan-400/50 hover:bg-slate-900/80 hover:shadow-lg'
                  }`}
                >
                  <div>
                    <div className="flex items-center justify-between gap-2 mb-2">
                      <div className="flex items-center gap-2">
                        <div
                          className={`w-8 h-8 rounded-xl flex items-center justify-center border ${
                            isMonitor
                              ? 'bg-cyan-500/20 border-cyan-400/40 text-cyan-300'
                              : 'bg-blue-500/20 border-blue-400/40 text-blue-300'
                          }`}
                        >
                          {isMonitor ? (
                            <Stethoscope className="w-4 h-4" />
                          ) : (
                            <UserCheck className="w-4 h-4" />
                          )}
                        </div>
                        <div>
                          <p className="text-xs font-bold text-white">
                            {tx(isAr ? acc.fullNameAr : acc.fullNameEn)}
                          </p>
                          <span
                            className={`text-[10px] font-semibold ${
                              isMonitor ? 'text-cyan-300' : 'text-blue-300'
                            }`}
                          >
                            {tx(isAr ? acc.roleLabelAr : acc.roleLabelEn)}
                          </span>
                        </div>
                      </div>

                      {isSelected ? (
                        <span className="inline-flex items-center gap-1 text-[10px] font-extrabold px-2 py-0.5 rounded-full bg-emerald-500/25 border border-emerald-400/40 text-emerald-300">
                          <CheckCircle2 className="w-3 h-3 text-emerald-400" />
                          <span>{tx(isAr ? 'نشط' : 'Active')}</span>
                        </span>
                      ) : isAuthenticated ? (
                        <span className="inline-flex items-center gap-1 text-[10px] font-bold px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 border border-white/10">
                          <Lock className="w-2.5 h-2.5" />
                          <span>{tx(isAr ? 'مقفل' : 'Locked')}</span>
                        </span>
                      ) : null}
                    </div>

                    <p className="text-[11px] text-slate-400 font-mono mt-1" dir="ltr">
                      {tx(acc.email)}
                    </p>
                  </div>

                  <div className="pt-2 border-t border-white/10 flex items-center justify-between gap-2 text-[11px]">
                    <span className="text-slate-400 font-mono">{tx(acc.code)}</span>
                    {isSelected ? (
                      <span className="font-extrabold text-emerald-300">
                        {tx(isAr ? 'الجلسة النشطة حالياً' : 'Current Active Session')}
                      </span>
                    ) : isAuthenticated ? (
                      <AsyncButton
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleQuickLogin(acc);
                        }}
                        disabled={loading}
                        className="px-2.5 py-1 rounded-lg bg-amber-500/20 hover:bg-amber-500/30 border border-amber-400/40 text-amber-200 font-bold text-[10px] flex items-center gap-1 transition-all cursor-pointer"
                      >
                        <LogOut className="w-2.5 h-2.5" />
                        <span>{tx(isAr ? 'خروج وتبديل لهذا الحساب' : 'Switch & Sign In')}</span>
                      </AsyncButton>
                    ) : (
                      <AsyncButton
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleQuickLogin(acc);
                        }}
                        disabled={loading}
                        className="px-2.5 py-1 rounded-lg bg-cyan-500/20 hover:bg-cyan-500/30 border border-cyan-400/40 text-cyan-200 font-bold text-[10px] flex items-center gap-1 transition-all cursor-pointer"
                      >
                        <UserCheck className="w-2.5 h-2.5" />
                        <span>{tx(isAr ? 'دخول فوري مباشر ←' : 'Quick Sign In →')}</span>
                      </AsyncButton>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Security / Isolation Assurance Footnote */}
        <div className="p-3.5 rounded-2xl bg-cyan-950/30 border border-cyan-400/20 text-xs text-cyan-200/90 flex items-center gap-2.5">
          <Radio className="w-4 h-4 text-cyan-400 shrink-0" />
          <p className="leading-relaxed">
            {tx(isAr
              ? "سجّل الدخول بحسابك المخصّص للوصول إلى سجلاتك ومحادثاتك الطبية."
              : "Sign in with your assigned account to access your records and medical conversations.")}
          </p>
        </div>
      </div>
    </div>
  );
};
