import type { ChangeEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { useAppData } from '../app/AppData';
import type { Route, Step } from '../app/router';
import { steps } from '../app/router';
import { useSaveStatus } from '../app/SaveStatus';
import { supportedLanguages } from '../i18n/config';
import { useErrorText } from './text';

/** The bar at the top of every page (§I.10.1). */
export function TopBar(props: { route: Route; projectName: string | null; navigate: (route: Route) => void }) {
  const { t, i18n } = useTranslation();
  const { generator } = useAppData();

  // ComfyUI's state in words (DEC-105): connected, unreachable, not set up.
  const state = generator?.state;
  const dot = state === 'Available' ? 'ok' : state === undefined ? '' : 'off';
  const label = state === undefined ? t('generator.unknown') : t(`generator.states.${state}`);

  function changeLanguage(event: ChangeEvent<HTMLSelectElement>) {
    void i18n.changeLanguage(event.target.value);
  }

  return (
    <header className="topbar">
      <button className="brand" onClick={() => props.navigate({ name: 'projects' })}>
        <svg viewBox="0 0 24 24" aria-hidden="true">
          <path d="M8 3h8v18H8z" fill="none" stroke="currentColor" strokeWidth="1.6" />
          <path d="M8 12h8" stroke="var(--accent)" strokeWidth="1.6" strokeDasharray="2 1.5" />
          <path d="M10 21h4v2h-4z" fill="currentColor" />
        </svg>
        {t('app.name')}
      </button>
      {props.projectName !== null ? (
        <button className="project-pill" onClick={() => props.navigate({ name: 'projects' })}>
          <span className="label">{t('shell.project')}</span>
          <strong>{props.projectName}</strong>
          <span aria-hidden="true">▾</span>
        </button>
      ) : null}
      <span className="spacer" />
      <button className="status-chip" onClick={() => props.navigate({ name: 'generator' })}>
        <span className={`dot ${dot}`} />
        {label}
      </button>
      <button className="linkish" onClick={() => props.navigate({ name: 'catalog' })}>{t('shell.catalog')}</button>
      <button className="linkish" onClick={() => props.navigate({ name: 'logs' })}>{t('shell.logs')}</button>
      <label className="language">
        <span className="muted small">{t('language.label')} </span>
        <select value={i18n.resolvedLanguage} onChange={changeLanguage}>
          {supportedLanguages.map((language) => (
            <option key={language} value={language}>{t(`language.names.${language}`)}</option>
          ))}
        </select>
      </label>
    </header>
  );
}

/** The five steps, centred, with the autosave indicator (§15.1, DEC-111). */
export function StepRail(props: { folder: string; step: Step; navigate: (route: Route) => void }) {
  const { t } = useTranslation();
  const { state } = useSaveStatus();
  const errorText = useErrorText();

  return (
    <nav className="rail" aria-label={t('shell.steps')}>
      <div className="steps">
        {steps.map((step, index) => (
          <button
            key={step}
            className="step"
            aria-current={props.step === step ? 'page' : undefined}
            onClick={() => props.navigate({ name: 'project', folder: props.folder, step })}
          >
            <span className="n">{index + 1}</span>
            {t(`steps.${step}`)}
          </button>
        ))}
      </div>
      <span className={`save-state ${state.kind === 'failed' ? 'failed' : ''}`} role="status">
        {state.kind === 'saving' ? t('save.saving') : null}
        {state.kind === 'saved' ? t('save.saved') : null}
        {state.kind === 'failed' ? errorText(state.code) : null}
      </span>
    </nav>
  );
}
