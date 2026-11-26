import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { api } from '../api/client';
import { useAppData } from '../app/AppData';
import { useProject } from '../app/ProjectData';
import { ErrorNotice } from '../components/ErrorNotice';
import { useValueLabel } from '../components/text';
import { Findings, useDefaultCulture, useSheetReport } from './LayoutScreen';

/**
 * The Impression step: the language of the sheet's labels, the checks before
 * cutting, and the download.
 *
 * The culture is a choice made here, at print time (§15.1), and nowhere else:
 * the server never reads it from the browser (§G.10). Nothing scales the
 * sheet (§15.5): the calibration line is the judge (§B.5.5).
 */
export function PrintScreen() {
  const { t } = useTranslation();
  const { project } = useProject();
  const { catalog, configuration } = useAppData();
  const valueLabel = useValueLabel(catalog);
  const { report, error } = useSheetReport(project);
  const [culture, setCulture] = useState(useDefaultCulture());

  const nameOf = (id: string) => {
    const blueprint = project.blueprints.find((each) => each.id === id);
    return blueprint ? `${valueLabel('race', blueprint.race)} ${valueLabel('characterClass', blueprint.characterClass)}` : id;
  };
  const printable = report !== null && report.pages.length > 0;
  const pawns = report?.pages.reduce((total, page) => total + page.used, 0) ?? 0;

  return (
    <>
      <div className="head">
        <div className="stack">
          <h2>{t('print.title')}</h2>
          <p className="muted">{t('print.lead')}</p>
        </div>
      </div>

      <ErrorNotice code={error} />

      <div className="grid-2">
        <section className="panel">
          <h3>{t('print.sheet')}</h3>
          <div className="field">
            <label htmlFor="sheet-culture">{t('print.culture')}</label>
            <select id="sheet-culture" value={culture} onChange={(event) => setCulture(event.target.value)}>
              {configuration.cultures.map((each) => (
                <option key={each} value={each}>{t(`language.names.${each}`, { defaultValue: each })}</option>
              ))}
            </select>
            <span className="hint">{t('print.cultureHint')}</span>
          </div>
          {report !== null ? <p>{t('print.summary', { pages: report.pages.length, pawns })}</p> : null}
          {printable ? (
            <div>
              <a className="btn primary" href={api.sheetUrl(project.folder, culture, false)} download>{t('print.download')}</a>
            </div>
          ) : report !== null ? (
            <div className="notice info">{t('layout.empty')}</div>
          ) : null}
        </section>

        <section className="panel">
          <h3>{t('print.checks')}</h3>
          <ul className="check">
            <li>{t('print.check.actualSize')}</li>
            <li>{t('print.check.paper', { paper: project.paperFormat })}</li>
            <li>{t('print.check.ruler')}</li>
            <li>{t('print.check.correction')}</li>
            <li>{t('print.check.cut')}</li>
          </ul>
        </section>
      </div>

      {report !== null ? <Findings report={report} nameOf={nameOf} /> : null}
    </>
  );
}
