import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { api } from '../api/client';
import type { CatalogParameterDto } from '../api/types';
import { useAppData } from '../app/AppData';
import { codeOf } from '../app/SaveStatus';
import { ErrorNotice } from '../components/ErrorNotice';
import { useLabel } from '../components/text';
import { NewEntryDialog } from './NewEntryDialog';

/**
 * The catalogue of the universe, list by list (§I.4.2).
 *
 * Shipped items are read-only: they come from config/ and are the same for
 * everyone. Personal items, added here or by "Other…" in a blueprint, can be
 * removed; a blueprint that used one keeps its value, shown as out of the
 * list (DEC-107, DEC-056).
 */
export function CatalogScreen() {
  const { t } = useTranslation();
  const { catalog, setCatalog } = useAppData();
  const label = useLabel();
  const [adding, setAdding] = useState<CatalogParameterDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function remove(key: string, value: string) {
    try {
      setCatalog(await api.removeCatalogEntry(catalog.universe, key, value));
      setError(null);
    } catch (failure) {
      setError(codeOf(failure));
    }
  }

  return (
    <>
      <div className="head">
        <div className="stack">
          <h2>{t('catalog.title')}</h2>
          <p className="muted">{t('catalog.lead', { universe: t(`universes.${catalog.universe}`) })}</p>
        </div>
      </div>

      <ErrorNotice code={error} />

      {catalog.parameters.map((parameter) => (
        <section className="panel" key={parameter.key}>
          <div className="head">
            <h3>{label(parameter.labels, parameter.key)}</h3>
            <button className="btn sm" onClick={() => setAdding(parameter)}>{t('catalog.add')}</button>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{t('catalog.label')}</th>
                  <th>{t('catalog.value')}</th>
                  <th>{t('catalog.fragment')}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {parameter.entries.map((entry) => (
                  <tr key={entry.value}>
                    <td>{label(entry.labels, entry.value)}</td>
                    <td className="mono">{entry.value}</td>
                    <td className="mono small">{entry.fragment}</td>
                    <td>
                      {entry.origin === 'Personal' ? (
                        <span className="row">
                          <span className="chip">{t('common.personal')}</span>
                          <button className="btn sm danger" onClick={() => void remove(parameter.key, entry.value)}>{t('common.delete')}</button>
                        </span>
                      ) : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ))}

      <NewEntryDialog parameter={adding} onClose={() => setAdding(null)} onAdded={() => undefined} />
    </>
  );
}
