import { useState } from 'react';
import type { FormEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { api } from '../api/client';
import { useAppData } from '../app/AppData';
import { codeOf } from '../app/SaveStatus';
import { ErrorNotice } from '../components/ErrorNotice';
import { useErrorText } from '../components/text';

/**
 * The Générateur page (§I.5, DEC-108): the state of ComfyUI, its address,
 * and a test.
 *
 * The address is saved on the server, in the user directory, and wins over
 * the environment at the next start. The workflow stays a configuration
 * file — ComfyUI's export, not a setting of this screen — and its framing
 * clause is never shown (DEC-029): the page only says whether one is read.
 */
export function GeneratorScreen() {
  const { t } = useTranslation();
  const { generator, setGenerator, refreshGenerator } = useAppData();
  const errorText = useErrorText();
  const [address, setAddress] = useState(generator?.address ?? '');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function save(next: string | null) {
    setBusy(true);
    try {
      const updated = await api.setGeneratorAddress(next);
      setGenerator(updated);
      setAddress(updated.address ?? '');
      setError(null);
    } catch (failure) {
      setError(codeOf(failure));
    } finally {
      setBusy(false);
    }
  }

  async function test() {
    setBusy(true);
    await refreshGenerator();
    setBusy(false);
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    void save(address.trim());
  }

  const state = generator?.state;

  return (
    <>
      <div className="head">
        <div className="stack">
          <h2>{t('generator.title')}</h2>
          <p className="muted">{t('generator.lead')}</p>
        </div>
      </div>

      <div className="grid-2">
        <section className="panel">
          <h3>{t('generator.stateTitle')}</h3>
          <span className="line">
            <span className={`chip ${state === 'Available' ? 'ok' : state === undefined ? '' : 'err'}`}>
              {state === undefined ? t('generator.unknown') : t(`generator.states.${state}`)}
            </span>
            <button className="btn sm" disabled={busy} onClick={() => void test()}>{t('generator.test')}</button>
          </span>
          {generator?.code !== null && generator?.code !== undefined ? (
            <p className="small">{errorText(generator.code)} <span className="mono muted">{generator.code}</span></p>
          ) : null}
          <dl className="kv">
            <dt>{t('generator.workflow')}</dt>
            <dd>{generator?.framingClause !== null && generator?.framingClause !== undefined ? t('generator.workflowRead') : t('generator.workflowMissing')}</dd>
          </dl>
          <span className="hint">{t('generator.workflowHint')}</span>
        </section>

        <form className="panel" onSubmit={submit}>
          <h3>{t('generator.address')}</h3>
          <div className="field">
            <label htmlFor="generator-address">{t('generator.addressLabel')}</label>
            <input id="generator-address" className="mono" placeholder="http://127.0.0.1:8188" value={address} onChange={(event) => setAddress(event.target.value)} />
            <span className="hint">{t('generator.addressHint')}</span>
          </div>
          <ErrorNotice code={error} />
          <div className="row">
            <button type="submit" className="btn primary" disabled={busy || address.trim() === ''}>{t('generator.saveAddress')}</button>
            <button type="button" className="btn" disabled={busy || generator?.address === null} onClick={() => void save(null)}>{t('generator.clearAddress')}</button>
          </div>
          <span className="hint">{t('generator.runningBatches')}</span>
        </form>
      </div>
    </>
  );
}
