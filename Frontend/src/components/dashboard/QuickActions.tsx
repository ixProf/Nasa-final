import { useUiText } from "../../i18n/useUiText";
import React from 'react';
import { Plus, FileText, Zap } from 'lucide-react';
import { useLanguage } from '../../context/LanguageContext';

interface QuickActionsProps {
  onNavigateRecord: () => void;
  onNavigateReport: () => void;
}

export const QuickActions: React.FC<QuickActionsProps> = ({
  onNavigateRecord,
  onNavigateReport,
}) => {
  const tx = useUiText();
  const { t } = useLanguage();

  return (
    <div className="glass-panel rounded-3xl p-5 md:p-6 flex flex-col justify-between h-full transition-all duration-300 relative overflow-hidden">
      {/* Header */}
      <div className="flex items-center gap-2 mb-4 z-10">
        <Zap className="w-5 h-5 text-cyan-400 fill-cyan-400/20" />
        <h3 className="text-sm md:text-base font-bold text-white">
          {tx(t.quickActions.title)}
        </h3>
      </div>

      {/* Action Buttons Grid */}
      <div className="grid grid-cols-2 gap-3.5 z-10 my-auto">
        {/* Button 1: Record New Reading */}
        <button
          onClick={onNavigateRecord}
          className="glass-action-primary text-white rounded-2xl p-5 flex flex-col items-center justify-center gap-3 transition-all duration-300 hover:-translate-y-1 active:translate-y-0 group"
        >
          <div className="w-11 h-11 rounded-full bg-white text-blue-600 flex items-center justify-center shadow-[0_0_18px_rgba(255,255,255,0.75)] group-hover:scale-110 transition-transform duration-300">
            <Plus className="w-6 h-6 stroke-[3]" />
          </div>
          <span className="text-xs md:text-sm font-bold text-center">
            {tx(t.quickActions.recordNew)}
          </span>
        </button>

        {/* Button 2: View Report */}
        <button
          onClick={onNavigateReport}
          className="glass-panel-subtle hover:bg-white/10 border border-cyan-300/20 hover:border-cyan-300/45 rounded-2xl p-5 flex flex-col items-center justify-center gap-3 text-slate-100 hover:text-white transition-all duration-300 hover:-translate-y-1 active:translate-y-0 group"
        >
          <div className="w-11 h-11 rounded-2xl bg-white/10 border border-white/20 flex items-center justify-center text-cyan-300 group-hover:scale-110 transition-transform">
            <FileText className="w-5 h-5 stroke-[2.2]" />
          </div>
          <span className="text-xs md:text-sm font-bold text-center">
            {tx(t.quickActions.viewReport)}
          </span>
        </button>
      </div>
    </div>
  );
};
