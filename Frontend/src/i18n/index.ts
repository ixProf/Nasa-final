import { ar } from './ar';
import { en } from './en';

export type Language = 'ar' | 'en';
export type Translations = typeof ar;

export const translations: Record<Language, Translations> = {
  ar,
  en,
};

