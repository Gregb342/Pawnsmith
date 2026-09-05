import i18next from 'i18next';
import { initReactI18next } from 'react-i18next';

import en from './locales/en.json';
import fr from './locales/fr.json';

/**
 * The two languages of v1. Declared as a tuple so that adding a catalogue
 * without adding it here is a compile error rather than a silent omission.
 */
export const supportedLanguages = ['fr', 'en'] as const;

export type SupportedLanguage = (typeof supportedLanguages)[number];

const catalogues: Record<SupportedLanguage, { translation: typeof fr }> = {
  fr: { translation: fr },
  en: { translation: en },
};

// No browser language detector: that would be a dependency beyond the ones the
// specification lists, for a behaviour the language selector already covers.
void i18next.use(initReactI18next).init({
  resources: catalogues,
  lng: 'fr',
  fallbackLng: 'fr',
  interpolation: {
    // React escapes interpolated values already.
    escapeValue: false,
  },
});

export default i18next;
