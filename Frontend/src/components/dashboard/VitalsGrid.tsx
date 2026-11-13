import React from 'react';
import { VitalCard } from './VitalCard';
import { useTelemetry } from '../../context/TelemetryContext';

interface VitalsGridProps {
  onNavigateToVitals?: () => void;
}

export const VitalsGrid: React.FC<VitalsGridProps> = ({ onNavigateToVitals }) => {
  const { vitals } = useTelemetry();

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 w-full">
      <VitalCard data={vitals.HR} onSelect={onNavigateToVitals} />
      <VitalCard data={vitals.SPO2} onSelect={onNavigateToVitals} />
      <VitalCard data={vitals.BP} onSelect={onNavigateToVitals} />
      <VitalCard data={vitals.TEMP} onSelect={onNavigateToVitals} />
    </div>
  );
};
