import type { ChangeEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { supportedLanguages, type SupportedLanguage } from './i18n';

/**
 * A.5 — the language selector is the only interactive element of the skeleton.
 * Its own labels come from the catalogue, including the language names.
 */
export function LanguageSelector() {
  const { t, i18n } = useTranslation();

  function handleChange(event: ChangeEvent<HTMLSelectElement>) {
    void i18n.changeLanguage(event.target.value as SupportedLanguage);
  }

  return (
    <label className="language-selector">
      <span>{t('language.label')}</span>
      <select value={i18n.resolvedLanguage} onChange={handleChange}>
        {supportedLanguages.map((language) => (
          <option key={language} value={language}>
            {t(`language.names.${language}`)}
          </option>
        ))}
      </select>
    </label>
  );
}
