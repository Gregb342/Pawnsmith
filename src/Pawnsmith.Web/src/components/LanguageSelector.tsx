import type { ChangeEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { supportedLanguages } from '../i18n/config';

/**
 * A.5 — the language selector is the only interactive element of the skeleton.
 * Its own labels come from the catalogue, including the language names.
 */
export function LanguageSelector() {
  const { t, i18n } = useTranslation();

  function handleChange(event: ChangeEvent<HTMLSelectElement>) {
    // No cast to SupportedLanguage here. changeLanguage accepts any string, so
    // the assertion asserted nothing and only looked like a guarantee — the
    // real guarantee is that the options below are built from
    // supportedLanguages, which is the tuple that defines the type.
    void i18n.changeLanguage(event.target.value);
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
