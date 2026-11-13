import { AsyncButton } from "../AsyncButton";
import { useUiText } from "../../i18n/useUiText";
import { AsyncForm } from "../AsyncForm";
import React, { useState, useEffect, useRef } from 'react';
import {
  MessageSquare,
  Send,
  CheckCheck,
  Check,
  Stethoscope,
  Users,
  Radio,
  User,
  Shield,
  Clock,
  Sparkles,
  UserCheck,
  Building2,
  BrainCircuit,
  ArrowRight,
} from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';

interface RecipientOption {
  id: string;
  role: 'Astronaut' | 'Monitor';
  astronautId?: number;
  titleAr: string;
  titleEn: string;
  roleLabelAr: string;
  roleLabelEn: string;
  badgeAr: string;
  badgeEn: string;
  iconType: 'doctor' | 'control' | 'behavioral' | 'astronaut';
  code: string;
}

export const GroundOrbitChatPage: React.FC = () => {
  const tx = useUiText();
  const { language } = useLanguage();
  const {
    chatMessages,
    sendChatMessage,
    markAllChatRead,
    currentRole,
    currentUserName,
    currentUserCode,
    activeAccount,
    selectedChatAstronautId,
    setSelectedChatAstronautId,
    crewMembers,
    isOnline,
    activeScenario,
  } = useTelemetry();
  const isAr = language === 'ar';

  const [messageText, setMessageText] = useState('');
  const [sending, setSending] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // All prospective recipients available across the mission network
  const allRecipients: RecipientOption[] = [
    {
      id: 'doctor-rivera',
      role: 'Monitor',
      titleAr: 'د. كيسي ريفيرا',
      titleEn: 'Dr. Casey Rivera',
      roleLabelAr: 'طبيب الرحلة الرئيسي',
      roleLabelEn: 'Lead Flight Surgeon',
      badgeAr: 'المركز الطبي الأرضي',
      badgeEn: 'Ground Medical Desk',
      iconType: 'doctor',
      code: 'MED-SURGEON',
      astronautId: 1,
    },
    {
      id: 'mcc-ground',
      role: 'Monitor',
      titleAr: 'مركز العمليات الطبية - هيوستن',
      titleEn: 'Mission Control Medical Console',
      roleLabelAr: 'غرفة الدعم الطبي الأرضي المستمر',
      roleLabelEn: 'Continuous Medical Operations Desk',
      badgeAr: 'مركز المراقبة MCC',
      badgeEn: 'Flight Ops Desk',
      iconType: 'control',
      code: 'MCC-HOUSTON',
      astronautId: 1,
    },
    {
      id: 'behavioral-support',
      role: 'Monitor',
      titleAr: 'أخصائي الدعم النفسي والفسيولوجي',
      titleEn: 'Bio-Behavioral Health Officer',
      roleLabelAr: 'متابعة جودة النوم والراحة والبيئة',
      roleLabelEn: 'Circadian, Sleep & Stress Specialist',
      badgeAr: 'الصحة السلوكية',
      badgeEn: 'Behavioral Health',
      iconType: 'behavioral',
      code: 'MED-PSYCH',
      astronautId: 1,
    },
    {
      id: 'astro-vance',
      role: 'Astronaut',
      astronautId: 1,
      titleAr: 'مورغان فانس',
      titleEn: 'Morgan Vance',
      roleLabelAr: 'قائد المهمة (AST-001)',
      roleLabelEn: 'Commander (AST-001)',
      badgeAr: 'طاقم المركبة',
      badgeEn: 'Flight Crew',
      iconType: 'astronaut',
      code: 'AST-001',
    },
    {
      id: 'astro-lee',
      role: 'Astronaut',
      astronautId: 2,
      titleAr: 'جوردان لي',
      titleEn: 'Jordan Lee',
      roleLabelAr: 'مهندس طيران (AST-002)',
      roleLabelEn: 'Flight Engineer (AST-002)',
      badgeAr: 'طاقم المركبة',
      badgeEn: 'Flight Crew',
      iconType: 'astronaut',
      code: 'AST-002',
    },
    {
      id: 'astro-quinn',
      role: 'Astronaut',
      astronautId: 3,
      titleAr: 'تايلور كوين',
      titleEn: 'Taylor Quinn',
      roleLabelAr: 'أخصائي حمولة (AST-003)',
      roleLabelEn: 'Payload Specialist (AST-003)',
      badgeAr: 'طاقم المركبة',
      badgeEn: 'Flight Crew',
      iconType: 'astronaut',
      code: 'AST-003',
    },
  ];

  // Exclude current user from recipient candidates
  const availableRecipients = allRecipients.filter((r) => {
    if (currentRole === 'Monitor' && r.id === 'doctor-rivera') return false;
    if (activeAccount === 'astro.vance' && r.id === 'astro-vance') return false;
    if (activeAccount === 'astro.lee' && r.id === 'astro-lee') return false;
    if (activeAccount === 'astro.quinn' && r.id === 'astro-quinn') return false;
    return true;
  });

  // Default active recipient based on role
  const initialRecipientId =
    currentRole === 'Monitor'
      ? selectedChatAstronautId === 2
        ? 'astro-lee'
        : selectedChatAstronautId === 3
        ? 'astro-quinn'
        : 'astro-vance'
      : 'doctor-rivera';

  const [selectedRecipientId, setSelectedRecipientId] = useState<string>(initialRecipientId);

  // Sync recipient state when role or active astronaut changes
  useEffect(() => {
    if (currentRole === 'Monitor') {
      const matchAstro =
        selectedChatAstronautId === 2
          ? 'astro-lee'
          : selectedChatAstronautId === 3
          ? 'astro-quinn'
          : 'astro-vance';
      setSelectedRecipientId(matchAstro);
    } else {
      setSelectedRecipientId((prev) => (prev.startsWith('astro') ? prev : 'doctor-rivera'));
    }
  }, [currentRole, selectedChatAstronautId]);

  const activeRecipient =
    availableRecipients.find((r) => r.id === selectedRecipientId) ||
    availableRecipients[0] ||
    allRecipients[0];

  const handleSelectRecipient = (rec: RecipientOption) => {
    setSelectedRecipientId(rec.id);
    if (currentRole === 'Monitor' && rec.astronautId) {
      setSelectedChatAstronautId(rec.astronautId);
    }
  };

  // Find currently selected astronaut for doctor thread view
  const activeAstronaut =
    crewMembers.find((c) => c.id === selectedChatAstronautId) ||
    crewMembers[0] || {
      id: 1,
      astronautCode: 'AST-001',
      fullName: 'Morgan Vance',
      fullNameAr: 'مورغان فانس',
      missionName: 'Ares III Expedition',
    };

  // Mark messages as read whenever thread changes or new messages arrive
  useEffect(() => {
    markAllChatRead();
  }, [selectedChatAstronautId, chatMessages.length]);

  // Auto scroll to bottom
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [chatMessages]);

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!messageText.trim() || sending) return;
    const text = messageText.trim();

    setSending(true);
    try {
      const recipientLabel = isAr ? activeRecipient.titleAr : activeRecipient.titleEn;
      const recipientRoleLabel = isAr ? activeRecipient.roleLabelAr : activeRecipient.roleLabelEn;
      const targetAstroId =
        currentRole === 'Monitor'
          ? activeRecipient.astronautId || selectedChatAstronautId
          : undefined;

      await sendChatMessage(text, currentRole, targetAstroId, recipientLabel, recipientRoleLabel);
      setMessageText('');
    } finally {
      setSending(false);
    }
  };

  const scenarioQuickReply =
    activeScenario.id !== 'nominal'
      ? currentRole === 'Monitor'
        ? isAr
          ? `توجيه طبي عاجل بخصوص ${activeScenario.titleAr}: يرجى اتباع البروتوكول فوراً — ${activeScenario.flightSurgeonDirectiveAr}`
          : `Urgent Medical Directive for ${activeScenario.titleEn}: ${activeScenario.flightSurgeonDirectiveEn}`
        : isAr
        ? `بلاغ طبي لطبيب الرحلة: أرصد أعراض (${activeScenario.symptomsAr.slice(0, 2).join('، ')}) مع مؤشرات حيوية: نبض ${activeScenario.targetVitals.hr} bpm وأكسجين ${activeScenario.targetVitals.spo2}%.`
        : `Clinical Report: Experiencing (${activeScenario.symptomsEn.slice(0, 2).join(', ')}) with HR ${activeScenario.targetVitals.hr} bpm and SpO2 ${activeScenario.targetVitals.spo2}%.`
      : null;

  const quickReplies =
    currentRole === 'Monitor'
      ? [
          ...(scenarioQuickReply ? [scenarioQuickReply] : []),
          ...(isAr
            ? [
                'تم استلام المؤشرات الحيوية الصباحية وجميعها ضمن النطاق الآمن.',
                'يرجى شرب 500 مل من محلول الإماهة وإعادة قياس ضغط الدم.',
                'تم تحديث بروتوكول التمارين والنوم في صفحة التوصيات الطبية.',
                'هل تشعر بأي دوار حركة أو أعراض تكيف مع الجاذبية الصغرى؟',
              ]
            : [
                'Morning vitals confirmed — all biometrics within safe operational limits.',
                'Please hydrate with 500ml electrolyte solution and re-test BP.',
                'Updated countermeasure protocol published in Medical Recommendations.',
                'Are you experiencing any motion sickness or microgravity adaptation?',
              ]),
        ]
      : [
          ...(scenarioQuickReply ? [scenarioQuickReply] : []),
          ...(isAr
            ? [
                'تم تسجيل المؤشرات الحيوية الصباحية بنجاح، أشعر باستقرار تام.',
                'أنهيت حصة التمارين الرياضية على السير المتحرك ومعدل النبض طبيعي.',
                'أشعر ببعض الإرهاق والصداع الخفيف بعد جولة العمل خارج المركبة.',
                'يرجى مراجعة بيانات النوم الأخيرة وإرشادات جدول المهام.',
              ]
            : [
                'Morning vitals logged successfully, feeling fully stable.',
                'Completed treadmill countermeasure session, pulse is nominal.',
                'Experiencing mild fatigue and light headache post-EVA sortie.',
                'Please review latest sleep architecture and shift directives.',
              ]),
        ];

  const renderIcon = (type: RecipientOption['iconType']) => {
    switch (type) {
      case 'doctor':
        return <Stethoscope className="w-4 h-4 text-cyan-300" />;
      case 'control':
        return <Radio className="w-4 h-4 text-emerald-300" />;
      case 'behavioral':
        return <BrainCircuit className="w-4 h-4 text-purple-300" />;
      case 'astronaut':
        return <User className="w-4 h-4 text-blue-300" />;
    }
  };

  return (
    <div className="glass-panel rounded-[22px] p-5 flex flex-col gap-4 animate-fade-in">
      {/* Top Consultation Channel Header */}
      <div className="pb-4 border-b border-white/10 flex flex-col gap-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          {/* Channel Title */}
          <div className="flex items-center gap-2.5">
            <div className="w-10 h-10 rounded-xl bg-cyan-500/20 border border-cyan-400/30 flex items-center justify-center text-cyan-300 shadow-sm">
              <MessageSquare className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white flex items-center gap-2">
                <span>{tx(isAr ? 'الاستشارة الطبية المباشرة والمحادثات' : 'Ground-Orbit Telemedicine')}</span>
                <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-extrabold bg-emerald-500/15 text-emerald-300 border border-emerald-400/30">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-ping" />
                  {tx(isOnline ? (isAr ? "متصل" : "Connected") : (isAr ? 'وضع غير متصل' : 'Offline'))}
                </span>
              </h3>
              <p className="text-xs text-slate-300">
                {tx(isAr
                  ? "ابقَ على تواصل مع فريقك الطبي."
                  : "Stay connected with your medical team.")}
              </p>
            </div>
          </div>

          {/* Current Sender Status Indicator */}
          <div className="flex items-center gap-2 bg-slate-900/80 px-3.5 py-1.5 rounded-xl border border-cyan-400/30 shadow-sm text-xs font-bold text-cyan-200">
            <span className="w-2 h-2 rounded-full bg-cyan-400 animate-pulse" />
            <span className="text-slate-400">{tx(isAr ? 'حسابك النشط:' : 'You:')}</span>
            <span className="text-white font-extrabold">{tx(currentUserName)}</span>
            <span className="text-[10px] font-mono text-cyan-300 px-1.5 py-0.5 rounded bg-white/10">
              {tx(currentUserCode || (currentRole === 'Monitor' ? 'MED-SURGEON' : 'AST-001'))}
            </span>
          </div>
        </div>

        {/* ── RECIPIENT SELECTOR (من تريد مراسلته؟) ── */}
        <div className="mt-1 p-3.5 rounded-2xl bg-slate-900/80 border border-cyan-400/25 flex flex-col gap-2.5">
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-bold text-white flex items-center gap-2">
              <Users className="w-4 h-4 text-cyan-400" />
              <span>{tx(isAr ? 'اختر المستقبل (من تريد إرسال الرسالة إليه؟):' : 'Select Target Recipient:')}</span>
            </span>
            <span className="text-[10px] text-cyan-300 font-semibold bg-cyan-500/15 border border-cyan-400/30 px-2 py-0.5 rounded-full">
              {tx(isAr ? 'انقر على أي شخص للتواصل معه مباشرة' : 'Click to direct messages')}
            </span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2">
            {availableRecipients.map((rec) => {
              const isSelected = rec.id === activeRecipient.id;
              return (
                <AsyncButton
                  key={rec.id}
                  type="button"
                  onClick={() => handleSelectRecipient(rec)}
                  className={`p-2.5 rounded-xl border text-start transition-all flex flex-col justify-between gap-1.5 cursor-pointer ${
                    isSelected
                      ? 'bg-gradient-to-br from-cyan-500/25 to-blue-600/25 border-cyan-400 shadow-[0_0_18px_rgba(34,211,238,0.3)] ring-1 ring-cyan-400'
                      : 'bg-slate-950/60 border-white/10 hover:border-cyan-400/40 hover:bg-slate-900/80'
                  }`}
                >
                  <div className="flex items-center justify-between gap-1.5">
                    <div className="w-7 h-7 rounded-lg bg-white/5 border border-white/10 flex items-center justify-center shrink-0">
                      {renderIcon(rec.iconType)}
                    </div>
                    {isSelected ? (
                      <span className="inline-flex items-center gap-1 text-[9px] font-extrabold px-1.5 py-0.5 rounded-full bg-cyan-400/20 text-cyan-200 border border-cyan-400/40">
                        <Check className="w-2.5 h-2.5" />
                        <span>{tx(isAr ? 'محدد' : 'Active')}</span>
                      </span>
                    ) : (
                      <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" title={tx("Online")} />
                    )}
                  </div>

                  <div>
                    <p className="text-xs font-bold text-white truncate">
                      {tx(isAr ? rec.titleAr : rec.titleEn)}
                    </p>
                    <p className="text-[10px] text-slate-300 truncate">
                      {tx(isAr ? rec.roleLabelAr : rec.roleLabelEn)}
                    </p>
                  </div>
                </AsyncButton>
              );
            })}
          </div>
        </div>

        {/* Active Channel Target Summary Banner */}
        <div className="p-2.5 rounded-xl bg-slate-900/60 border border-white/10 flex flex-wrap items-center justify-between gap-2 text-xs">
          <div className="flex items-center gap-2">
            <span className="text-slate-400">{tx(isAr ? 'القناة المفتوحة موجهة إلى:' : 'Direct Channel With:')}</span>
            <span className="font-extrabold text-cyan-200 flex items-center gap-1.5">
              <span className="w-2 h-2 rounded-full bg-emerald-400" />
              <span>{tx(isAr ? activeRecipient.titleAr : activeRecipient.titleEn)}</span>
              <span className="text-[10px] font-mono text-cyan-300 px-1.5 py-0.5 rounded bg-white/10">
                ({tx(isAr ? activeRecipient.roleLabelAr : activeRecipient.roleLabelEn)})
              </span>
            </span>
          </div>

          <div className="flex items-center gap-2">
            <span className="text-[10px] px-2 py-0.5 rounded bg-white/10 text-slate-300">
              {tx(chatMessages.length)} {tx(isAr ? 'رسالة في هذا السجل' : 'msgs in thread')}
            </span>
          </div>
        </div>
      </div>

      {/* Messages Stream Container */}
      <div className="flex flex-col gap-3 min-h-[280px] max-h-[360px] overflow-y-auto px-2 py-2">
        {chatMessages.length === 0 ? (
          <div className="flex-1 flex flex-col items-center justify-center py-12 text-center text-slate-400">
            <MessageSquare className="w-10 h-10 text-cyan-400/40 mb-2 stroke-[1.5]" />
            <p className="text-sm font-bold text-slate-200">
              {tx(isAr ? 'لا توجد رسائل سابقة في هذه القناة' : 'No previous messages in this channel')}
            </p>
            <p className="text-xs text-slate-400 mt-1 max-w-sm">
              {tx(isAr
                ? `هذه القناة مخصصة للمحادثة مع ${isAr ? activeRecipient.titleAr : activeRecipient.titleEn}. اكتب رسالتك بالأسفل للإرسال.`
                : `This channel routes directly to ${activeRecipient.titleEn}. Type your transmission below to begin.`)}
            </p>
          </div>
        ) : (
          chatMessages.map((msg) => {
            const isMine = msg.senderRole === currentRole;
            const isRead = msg.isRead || msg.readAt != null;

            return (
              <div
                key={msg.id || msg.clientGeneratedId}
                className={`flex flex-col max-w-[85%] sm:max-w-[72%] ${
                  isMine ? 'self-end items-end' : 'self-start items-start'
                }`}
              >
                {/* Sender Name, Recipient & Timestamp */}
                <div className="flex items-center gap-2 mb-1 px-1 flex-wrap">
                  <span className="text-[11px] font-bold text-slate-200">
                    {tx(isMine
                      ? isAr
                        ? `أنت (${currentUserName})`
                        : `You (${currentUserName})`
                      : msg.senderName ||
                        (msg.senderRole === 'Monitor'
                          ? isAr
                            ? 'د. كيسي ريفيرا'
                            : 'Dr. Casey Rivera'
                          : isAr
                          ? activeAstronaut.fullNameAr
                          : activeAstronaut.fullName))}
                  </span>

                  {msg.recipientName && (
                    <span className="text-[10px] text-cyan-300 bg-cyan-950/60 border border-cyan-400/30 px-1.5 py-0.2 rounded">
                      {tx(isAr ? `إلى: ${msg.recipientName}` : `To: ${msg.recipientName}`)}
                    </span>
                  )}

                  <span className="text-[10px] text-slate-400 flex items-center gap-1 font-mono">
                    <Clock className="w-2.5 h-2.5 text-slate-400" />
                    {tx(new Date(msg.sentAt).toLocaleTimeString(isAr ? 'ar-EG' : 'en-US', {
                      hour: '2-digit',
                      minute: '2-digit',
                    }))}
                  </span>
                </div>

                {/* Bubble */}
                <div
                  className={`px-4 py-2.5 rounded-2xl text-xs sm:text-sm leading-relaxed shadow-md ${
                    isMine
                      ? 'bg-gradient-to-r from-blue-600 to-cyan-600 text-white rounded-ee-sm'
                      : 'bg-slate-900/85 border border-white/15 text-slate-100 rounded-es-sm shadow-[0_4px_16px_rgba(0,0,0,0.4)]'
                  }`}
                >
                  {tx(msg.content)}
                </div>

                {/* Delivery / Read Status for sent messages */}
                {isMine && (
                  <div className="flex items-center gap-1 mt-1 px-1 text-[10px] text-cyan-300">
                    {msg.deliveryState === 'sending' ? (
                      <span className="text-slate-400">{tx(isAr ? 'جاري الإرسال...' : 'Sending...')}</span>
                    ) : msg.deliveryState === 'failed' ? (
                      <span className="text-rose-400">{tx(isAr ? 'تعذر الإرسال' : 'Failed')}</span>
                    ) : isRead ? (
                      <>
                        <CheckCheck className="w-3.5 h-3.5 text-emerald-400" />
                        <span className="text-emerald-300">{tx(isAr ? 'تمت القراءة' : 'Read')}</span>
                      </>
                    ) : (
                      <>
                        <Check className="w-3.5 h-3.5 text-cyan-400" />
                        <span>{tx(isAr ? 'تم التسليم' : 'Delivered')}</span>
                      </>
                    )}
                  </div>
                )}
              </div>
            );
          })
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Quick Suggested Replies */}
      <div className="space-y-1.5 pt-2 border-t border-white/10">
        <div className="flex items-center gap-1.5 text-[11px] font-semibold text-slate-400">
          <Sparkles className="w-3 h-3 text-cyan-400" />
          <span>{tx(isAr ? 'ردود وإجراءات سريعة مقترحة:' : 'Quick Suggested Transmissions:')}</span>
        </div>
        <div className="flex flex-wrap gap-2">
          {quickReplies.map((qr, idx) => (
            <AsyncButton
              key={idx}
              type="button"
              onClick={() => setMessageText(qr)}
              className="text-xs px-3 py-1.5 rounded-xl bg-slate-900/60 hover:bg-slate-800/80 border border-white/10 hover:border-cyan-400/40 text-slate-200 transition-all cursor-pointer text-start"
            >
              {tx(qr)}
            </AsyncButton>
          ))}
        </div>
      </div>

      {/* Compose & Transmit Form with Direct Recipient Chip */}
      <AsyncForm onSubmit={handleSend} className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2 pt-1">
        {/* Recipient Indicator Tag */}
        <div className="flex items-center gap-1.5 bg-slate-900/90 border border-cyan-400/40 px-3 py-2 rounded-xl text-xs font-bold text-cyan-200 shrink-0">
          <Send className="w-3.5 h-3.5 text-cyan-400 rotate-45" />
          <span className="text-slate-400">{tx(isAr ? 'إلى:' : 'To:')}</span>
          <span className="text-white truncate max-w-[140px]">
            {tx(isAr ? activeRecipient.titleAr : activeRecipient.titleEn)}
          </span>
        </div>

        <input
          type="text"
          value={messageText}
          onChange={(e) => setMessageText(e.target.value)}
          placeholder={
            tx(isAr
              ? `اكتب رسالتك الموجهة إلى ${activeRecipient.titleAr}...`
              : `Transmit message to ${activeRecipient.titleEn}...`)
          }
          className="flex-1 rounded-xl bg-slate-900/80 border border-white/15 px-4 py-2.5 text-sm text-white placeholder-slate-400 focus:outline-none focus:border-cyan-400"
        />

        <AsyncButton
          type="submit"
          disabled={!messageText.trim() || sending}
          className="px-5 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white font-bold text-sm flex items-center justify-center gap-2 shadow-lg transition-all disabled:opacity-50 cursor-pointer"
        >
          <Send className="w-4 h-4" />
          <span>{tx(isAr ? 'إرسال' : 'Send')}</span>
        </AsyncButton>
      </AsyncForm>
    </div>
  );
};