import { useState } from 'react';
import type { FormEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { api } from '../api/client';
import type { ProjectSettings } from '../api/client';
import type { ArchiveProfile, ProjectDto, StyleDto, StylePresetDto } from '../api/types';
import { useAppData } from '../app/AppData';
import { useProject } from '../app/ProjectData';
import type { Route } from '../app/router';
import { codeOf, useSaveStatus } from '../app/SaveStatus';
import { Dialog } from '../components/Dialog';
import { ErrorNotice } from '../components/ErrorNotice';
import { GeometryIcon } from '../components/GeometryIcon';
import { TextField } from '../components/TextField';
import { useLabel } from '../components/text';

/** Everything the settings route takes, as the project holds it now. The palette goes back as received (DEC-110). */
function settingsOf(project: ProjectDto): ProjectSettings {
  return {
    name: project.name,
    universe: project.universe,
    geometry: project.geometry,
    paperFormat: project.paperFormat,
    style: project.style,
    calibrationOverrides: project.calibrationOverrides,
  };
}

/** A number typed in a field, or null when it is empty or not a number. */
function numberOrNull(text: string): number | null {
  const value = Number.parseFloat(text.replace(',', '.'));
  return text.trim() === '' || Number.isNaN(value) ? null : value;
}

/**
 * The Projet step: name, universe, geometry, paper, tabs, style, archives.
 *
 * Each field is saved when it is left (DEC-111). Once a blueprint has a
 * proposal, the universe and the style no longer change (DEC-112): the
 * fields are shown disabled, with the way out — duplicating the project.
 */
export function ProjectScreen(props: { navigate: (route: Route) => void }) {
  const { t } = useTranslation();
  const { configuration } = useAppData();
  const { project, setProject } = useProject();
  const { run } = useSaveStatus();
  const frozen = project.universeAndStyleFrozen;

  async function save(change: Partial<ProjectSettings>) {
    const saved = await run(() => api.saveSettings(project.folder, { ...settingsOf(project), ...change }));
    if (saved !== undefined) {
      setProject(saved);
    }
  }

  const overrides = project.calibrationOverrides;

  return (
    <>
      <div className="head">
        <div className="stack">
          <h2>{t('project.title')}</h2>
          <p className="muted">{t('project.lead')}</p>
        </div>
      </div>

      <div className="three">
        <section className="panel">
          <TextField key={`name-${project.name}`} label={t('project.name')} initial={project.name} onCommit={(name) => void save({ name })} />

          <div className="row">
            <div className="field">
              <label htmlFor="universe">{t('project.universe')}</label>
              <select id="universe" value={project.universe} disabled={frozen} onChange={(event) => void save({ universe: event.target.value as ProjectDto['universe'] })}>
                {configuration.universes.map((each) => <option key={each} value={each}>{t(`universes.${each}`)}</option>)}
              </select>
            </div>
            <div className="field">
              <label htmlFor="paper">{t('project.paper')}</label>
              <select id="paper" value={project.paperFormat} onChange={(event) => void save({ paperFormat: event.target.value })}>
                {configuration.paperFormats.map((format) => (
                  <option key={format.name} value={format.name}>{t('project.paperFormat', { name: format.name, width: format.widthMm, height: format.heightMm })}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="group">
            <span className="lbl">{t('project.geometry')}</span>
            <div className="cards">
              {configuration.geometries.map((geometry) => (
                <button key={geometry} className="card" aria-pressed={project.geometry === geometry} onClick={() => void save({ geometry })}>
                  <GeometryIcon geometry={geometry} />
                  <strong>{t(`geometries.${geometry}.name`)}</strong>
                  <span className="small muted">{t(`geometries.${geometry}.hint`)}</span>
                </button>
              ))}
            </div>
          </div>

          {/* The tab dimensions only mean something with a tab (DEC-040). */}
          {project.geometry === 'TabAndSocket' ? (
            <div className="group">
              <span className="lbl">{t('project.tabs')}</span>
              <span className="hint">{t('project.tabsHint')}</span>
              <div className="row">
                <TextField
                  key={`tw-${overrides.tabWidthMm ?? ''}`}
                  label={t('project.tabWidth')}
                  type="number"
                  placeholder={t('project.fromCalibration')}
                  initial={overrides.tabWidthMm?.toString() ?? ''}
                  onCommit={(text) => void save({ calibrationOverrides: { ...overrides, tabWidthMm: numberOrNull(text) } })}
                />
                <TextField
                  key={`th-${overrides.tabHeightMm ?? ''}`}
                  label={t('project.tabHeight')}
                  type="number"
                  placeholder={t('project.fromCalibration')}
                  initial={overrides.tabHeightMm?.toString() ?? ''}
                  onCommit={(text) => void save({ calibrationOverrides: { ...overrides, tabHeightMm: numberOrNull(text) } })}
                />
              </div>
            </div>
          ) : null}
        </section>

        <StylePanel project={project} frozen={frozen} save={save} navigate={props.navigate} />

        <Archives folder={project.folder} />
      </div>
    </>
  );
}

function StylePanel(props: {
  project: ProjectDto;
  frozen: boolean;
  save: (change: Partial<ProjectSettings>) => Promise<void>;
  navigate: (route: Route) => void;
}) {
  const { t } = useTranslation();
  const { styles, setStyles } = useAppData();
  const { run } = useSaveStatus();
  const label = useLabel();
  const [saving, setSaving] = useState(false);
  const [duplicating, setDuplicating] = useState(false);
  const { project, frozen } = props;
  const style = project.style;

  function use(preset: StylePresetDto) {
    // A copy, not a link (DEC-110): the project keeps these words even if
    // the library changes. The palette is kept as it was.
    void props.save({ style: { name: label(preset.names, preset.id), styleClause: preset.styleClause, negativeClause: preset.negativeClause, palette: style.palette } });
  }

  async function remove(preset: StylePresetDto) {
    const updated = await run(() => api.removeStyle(project.universe, preset.id));
    if (updated !== undefined) {
      setStyles(updated);
    }
  }

  return (
    <section className="panel center">
      <h3>{t('project.style')}</h3>

      {frozen ? (
        <div className="notice info">
          <span>{t('project.styleFrozen')}</span>
          <div>
            <button className="btn sm" onClick={() => setDuplicating(true)}>{t('project.duplicate')}</button>
          </div>
        </div>
      ) : null}

      <TextField
        key={`sn-${style.name}`}
        label={t('project.styleName')}
        initial={style.name}
        disabled={frozen}
        onCommit={(name) => void props.save({ style: { ...style, name } })}
      />
      <TextField
        key={`sc-${style.styleClause}`}
        label={t('project.styleClause')}
        hint={t('project.styleClauseHint')}
        initial={style.styleClause}
        multiline
        mono
        disabled={frozen}
        onCommit={(styleClause) => void props.save({ style: { ...style, styleClause } })}
      />

      <details>
        <summary>{t('project.advanced')}</summary>
        <TextField
          key={`neg-${style.negativeClause}`}
          label={t('project.negative')}
          hint={t('project.negativeHint')}
          initial={style.negativeClause}
          multiline
          mono
          disabled={frozen}
          onCommit={(negativeClause) => void props.save({ style: { ...style, negativeClause } })}
        />
      </details>

      <div className="group">
        <div className="line">
          <span className="lbl">{t('project.library')}</span>
          <button className="btn sm" disabled={frozen || style.styleClause.trim() === ''} onClick={() => setSaving(true)}>
            {t('project.saveAsStyle')}
          </button>
        </div>
        <span className="hint">{t('project.libraryHint')}</span>
        <div className="list">
          {styles.map((preset) => (
            <div key={preset.id} className="item" aria-current={preset.styleClause === style.styleClause}>
              <div className="top">
                <span className="name">{label(preset.names, preset.id)}</span>
                <span className="meta">
                  {preset.origin === 'Personal' ? <span className="chip">{t('common.personal')}</span> : null}
                  <button className="btn sm" disabled={frozen} onClick={() => use(preset)}>{t('project.useStyle')}</button>
                  {preset.origin === 'Personal' ? (
                    <button className="btn sm danger" onClick={() => void remove(preset)}>{t('common.delete')}</button>
                  ) : null}
                </span>
              </div>
              <span className="small muted mono">{preset.styleClause}</span>
            </div>
          ))}
        </div>
      </div>

      <SaveStyleDialog open={saving} project={project} onClose={() => setSaving(false)} />
      <DuplicateDialog open={duplicating} project={project} onClose={() => setDuplicating(false)} navigate={props.navigate} />
    </section>
  );
}

function SaveStyleDialog(props: { open: boolean; project: ProjectDto; onClose: () => void }) {
  const { t } = useTranslation();
  const { setStyles } = useAppData();
  const [name, setName] = useState('');
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const style = props.project.style;
    try {
      setStyles(await api.addStyle(props.project.universe, { name: name.trim(), styleClause: style.styleClause, negativeClause: style.negativeClause }));
      props.onClose();
    } catch (failure) {
      setError(codeOf(failure));
    }
  }

  return (
    <Dialog open={props.open} title={t('project.saveAsStyle')} onClose={props.onClose}>
      <form onSubmit={(event) => void submit(event)}>
        <div className="field">
          <label htmlFor="style-name">{t('project.styleName')}</label>
          <input id="style-name" required value={name} onChange={(event) => setName(event.target.value)} />
        </div>
        <div className="prompt">{props.project.style.styleClause}</div>
        <ErrorNotice code={error} />
        <div className="row">
          <button type="submit" className="btn primary" disabled={name.trim() === ''}>{t('common.save')}</button>
          <button type="button" className="btn" onClick={props.onClose}>{t('common.cancel')}</button>
        </div>
      </form>
    </Dialog>
  );
}

function DuplicateDialog(props: { open: boolean; project: ProjectDto; onClose: () => void; navigate: (route: Route) => void }) {
  const { t } = useTranslation();
  const { styles } = useAppData();
  const label = useLabel();
  const [name, setName] = useState('');
  const [presetId, setPresetId] = useState('');
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const preset = styles.find((each) => each.id === presetId);
    const style: StyleDto | null =
      preset === undefined
        ? null
        : { name: label(preset.names, preset.id), styleClause: preset.styleClause, negativeClause: preset.negativeClause, palette: '' };
    try {
      const copy = await api.duplicate(props.project.folder, name.trim(), style);
      props.onClose();
      props.navigate({ name: 'project', folder: copy.folder, step: 'project' });
    } catch (failure) {
      setError(codeOf(failure));
    }
  }

  return (
    <Dialog open={props.open} title={t('project.duplicate')} onClose={props.onClose}>
      <form onSubmit={(event) => void submit(event)}>
        <p className="muted">{t('project.duplicateLead')}</p>
        <div className="field">
          <label htmlFor="copy-name">{t('project.name')}</label>
          <input id="copy-name" required value={name} onChange={(event) => setName(event.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="copy-style">{t('project.style')}</label>
          <select id="copy-style" value={presetId} onChange={(event) => setPresetId(event.target.value)}>
            <option value="">{t('project.keepStyle')}</option>
            {styles.map((preset) => <option key={preset.id} value={preset.id}>{label(preset.names, preset.id)}</option>)}
          </select>
        </div>
        <ErrorNotice code={error} />
        <div className="row">
          <button type="submit" className="btn primary" disabled={name.trim() === ''}>{t('project.duplicateAction')}</button>
          <button type="button" className="btn" onClick={props.onClose}>{t('common.cancel')}</button>
        </div>
      </form>
    </Dialog>
  );
}

function Archives(props: { folder: string }) {
  const { t } = useTranslation();
  const { run } = useSaveStatus();

  const profiles: ArchiveProfile[] = ['Backup', 'Share'];

  return (
    <section className="panel">
      <h3>{t('project.archives')}</h3>
      {profiles.map((profile) => (
        <div key={profile} className="line">
          <div className="stack">
            <strong>{t(`project.profiles.${profile}.name`)}</strong>
            <span className="small muted">{t(`project.profiles.${profile}.hint`)}</span>
          </div>
          <button className="btn" onClick={() => void run(() => api.exportArchive(props.folder, profile))}>{t('project.export')}</button>
        </div>
      ))}
    </section>
  );
}
