import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { api, withBlueprint } from '../api/client';
import type { BlueprintDto, CandidateDto, CandidateStatus, JobDto } from '../api/types';
import { useAppData } from '../app/AppData';
import { useProject } from '../app/ProjectData';
import { codeOf, useSaveStatus } from '../app/SaveStatus';
import { ErrorNotice } from '../components/ErrorNotice';
import { useErrorText, useValueLabel } from '../components/text';
import { BlueprintItem } from './BlueprintsScreen';

/** How often the queue is asked for news while this screen is open. Local server, small answer. */
const POLL_MS = 1500;

const statuses: CandidateStatus[] = ['Draft', 'Valid', 'Rejected'];

/** A seed is a decimal string (§G.4): JavaScript numbers would round it past 2^53. */
const SEED = /^[0-9]+$/;

/**
 * The Génération step: launch a batch, follow the queue, sort the proposals.
 *
 * Every image comes from the project's own routes (DEC-088). A proposal whose
 * prompt changed since it was made is bordered and hatched, and says which
 * clauses moved (DEC-105); its detail shows the text it was made with next to
 * today's, for the subject and the style — never the framing clause, which
 * the interface does not show (DEC-029).
 */
export function GenerationScreen() {
  const { t } = useTranslation();
  const { project, reload } = useProject();
  const { catalog } = useAppData();
  const valueLabel = useValueLabel(catalog);
  const [selectedId, setSelectedId] = useState<string | null>(project.blueprints[0]?.id ?? null);
  const [jobs, setJobs] = useState<JobDto[]>([]);
  const [jobsError, setJobsError] = useState<string | null>(null);
  const signature = useRef('');

  const nameOf = (blueprint: BlueprintDto) => `${valueLabel('race', blueprint.race)} ${valueLabel('characterClass', blueprint.characterClass)}`;
  const selected = project.blueprints.find((blueprint) => blueprint.id === selectedId) ?? project.blueprints[0] ?? null;
  const folder = project.folder;

  // The queue is polled — the API answers the newest job first; every change of a job of this project — a new
  // candidate, a state, a cut-out failure — reloads the project, which holds
  // the candidates themselves.
  useEffect(() => {
    let alive = true;
    const poll = () => {
      api.jobs().then(
        (all) => {
          if (!alive) {
            return;
          }
          const mine = all.filter((job) => job.folder === folder);
          const next = mine.map((job) => `${job.id}:${job.state}:${job.produced.length}:${job.cutoutFailures.length}`).join('|');
          if (signature.current !== '' && next !== signature.current) {
            void reload();
          }
          signature.current = next;
          setJobs(mine);
          setJobsError(null);
        },
        (failure: unknown) => alive && setJobsError(codeOf(failure)),
      );
    };
    poll();
    const timer = window.setInterval(poll, POLL_MS);
    return () => {
      alive = false;
      window.clearInterval(timer);
    };
  }, [folder, reload]);

  return (
    <>
      <div className="head">
        <div className="stack">
          <h2>{t('generation.title')}</h2>
          <p className="muted">{t('generation.lead')}</p>
        </div>
      </div>

      {project.blueprints.length === 0 ? <p className="muted">{t('generation.noBlueprint')}</p> : null}

      <div className="split">
        <nav className="list" aria-label={t('blueprints.title')}>
          {project.blueprints.map((blueprint) => (
            <BlueprintItem
              key={blueprint.id}
              blueprint={blueprint}
              current={blueprint.id === selected?.id}
              name={nameOf(blueprint)}
              onSelect={() => setSelectedId(blueprint.id)}
            />
          ))}
        </nav>

        {selected !== null ? (
          <div className="stack">
            <LaunchBatch key={selected.id} blueprint={selected} onStarted={(job) => setJobs((current) => [job, ...current])} />
            <Queue jobs={jobs} error={jobsError} nameOf={(id) => {
              const blueprint = project.blueprints.find((each) => each.id === id);
              return blueprint ? nameOf(blueprint) : id;
            }} />
            <Gallery key={selected.id} blueprint={selected} />
          </div>
        ) : null}
      </div>
    </>
  );
}

function LaunchBatch(props: { blueprint: BlueprintDto; onStarted: (job: JobDto) => void }) {
  const { t } = useTranslation();
  const { project } = useProject();
  const { generator } = useAppData();
  const [count, setCount] = useState('4');
  const [seeds, setSeeds] = useState('');
  const [error, setError] = useState<string | null>(null);

  const seedList = seeds.split(/[\s,;]+/).filter((seed) => seed !== '');
  const seedsValid = seedList.every((seed) => SEED.test(seed));
  const countValue = Number.parseInt(count, 10);
  // An empty count would travel as null; the API would refuse it, but the button can say so first.
  const launchable = seedsValid && (seedList.length > 0 || countValue >= 1);

  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      const body = seedList.length > 0 ? { seeds: seedList } : { count: countValue };
      props.onStarted(await api.startJob(project.folder, props.blueprint.id, body));
      setError(null);
    } catch (failure) {
      setError(codeOf(failure));
    }
  }

  return (
    <form className="panel" onSubmit={(event) => void submit(event)}>
      <h3>{t('generation.launch')}</h3>
      {generator !== null && generator.state !== 'Available' ? (
        <div className="notice warn">
          <span>{t(`generator.states.${generator.state}`)}</span>
          <a href="#/generator">{t('generation.toGenerator')}</a>
        </div>
      ) : null}
      <div className="row">
        <div className="field">
          <label htmlFor="batch-count">{t('generation.count')}</label>
          <input id="batch-count" type="number" min={1} max={20} value={count} disabled={seedList.length > 0} onChange={(event) => setCount(event.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="batch-seeds">{t('generation.seeds')}</label>
          <input id="batch-seeds" className="mono" value={seeds} onChange={(event) => setSeeds(event.target.value)} />
          <span className="hint">{seedsValid ? t('generation.seedsHint') : t('generation.seedsInvalid')}</span>
        </div>
      </div>
      <ErrorNotice code={error} />
      <div>
        <button type="submit" className="btn primary" disabled={!launchable}>
          {seedList.length > 0 ? t('generation.launchSeeds', { count: seedList.length }) : t('generation.launchCount', { count: countValue || 0 })}
        </button>
      </div>
    </form>
  );
}

function Queue(props: { jobs: JobDto[]; error: string | null; nameOf: (blueprintId: string) => string }) {
  const { t } = useTranslation();
  const errorText = useErrorText();
  const [error, setError] = useState<string | null>(null);

  if (props.jobs.length === 0 && props.error === null) {
    return null;
  }

  async function cancel(id: string) {
    try {
      await api.cancelJob(id);
      setError(null);
    } catch (failure) {
      setError(codeOf(failure));
    }
  }


  return (
    <section className="panel">
      <h3>{t('generation.queue')}</h3>
      <ErrorNotice code={props.error ?? error} />
      <div className="jobs">
        {props.jobs.map((job) => (
          <div className="job" key={job.id}>
            <div className="stack">
              <span className="line">
                <strong>{props.nameOf(job.blueprintId)}</strong>
                <span className={`chip ${job.state === 'Completed' ? 'ok' : job.state === 'Failed' ? 'err' : job.state === 'Cancelled' ? '' : 'warn'}`}>
                  {t(`generation.states.${job.state}`)}
                </span>
              </span>
              <span className="small muted">{t('generation.produced', { produced: job.produced.length, requested: job.requested })}</span>
              {job.failureCode !== null ? <span className="small">{errorText(job.failureCode)} <span className="mono muted">{job.failureCode}</span></span> : null}
              {job.cutoutFailures.map((failure) => (
                <span className="small" key={failure.candidateId}>{t('generation.cutoutFailed', { reason: errorText(failure.code) })}</span>
              ))}
            </div>
            {job.state === 'Queued' || job.state === 'Running' ? (
              <button className="btn sm" onClick={() => void cancel(job.id)}>{t('common.cancel')}</button>
            ) : null}
          </div>
        ))}
      </div>
    </section>
  );
}

function Gallery(props: { blueprint: BlueprintDto }) {
  const { t } = useTranslation();
  const { project } = useProject();
  const { blueprint } = props;
  const [selectedId, setSelectedId] = useState<string | null>(null);
  // A cut-out is rewritten under the same name; the browser would show the old one.
  const [versions, setVersions] = useState<Record<string, number>>({});

  // The newest first, by instant: never a culture-aware string comparison.
  const candidates = [...blueprint.candidates].sort((a, b) => Date.parse(b.generatedAt) - Date.parse(a.generatedAt));
  const selected = candidates.find((candidate) => candidate.id === selectedId) ?? null;

  return (
    <section className="panel">
      <div className="line">
        <h3>{t('generation.proposals')}</h3>
        <span className="small muted">{t('blueprints.proposals', { count: candidates.length })}</span>
      </div>
      {!project.misalignmentKnown ? <div className="notice info">{t('generation.misalignmentUnknown')}</div> : null}
      {candidates.length === 0 ? <p className="muted">{t('generation.noProposal')}</p> : null}
      <div className="gallery">
        {candidates.map((candidate) => (
          <Proposal
            key={candidate.id}
            blueprint={blueprint}
            candidate={candidate}
            version={versions[candidate.id]}
            current={candidate.id === selectedId}
            onSelect={() => setSelectedId(candidate.id === selectedId ? null : candidate.id)}
          />
        ))}
      </div>
      {selected !== null ? (
        <Detail
          key={selected.id}
          blueprint={blueprint}
          candidate={selected}
          version={versions[selected.id]}
          onCutOut={() => setVersions((current) => ({ ...current, [selected.id]: Date.now() }))}
        />
      ) : null}
    </section>
  );
}

/** The two cut-outs side by side, or the paired image while there are none. Drawn inside a `.picture`. */
function PictureImages(props: { candidate: CandidateDto; version: number | undefined }) {
  const { t } = useTranslation();
  const { project } = useProject();
  const { candidate } = props;
  const url = (stored: string) => api.imageUrl(project.folder, stored) + (props.version !== undefined ? `?v=${props.version}` : '');

  if (candidate.frontImage !== null && candidate.backImage !== null) {
    return (
      <>
        <img src={url(candidate.frontImage)} alt={t('generation.front')} loading="lazy" />
        <img src={url(candidate.backImage)} alt={t('generation.back')} loading="lazy" />
      </>
    );
  }

  return candidate.pairedImage !== null ? (
    <img src={url(candidate.pairedImage)} alt={t('generation.paired')} loading="lazy" />
  ) : (
    <span className="muted small">{t('generation.noImage')}</span>
  );
}

function Proposal(props: { blueprint: BlueprintDto; candidate: CandidateDto; version: number | undefined; current: boolean; onSelect: () => void }) {
  const { t } = useTranslation();
  const { candidate } = props;
  const changed = (candidate.misalignedClauses ?? []).length > 0;
  const elected = props.blueprint.electedCandidateId === candidate.id;

  return (
    <article className={`proposal${changed ? ' changed' : ''}${candidate.status === 'Rejected' ? ' rejected' : ''}`} aria-current={props.current}>
      <button
        className={`picture ${candidate.frontImage !== null && candidate.backImage !== null ? 'checker' : 'single'}`}
        onClick={props.onSelect}
        aria-label={t('generation.open', { seed: candidate.seed })}
      >
        <PictureImages candidate={candidate} version={props.version} />
      </button>
      <div className="body">
        <span className="line">
          <span className="mono small muted">#{candidate.seed}</span>
          {elected ? <span className="chip ok">★ {t('blueprints.kept')}</span> : null}
        </span>
        <span className="line">
          <span className="chip">{t(`generation.statuses.${candidate.status}`)}</span>
          {candidate.frontImage === null ? <span className="chip warn">{t('generation.notCut')}</span> : null}
        </span>
        {changed ? (
          <span className="small">
            {t('generation.changedSince')} {(candidate.misalignedClauses ?? []).map((clause) => t(`clauses.${clause}`)).join(', ')}
          </span>
        ) : null}
      </div>
    </article>
  );
}

function Detail(props: { blueprint: BlueprintDto; candidate: CandidateDto; version: number | undefined; onCutOut: () => void }) {
  const { t } = useTranslation();
  const { project, setProject } = useProject();
  const { run } = useSaveStatus();
  const { blueprint, candidate } = props;
  const [cutting, setCutting] = useState(false);
  const elected = blueprint.electedCandidateId === candidate.id;
  const cut = candidate.frontImage !== null && candidate.backImage !== null;
  const clauses = candidate.misalignedClauses ?? [];

  async function write(action: () => ReturnType<typeof api.elect>) {
    const edited = await run(action);
    if (edited !== undefined) {
      setProject(withBlueprint(project, edited.blueprint));
    }
    return edited;
  }

  async function cutOut() {
    setCutting(true);
    const edited = await write(() => api.cutOut(project.folder, blueprint.id, candidate.id));
    setCutting(false);
    if (edited !== undefined) {
      props.onCutOut();
    }
  }

  const url = (stored: string) => api.imageUrl(project.folder, stored) + (props.version !== undefined ? `?v=${props.version}` : '');

  return (
    <div className="group">
      <span className="eyebrow">{t('generation.detail', { seed: candidate.seed })}</span>
      <div className="views">
        {cut ? (
          <>
            <figure>
              <span className="pic single checker"><img src={url(candidate.frontImage ?? '')} alt={t('generation.front')} /></span>
              <figcaption>{t('generation.front')}</figcaption>
            </figure>
            <figure>
              <span className="pic single checker"><img src={url(candidate.backImage ?? '')} alt={t('generation.back')} /></span>
              <figcaption>{t('generation.back')}</figcaption>
            </figure>
          </>
        ) : (
          <figure>
            <span className="pic single">{candidate.pairedImage !== null ? <img src={url(candidate.pairedImage)} alt={t('generation.paired')} /> : null}</span>
            <figcaption>{t('generation.paired')}</figcaption>
          </figure>
        )}
      </div>

      <div className="line">
        <span className="seg" role="group" aria-label={t('generation.status')}>
          {statuses.map((status) => (
            <button
              key={status}
              aria-pressed={candidate.status === status}
              onClick={() => void write(() => api.setStatus(project.folder, blueprint.id, candidate.id, status))}
            >
              {t(`generation.statuses.${status}`)}
            </button>
          ))}
        </span>
        <span className="row">
          {candidate.pairedImage !== null ? (
            <button className="btn sm" disabled={cutting} onClick={() => void cutOut()}>
              {cutting ? t('generation.cutting') : cut ? t('generation.cutAgain') : t('generation.cut')}
            </button>
          ) : null}
          {elected ? (
            <button className="btn sm" onClick={() => void write(() => api.elect(project.folder, blueprint.id, null))}>{t('generation.unkeep')}</button>
          ) : (
            <button className="btn sm primary" disabled={!cut} onClick={() => void write(() => api.elect(project.folder, blueprint.id, candidate.id))}>
              {t('generation.keep')}
            </button>
          )}
        </span>
      </div>
      {!cut ? <span className="hint">{t('generation.keepNeedsCut')}</span> : null}

      {clauses.length > 0 ? (
        <div className="stack">
          <span className="eyebrow">{t('generation.whatChanged')}</span>
          {clauses.includes('Subject') ? <Change title={t('clauses.Subject')} then={candidate.subjectClauseUsed} now={blueprint.subjectClause} /> : null}
          {clauses.includes('Style') ? <Change title={t('clauses.Style')} then={candidate.styleClauseUsed} now={project.style.styleClause} /> : null}
          {clauses.includes('Framing') ? <p className="small">{t('generation.framingChanged')}</p> : null}
        </div>
      ) : null}
    </div>
  );
}

function Change(props: { title: string; then: string; now: string }) {
  const { t } = useTranslation();

  return (
    <div className="grid-2">
      <div className="stack">
        <span className="small muted">{t('generation.then', { clause: props.title })}</span>
        <div className="prompt">{props.then}</div>
      </div>
      <div className="stack">
        <span className="small muted">{t('generation.now', { clause: props.title })}</span>
        <div className="prompt">{props.now}</div>
      </div>
    </div>
  );
}
