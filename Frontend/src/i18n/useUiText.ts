import { useLanguage } from '../context/LanguageContext';
import { translateUiText } from './uiText';
export function useUiText() {
  const { language } = useLanguage();
  return (value: string | number | null | undefined) => translateUiText(value, language);
}
