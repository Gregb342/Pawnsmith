import { useTranslation } from 'react-i18next';

import type { CatalogDto, Labels } from '../api/types';

/**
 * A label of the catalogue or of the style library, in the interface's
 * language. Those labels come from the API (DEC-106), not from the
 * translation files; English is the fallback, then the raw value.
 */
export function useLabel(): (labels: Labels | undefined, fallback: string) => string {
  const { i18n } = useTranslation();
  const language = i18n.resolvedLanguage ?? 'fr';

  return (labels, fallback) => labels?.[language] ?? labels?.en ?? fallback;
}

/** The label of a value of the catalogue, or the value itself when the catalogue does not know it. */
export function useValueLabel(catalog: CatalogDto): (key: string, value: string) => string {
  const label = useLabel();

  return (key, value) => {
    const entry = catalog.parameters.find((parameter) => parameter.key === key)?.entries.find((each) => each.value === value);
    return label(entry?.labels, value);
  };
}

/** An API code in words (§I.10.4): the translation when there is one, the code itself otherwise — never an empty string. */
export function useErrorText(): (code: string) => string {
  const { t } = useTranslation();

  return (code) => t(`errors.${code}`, { defaultValue: code });
}
