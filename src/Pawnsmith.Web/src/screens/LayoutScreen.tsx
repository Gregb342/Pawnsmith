import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { api } from '../api/client';
import type { ProjectDto, SheetReportDto } from '../api/types';
import { useAppData } from '../app/AppData';
import { useProject } from '../app/ProjectData';
import { codeOf, useSaveStatus } from '../app/SaveStatus';
import { ErrorNotice } from '../components/ErrorNotice';
import { useValueLabel } from '../components/text';
import { fieldsOf } from './BlueprintsScreen';

/**
 * The sheet's report, asked again whenever the project changes: the API
 * lays the sheet out, the front only shows it (§15.5). `modifiedAt` moves on
 * every save, so it stands for "the project changed".
 */
export function useSheetReport(project: ProjectDto): { report: SheetReportDto | null; error: string | null } {
  const [state, setState] = useState<{ report: SheetReportDto | null; error: string | null }>({ report: null, error: null });
  const { folder, modifiedAt } = project;

  useEffect(() => {
    let alive = true;
    api.sheetReport(folder).then(
      (report) => alive && setState({ report, error: null }),
      (failure: unknown) => alive && setState({ report: null, error: codeOf(failure) }),
    );
    return () => {
      alive = false;
    };
  }, [folder, modifiedAt]);

  return state;
}

/** The culture of the sheet: the interface's language when the sheet has it, else the first the API offers. */
export function useDefaultCulture(): string {
  const { i18n } = useTranslation();
  const { configuration } = useAppData();
  const language = i18n.resolvedLanguage ?? 'fr';

  return configuration.cultures.includes(language) ? language : (configuration.cultures[0] ?? 'en');
}

/**
 * The Mise en page step (§15.2): what goes on the sheet on the left, the PDF
 * itself in the centre (DEC-113), the measures of the current page on the
 * right (§15.4) — counted in cells, never as a percentage.
 */
export function LayoutScreen(props: { goTo: (step: 'project' | 'generation') => void }) {
  const { t } = useTranslation();
  const { project, reload } = useProject();
  const { catalog, configuration } = useAppData();
  const { run } = useSaveStatus();
  const valueLabel = useValueLabel(catalog);
  const culture = useDefaultCulture();
  const { report, error } = useSheetReport(project);
  const [pageNumber, setPageNumber] = useState(1);

  const nameOf = (id: string) => {
    const blueprint = project.blueprints.find((each) => each.id === id);
    return blueprint ? `${valueLabel('race', blueprint.race)} ${valueLabel('characterClass', blueprint.characterClass)}` : id;
  };
  const page = report?.pages.find((each) => each.number === pageNumber) ?? report?.pages[0] ?? null;
  const paper = configuration.paperFormats.find((format) => format.name === project.paperFormat);
  const skipped = new Set(report?.skipped.map((each) => each.blueprintId) ?? []);

  async function setQuantity(id: string, quantity: number) {
    const blueprint = project.blueprints.find((each) => each.id === id);
    if (blueprint === undefined || quantity < 1) {
      return;
    }
    // Reloaded rather than patched: the new modifiedAt is what asks for the report and the PDF again.
    if ((await run(() => api.updateBlueprint(project.folder, id, { ...fieldsOf(blueprint), quantity }))) !== undefined) {
      await reload();
    }
  }

  return (
    <>
      <div className="head">
        <div className="stack">
          <h2>{t('layout.title')}</h2>
          <p className="muted">{t('layout.lead')}</p>
        </div>
      </div>

      <ErrorNotice code={error} />

      <div className="three">
        <section className="panel">
          <h3>{t('layout.onSheet')}</h3>
          <div className="list">
            {project.blueprints.map((blueprint) => (
              <div className="item" key={blueprint.id}>
                <span className="top">
                  <span className="name">{nameOf(blueprint.id)}</span>
                  <span className="chip">{t(`sizes.${blueprint.size}`)}</span>
                </span>
                {skipped.has(blueprint.id) ? (
                  <span className="small muted">{t(`layout.skip.${report?.skipped.find((each) => each.blueprintId === blueprint.id)?.reason ?? 'NoElectedCandidate'}`)}</span>
                ) : (
                  <span className="line">
                    <span className="small muted">{t('layout.copies')}</span>
                    <span className="seg" role="group" aria-label={t('blueprints.quantity')}>
                      <button aria-label={t('layout.less')} disabled={blueprint.quantity <= 1} onClick={() => void setQuantity(blueprint.id, blueprint.quantity - 1)}>−</button>
                      <button aria-pressed="false" tabIndex={-1}>{blueprint.quantity}</button>
                      <button aria-label={t('layout.more')} onClick={() => void setQuantity(blueprint.id, blueprint.quantity + 1)}>+</button>
                    </span>
                  </span>
                )}
              </div>
            ))}
          </div>
          {project.blueprints.length === 0 ? <p className="muted">{t('generation.noBlueprint')}</p> : null}
        </section>

        <section className="center stack">
          {report !== null && report.pages.length > 0 ? (
            <>
              <div className="tabs" role="group" aria-label={t('layout.pages')}>
                {report.pages.map((each) => (
                  <button key={each.number} aria-pressed={each.number === page?.number} onClick={() => setPageNumber(each.number)}>
                    {t('layout.page', { number: each.number, size: t(`sizes.${each.size}`) })}
                  </button>
                ))}
              </div>
              {/* The PDF itself (DEC-113): what it shows is what prints, marks included (§15.2). */}
              <iframe
                key={`${project.modifiedAt}-${page?.number ?? 1}`}
                className="sheet"
                title={t('layout.preview')}
                src={`${api.sheetUrl(project.folder, culture, true)}#page=${page?.number ?? 1}`}
              />
            </>
          ) : report !== null ? (
            <div className="panel">
              <p>{t('layout.empty')}</p>
              <div>
                <button className="btn" onClick={() => props.goTo('generation')}>{t('layout.toGeneration')}</button>
              </div>
            </div>
          ) : null}
        </section>

        <section className="panel">
          <h3>{t('layout.measures')}</h3>
          <dl className="kv">
            <dt>{t('project.paper')}</dt>
            <dd>{paper ? t('project.paperFormat', { name: paper.name, width: paper.widthMm, height: paper.heightMm }) : project.paperFormat}</dd>
            <dt>{t('project.geometry')}</dt>
            <dd>{t(`geometries.${project.geometry}.name`)}</dd>
            <dt>{t('layout.pageCount')}</dt>
            <dd>{report?.pages.length ?? '—'}</dd>
          </dl>
          {page !== null ? (
            <div className="stack">
              <span className="eyebrow">{t('layout.page', { number: page.number, size: t(`sizes.${page.size}`) })}</span>
              <span className="big">{t('layout.cellsUsed', { used: page.used, capacity: page.capacity })}</span>
              <span className="small muted">{t('layout.cellsLeft', { count: page.capacity - page.used })}</span>
              {/* One mark per cell: what is counted is cells, not area (§15.4). */}
              <span className="cells" aria-hidden="true">
                {Array.from({ length: page.capacity }, (_, index) => <i key={index} className={index < page.used ? 'used' : ''} />)}
              </span>
            </div>
          ) : null}
          <div>
            <button className="btn sm" onClick={() => props.goTo('project')}>{t('layout.changeFormat')}</button>
          </div>
        </section>
      </div>

      {report !== null ? <Findings report={report} nameOf={nameOf} /> : null}
    </>
  );
}

/** What the user should know before printing: kept proposals whose prompt changed, images held back by their width. */
export function Findings(props: { report: SheetReportDto; nameOf: (blueprintId: string) => string }) {
  const { t } = useTranslation();
  const { report } = props;

  if (report.misalignedElections.length === 0 && report.widthLimited.length === 0 && report.misalignmentKnown) {
    return null;
  }

  return (
    <section className="panel">
      <h3>{t('layout.findings')}</h3>
      {!report.misalignmentKnown ? <div className="notice info">{t('generation.misalignmentUnknown')}</div> : null}
      {report.misalignedElections.map((each) => (
        <div className="notice warn" key={each.candidateId}>
          {t('layout.misaligned', { name: props.nameOf(each.blueprintId), clauses: each.clauses.map((clause) => t(`clauses.${clause}`)).join(', ') })}
        </div>
      ))}
      {report.widthLimited.map((each) => (
        <div className="notice info" key={`${each.name}-${each.size}`}>
          {t('layout.widthLimited', {
            name: each.name,
            size: t(`sizes.${each.size}`),
            printed: Math.round(each.printedHeightMm),
            available: Math.round(each.availableHeightMm),
            usage: Math.round(each.heightUsage * 100),
          })}
        </div>
      ))}
    </section>
  );
}
