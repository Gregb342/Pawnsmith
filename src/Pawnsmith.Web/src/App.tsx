import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';

import { LanguageSelector } from './LanguageSelector';

/**
 * A.5 — the whole front of T1: one page, the product name, a tagline, and a
 * working language selector. No API call, no business component.
 */
export function App() {
  const { t, i18n } = useTranslation();

  // index.html ships an empty <title> and an empty lang attribute; both are
  // owned by the catalogue and follow the selected language.
  useEffect(() => {
    document.title = t('app.documentTitle');
    document.documentElement.lang = i18n.resolvedLanguage ?? 'fr';
  }, [t, i18n.resolvedLanguage]);

  return (
    <main>
      <header>
        <h1>{t('app.name')}</h1>
        <p>{t('app.tagline')}</p>
      </header>
      <LanguageSelector />
    </main>
  );
}
