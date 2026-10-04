import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { api } from '../api/client';
import type { Geometry, ProjectListingDto, Universe } from '../api/types';
import { useAppData } from '../app/AppData';
import type { Route } from '../app/router';
import { codeOf } from '../app/SaveStatus';
import { ErrorNotice } from '../components/ErrorNotice';
import { GeometryIcon } from '../components/GeometryIcon';

/**
 * The list of projects, their creation and the import of an archive.
 *
 * A project is addressed by its folder (DEC-083), shown under its name. Two
 * folders with the same projectId are two copies — an archive imported
 * twice; the list says so instead of merging them (DEC-047, the question T2
 * left to the front). An unreadable project stays listed with its code in
 * words: it does not vanish (DEC-056).
 */
export function ProjectsScreen(props: { navigate: (route: Route) => void }) {
  const { t, i18n } = useTranslation();
  const { configuration } = useAppData();
  const [listings, setListings] = useState<ProjectListingDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  useEffect(() => {
    api.projects().then(setListings, (failure: unknown) => setError(codeOf(failure)));
  }, []);

  const open = (folder: string) => props.navigate({ name: 'project', folder, step: 'project' });

  const copies = new Map<string, number>();
  for (const listing of listings ?? []) {
    if (listing.projectId !== null) {
      copies.set(listing.projectId, (copies.get(listing.projectId) ?? 0) + 1);
    }
  }

  const date = new Intl.DateTimeFormat(i18n.resolvedLanguage, { dateStyle: 'medium', timeStyle: 'short' });

  return (
    <>
      <div className="head">
        <div className="stack">
          <h2>{t('projects.title')}</h2>
          <p className="muted">{t('projects.lead')}</p>
        </div>
        <button className="btn primary" onClick={() => setCreating(true)}>{t('projects.new')}</button>
      </div>

      <ErrorNotice code={error} />

      {creating ? <NewProject universes={configuration.universes} onCreated={open} onCancel={() => setCreating(false)} /> : null}

      <ImportArchive onImported={open} />

      {listings === null ? <p className="muted">{t('app.loading')}</p> : null}
      {listings?.length === 0 ? <p className="muted">{t('projects.none')}</p> : null}

      <div className="list">
        {listings?.map((listing) => (
          <div className="item" key={listing.folder}>
            <div className="top">
              <span className="name">{listing.name ?? listing.folder}</span>
              {listing.errorCode === null ? (
                <button className="btn sm" onClick={() => open(listing.folder)}>{t('projects.open')}</button>
              ) : (
                <span className="chip err">{t('projects.unreadable')}</span>
              )}
            </div>
            <div className="meta">
              <span className="mono muted">{listing.folder}/</span>
              {listing.modifiedAt !== null ? (
                <span className="small muted">{t('projects.modified', { date: date.format(new Date(listing.modifiedAt)) })}</span>
              ) : null}
            </div>
            {listing.projectId !== null && (copies.get(listing.projectId) ?? 0) > 1 ? (
              <p className="small muted">{t('projects.copy')}</p>
            ) : null}
            {listing.errorCode !== null ? <ErrorNotice code={listing.errorCode} /> : null}
          </div>
        ))}
      </div>
    </>
  );
}

function NewProject(props: { universes: Universe[]; onCreated: (folder: string) => void; onCancel: () => void }) {
  const { t } = useTranslation();
  const { configuration } = useAppData();
  const [name, setName] = useState('');
  const [universe, setUniverse] = useState<Universe>(props.universes[0] ?? 'Fantasy');
  const [geometry, setGeometry] = useState<Geometry>('TabAndSocket');
  const [paper, setPaper] = useState(configuration.paperFormats.find((format) => format.name === 'A4')?.name ?? configuration.paperFormats[0]?.name ?? '');
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      const project = await api.createProject({ name: name.trim(), universe, geometry, paperFormat: paper });
      props.onCreated(project.folder);
    } catch (failure) {
      setError(codeOf(failure));
    }
  }

  return (
    <form className="panel" onSubmit={(event) => void submit(event)}>
      <h3>{t('projects.new')}</h3>
      <div className="row">
        <div className="field">
          <label htmlFor="new-name">{t('project.name')}</label>
          <input id="new-name" required value={name} onChange={(event) => setName(event.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="new-universe">{t('project.universe')}</label>
          <select id="new-universe" value={universe} onChange={(event) => setUniverse(event.target.value as Universe)}>
            {props.universes.map((each) => <option key={each} value={each}>{t(`universes.${each}`)}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="new-paper">{t('project.paper')}</label>
          <select id="new-paper" value={paper} onChange={(event) => setPaper(event.target.value)}>
            {configuration.paperFormats.map((format) => (
              <option key={format.name} value={format.name}>{t('project.paperFormat', { name: format.name, width: format.widthMm, height: format.heightMm })}</option>
            ))}
          </select>
        </div>
      </div>
      <span className="lbl">{t('project.geometry')}</span>
      <div className="cards">
        {configuration.geometries.map((each) => (
          <button type="button" key={each} className="card" aria-pressed={geometry === each} onClick={() => setGeometry(each)}>
            <GeometryIcon geometry={each} />
            <strong>{t(`geometries.${each}.name`)}</strong>
            <span className="small muted">{t(`geometries.${each}.hint`)}</span>
          </button>
        ))}
      </div>
      <ErrorNotice code={error} />
      <div className="row">
        <button type="submit" className="btn primary" disabled={name.trim() === ''}>{t('projects.create')}</button>
        <button type="button" className="btn" onClick={props.onCancel}>{t('common.cancel')}</button>
      </div>
    </form>
  );
}

function ImportArchive(props: { onImported: (folder: string) => void }) {
  const { t } = useTranslation();
  const [file, setFile] = useState<File | null>(null);
  const [name, setName] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (file === null) {
      return;
    }
    setBusy(true);
    try {
      const project = await api.importArchive(name.trim(), file);
      props.onImported(project.folder);
    } catch (failure) {
      setError(codeOf(failure));
    } finally {
      setBusy(false);
    }
  }

  return (
    <details className="panel">
      <summary>{t('projects.import')}</summary>
      <form className="stack" onSubmit={(event) => void submit(event)}>
        <div className="row">
          <div className="field">
            <label htmlFor="import-file">{t('projects.archive')}</label>
            <input id="import-file" type="file" accept=".zip,application/zip" onChange={(event) => setFile(event.target.files?.[0] ?? null)} />
          </div>
          <div className="field">
            <label htmlFor="import-name">{t('projects.importName')}</label>
            <input id="import-name" required value={name} onChange={(event) => setName(event.target.value)} />
          </div>
          <button type="submit" className="btn" disabled={busy || file === null || name.trim() === ''}>{t('projects.importAction')}</button>
        </div>
        <ErrorNotice code={error} />
      </form>
    </details>
  );
}
