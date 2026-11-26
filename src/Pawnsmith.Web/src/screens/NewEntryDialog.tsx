import { useState } from 'react';
import type { FormEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { api } from '../api/client';
import type { CatalogParameterDto } from '../api/types';
import { useAppData } from '../app/AppData';
import { codeOf } from '../app/SaveStatus';
import { Dialog } from '../components/Dialog';
import { ErrorNotice } from '../components/ErrorNotice';
import { useLabel } from '../components/text';

/**
 * "Other…" in a list: a complete new item, and nothing less (§I.4.1,
 * DEC-107) — an English value, a label in each language, and the English
 * phrase that goes into the prompt.
 *
 * The phrase starts from an example of the same list, because it carries the
 * pose constraint (DEC-064): an axe is "held vertically flat against the
 * body", so that it stays inside the silhouette. The application checks that
 * the phrase exists, not that it says the pose; the hint says so.
 */
export function NewEntryDialog(props: { parameter: CatalogParameterDto | null; onClose: () => void; onAdded: (value: string) => void }) {
  const { t } = useTranslation();
  const label = useLabel();
  const parameter = props.parameter;

  return (
    <Dialog open={parameter !== null} title={t('newEntry.title', { list: parameter ? label(parameter.labels, parameter.key) : '' })} onClose={props.onClose}>
      {parameter !== null ? <EntryForm parameter={parameter} onClose={props.onClose} onAdded={props.onAdded} /> : null}
    </Dialog>
  );
}

function EntryForm(props: { parameter: CatalogParameterDto; onClose: () => void; onAdded: (value: string) => void }) {
  const { t } = useTranslation();
  const { catalog, setCatalog } = useAppData();
  const example = props.parameter.entries.find((entry) => entry.origin === 'Shipped')?.fragment ?? '';
  const [value, setValue] = useState('');
  const [french, setFrench] = useState('');
  const [english, setEnglish] = useState('');
  const [fragment, setFragment] = useState(example);
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      const updated = await api.addCatalogEntry(catalog.universe, {
        key: props.parameter.key,
        value: value.trim(),
        labels: { fr: french.trim(), en: english.trim() },
        fragment: fragment.trim(),
      });
      setCatalog(updated);
      props.onAdded(value.trim());
      props.onClose();
    } catch (failure) {
      setError(codeOf(failure));
    }
  }

  const complete = [value, french, english, fragment].every((field) => field.trim() !== '');

  return (
    <form onSubmit={(event) => void submit(event)}>
      <p className="muted">{t('newEntry.lead')}</p>
      <div className="row">
        <div className="field">
          <label htmlFor="entry-fr">{t('newEntry.french')}</label>
          <input id="entry-fr" required value={french} onChange={(event) => setFrench(event.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="entry-en">{t('newEntry.english')}</label>
          <input id="entry-en" required value={english} onChange={(event) => setEnglish(event.target.value)} />
        </div>
      </div>
      <div className="field">
        <label htmlFor="entry-value">{t('newEntry.value')}</label>
        <input id="entry-value" className="mono" required value={value} onChange={(event) => setValue(event.target.value)} />
        <span className="hint">{t('newEntry.valueHint')}</span>
      </div>
      <div className="field">
        <label htmlFor="entry-fragment">{t('newEntry.fragment')}</label>
        <textarea id="entry-fragment" className="mono" required value={fragment} onChange={(event) => setFragment(event.target.value)} />
        <span className="hint">{t('newEntry.fragmentHint')}</span>
      </div>
      <ErrorNotice code={error} />
      <div className="row">
        <button type="submit" className="btn primary" disabled={!complete}>{t('newEntry.add')}</button>
        <button type="button" className="btn" onClick={props.onClose}>{t('common.cancel')}</button>
      </div>
    </form>
  );
}
