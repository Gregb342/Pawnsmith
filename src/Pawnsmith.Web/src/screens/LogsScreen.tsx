import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { api } from '../api/client';
import type { LogListDto, LogTailDto } from '../api/types';
import { codeOf } from '../app/SaveStatus';
import { ErrorNotice } from '../components/ErrorNotice';

const LINE_COUNTS = [100, 500, 2000];
const SERIOUS = new Set(['Warning', 'Error', 'Fatal']);

/** One line of a log file, as Serilog's JSON formatter writes it (DEC-091). */
interface LogEvent {
  Timestamp?: string;
  Level?: string;
  RenderedMessage?: string;
  Exception?: string;
  Properties?: { SourceContext?: string; JobId?: string };
}

/** A line read as JSON, or null when it is not: a line is shown either way, never dropped. */
function parse(line: string): LogEvent | null {
  try {
    const value: unknown = JSON.parse(line);
    return typeof value === 'object' && value !== null ? value : null;
  } catch {
    return null;
  }
}

/**
 * The Journaux page (DEC-094): the files, and the last lines of one.
 *
 * Lines come as strings and are shown as text — React escapes them — so a
 * message carrying markup or a forged line break stays what it is (MEN-011).
 */
export function LogsScreen() {
  const { t, i18n } = useTranslation();
  const [list, setList] = useState<LogListDto | null>(null);
  const [name, setName] = useState<string | null>(null);
  const [count, setCount] = useState(LINE_COUNTS[1] ?? 500);
  const [seriousOnly, setSeriousOnly] = useState(false);
  const [tail, setTail] = useState<LogTailDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.logs().then(
      (logs) => {
        setList(logs);
        setName((current) => current ?? logs.files[0]?.name ?? null);
      },
      (failure: unknown) => setError(codeOf(failure)),
    );
  }, []);

  useEffect(() => {
    if (name === null) {
      return;
    }
    api.logTail(name, count).then(setTail, (failure: unknown) => setError(codeOf(failure)));
  }, [name, count]);

  const time = new Intl.DateTimeFormat(i18n.resolvedLanguage, { dateStyle: 'short', timeStyle: 'medium' });
  const events = (tail?.lines ?? [])
    .map((line) => ({ line, event: parse(line) }))
    .filter((each) => !seriousOnly || SERIOUS.has(each.event?.Level ?? ''))
    .reverse();

  return (
    <>
      <div className="head">
        <div className="stack">
          <h2>{t('logs.title')}</h2>
          <p className="muted">{t('logs.lead')}</p>
        </div>
      </div>

      <ErrorNotice code={error} />
      {list !== null && !list.enabled ? <div className="notice info">{t('logs.disabled')}</div> : null}

      <div className="row">
        <div className="field">
          <label htmlFor="log-file">{t('logs.file')}</label>
          <select id="log-file" value={name ?? ''} onChange={(event) => setName(event.target.value)}>
            {list?.files.map((file) => (
              <option key={file.name} value={file.name}>{t('logs.fileOption', { name: file.name, size: Math.ceil(file.sizeBytes / 1024) })}</option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor="log-lines">{t('logs.lines')}</label>
          <select id="log-lines" value={count} onChange={(event) => setCount(Number.parseInt(event.target.value, 10))}>
            {LINE_COUNTS.map((each) => <option key={each} value={each}>{each}</option>)}
          </select>
        </div>
        <label className="row small">
          <input type="checkbox" checked={seriousOnly} onChange={(event) => setSeriousOnly(event.target.checked)} />
          {t('logs.seriousOnly')}
        </label>
      </div>

      {list?.files.length === 0 ? <p className="muted">{t('logs.none')}</p> : null}
      {tail?.truncated ? <p className="small muted">{t('logs.truncated')}</p> : null}

      <div className="table-wrap panel">
        <table>
          <thead>
            <tr>
              <th>{t('logs.time')}</th>
              <th>{t('logs.level')}</th>
              <th>{t('logs.message')}</th>
            </tr>
          </thead>
          <tbody>
            {events.map(({ line, event }, index) =>
              event === null ? (
                <tr key={index}>
                  <td colSpan={3} className="mono small">{line}</td>
                </tr>
              ) : (
                <tr key={index}>
                  <td className="small">{event.Timestamp !== undefined ? time.format(new Date(event.Timestamp)) : ''}</td>
                  <td>
                    <span className={`chip ${event.Level === 'Error' || event.Level === 'Fatal' ? 'err' : event.Level === 'Warning' ? 'warn' : ''}`}>{event.Level ?? '?'}</span>
                  </td>
                  <td>
                    <div>{event.RenderedMessage ?? ''}</div>
                    <div className="mono small muted">
                      {event.Properties?.SourceContext ?? ''}
                      {event.Properties?.JobId !== undefined ? ` · job ${event.Properties.JobId}` : ''}
                    </div>
                    {event.Exception !== undefined ? <pre className="mono small">{event.Exception}</pre> : null}
                  </td>
                </tr>
              ),
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
