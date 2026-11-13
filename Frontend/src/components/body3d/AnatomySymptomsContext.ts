import { createContext } from 'react';

export interface AnatomySymptom { code: string; label: string; severity?: string }
export const AnatomySymptomsContext = createContext<AnatomySymptom[]>([]);
