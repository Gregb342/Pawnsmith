import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { api, withBlueprint } from '../api/client';
import type { BlueprintFields } from '../api/client';
import type { BlueprintDto, CatalogParameterDto, CompositionDiagnosticDto, Size } from '../api/types';
import { useAppData } from '../app/AppData';
import { useProject } from '../app/ProjectData';
import { useSaveStatus } from '../app/SaveStatus';
import { TextField } from '../components/TextField';
import { useLabel, useValueLabel } from '../components/text';
import { NewEntryDialog } from './NewEntryDialog';

/** The two keys of the catalogue that hold the required fields (DEC-106). */
const RACE = 'race';
const CHARACTER_CLASS = 'characterClass';

/** The option of a list that opens "New item" rather than choosing a value. */
const OTHER = '\u0000other';

function fieldsOf(blueprint: BlueprintDto): BlueprintFields {
  return {
    race: blueprint.race,
    characterClass: blueprint.characterClass,
    size: blueprint.size,
    optionalParameters: blueprint.optionalParameters,
    details: blueprint.details,
    quantity: blueprint.quantity,
  };
}

/** The blueprint's value for an optional key, or '' for "not constrained". */
function optionalValue(blueprint: BlueprintDto, key: string): string {
  return blueprint.optionalParameters.find((parameter) => parameter.key === key)?.value ?? '';
}

/**
 * The Gabarits step: the list, and the editor of the selected blueprint.
 *
 * Every list comes from the catalogue, labelled in the interface's language
 * (DEC-106); "Other…" creates a complete item (DEC-107). The subject clause
 * is shown locked while it follows the fields; "Customise the text" unlocks
 * it, and greys out what no longer changes it; "Back to the automatic text"
 * recomposes it (DEC-109).
 */
export function BlueprintsScreen() {
  const { t } = useTranslation();
  const { project, setProject, reload } = useProject();
  const { catalog } = useAppData();
  const { run } = useSaveStatus();
  const valueLabel = useValueLabel(catalog);
  const [selectedId, setSelectedId] = useState<string | null>(project.blueprints[0]?.id ?? null);

  const selected = project.blueprints.find((blueprint) => blueprint.id === selectedId) ?? project.blueprints[0] ?? null;

  async function add() {
    const first = (key: string) => catalog.parameters.find((parameter) => parameter.key === key)?.entries[0]?.value ?? '';
    const edited = await run(() =>
      api.addBlueprint(project.folder, {
        race: first(RACE),
        characterClass: first(CHARACTER_CLASS),
        size: 'Medium',
        optionalParameters: [],
        details: '',
        quantity: 1,
      }),
    );
    if (edited !== undefined) {
      await reload();
      setSelectedId(edited.blueprint.id);
    }
  }

  return (
    <>
      <div className="head">
        <div className="stack">
          <h2>{t('blueprints.title')}</h2>
          <p className="muted">{t('blueprints.lead')}</p>
        </div>
        <button className="btn primary" onClick={() => void add()}>{t('blueprints.new')}</button>
      </div>

      <div className="split">
        <nav className="list" aria-label={t('blueprints.title')}>
          {project.blueprints.length === 0 ? <p className="muted">{t('blueprints.none')}</p> : null}
          {project.blueprints.map((blueprint) => (
            <BlueprintItem
              key={blueprint.id}
              blueprint={blueprint}
              current={blueprint.id === selected?.id}
              name={`${valueLabel(RACE, blueprint.race)} ${valueLabel(CHARACTER_CLASS, blueprint.characterClass)}`}
              onSelect={() => setSelectedId(blueprint.id)}
            />
          ))}
        </nav>

        {selected !== null ? (
          <Editor
            key={selected.id}
            blueprint={selected}
            onChanged={(blueprint) => setProject(withBlueprint(project, blueprint))}
            onRemoved={() => {
              setSelectedId(null);
              void reload();
            }}
          />
        ) : null}
      </div>
    </>
  );
}

export function BlueprintItem(props: { blueprint: BlueprintDto; current: boolean; name: string; onSelect: () => void }) {
  const { t } = useTranslation();
  const { blueprint } = props;

  return (
    <button className="item" aria-current={props.current} onClick={props.onSelect}>
      <span className="top">
        <span className="name">{props.name}</span>
        <span className="small muted">×{blueprint.quantity}</span>
      </span>
      <span className="meta">
        <span className="chip">{t(`sizes.${blueprint.size}`)}</span>
        {blueprint.electedCandidateId !== null ? <span className="chip ok">★ {t('blueprints.kept')}</span> : <span className="chip">{t('blueprints.noneKept')}</span>}
        <span className="small muted">{t('blueprints.proposals', { count: blueprint.candidates.length })}</span>
      </span>
    </button>
  );
}

/** A list of the catalogue as a select, with "Other…" at the end. */
function ListField(props: {
  parameter: CatalogParameterDto;
  value: string;
  optional: boolean;
  disabled: boolean;
  onChange: (value: string) => void;
  onOther: () => void;
}) {
  const { t } = useTranslation();
  const label = useLabel();
  const known = props.parameter.entries.some((entry) => entry.value === props.value);

  return (
    <div className="field">
      <label htmlFor={`list-${props.parameter.key}`}>{label(props.parameter.labels, props.parameter.key)}</label>
      <select
        id={`list-${props.parameter.key}`}
        value={props.value}
        disabled={props.disabled}
        onChange={(event) => (event.target.value === OTHER ? props.onOther() : props.onChange(event.target.value))}
      >
        {props.optional ? <option value="">{t('blueprints.generatorChoice')}</option> : null}
        {props.parameter.entries.map((entry) => (
          <option key={entry.value} value={entry.value}>{label(entry.labels, entry.value)}</option>
        ))}
        {/* A value from an older or imported project, which the catalogue no longer has (DEC-056). */}
        {props.value !== '' && !known ? <option value={props.value}>{t('blueprints.outOfList', { value: props.value })}</option> : null}
        <option value={OTHER}>{t('blueprints.other')}</option>
      </select>
    </div>
  );
}

function Editor(props: { blueprint: BlueprintDto; onChanged: (blueprint: BlueprintDto) => void; onRemoved: () => void }) {
  const { t } = useTranslation();
  const { project } = useProject();
  const { catalog, configuration } = useAppData();
  const { run } = useSaveStatus();
  const { blueprint } = props;
  const [diagnostics, setDiagnostics] = useState<CompositionDiagnosticDto[]>([]);
  const [unlocked, setUnlocked] = useState(false);
  const [confirmReset, setConfirmReset] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [adding, setAdding] = useState<{ parameter: CatalogParameterDto; apply: (value: string) => void } | null>(null);

  // Edited by hand: the options no longer change the text, so they are greyed out (DEC-109).
  const editing = blueprint.subjectClauseEdited || unlocked;

  async function apply(write: () => Promise<{ blueprint: BlueprintDto; compositionDiagnostics: CompositionDiagnosticDto[] }>) {
    const edited = await run(write);
    if (edited !== undefined) {
      props.onChanged(edited.blueprint);
      setDiagnostics(edited.compositionDiagnostics);
    }
  }

  const saveFields = (change: Partial<BlueprintFields>) =>
    void apply(() => api.updateBlueprint(project.folder, blueprint.id, { ...fieldsOf(blueprint), ...change }));

  function setOptional(key: string, value: string) {
    const others = blueprint.optionalParameters.filter((parameter) => parameter.key !== key);
    saveFields({ optionalParameters: value === '' ? others : [...others, { key, value }] });
  }

  const race = catalog.parameters.find((parameter) => parameter.key === RACE);
  const characterClass = catalog.parameters.find((parameter) => parameter.key === CHARACTER_CLASS);
  const optionals = catalog.parameters.filter((parameter) => parameter.key !== RACE && parameter.key !== CHARACTER_CLASS);
  const outOfList = blueprint.optionalParameters.filter(
    (parameter) => !catalog.parameters.find((each) => each.key === parameter.key)?.entries.some((entry) => entry.value === parameter.value),
  );

  return (
    <section className="panel">
      <div className="group" style={{ borderTop: 0, paddingTop: 0 }}>
        <span className="eyebrow">{t('blueprints.required')}</span>
        <div className="row">
          {race ? <ListField parameter={race} value={blueprint.race} optional={false} disabled={editing} onChange={(value) => saveFields({ race: value })} onOther={() => setAdding({ parameter: race, apply: (value) => saveFields({ race: value }) })} /> : null}
          {characterClass ? (
            <ListField parameter={characterClass} value={blueprint.characterClass} optional={false} disabled={editing} onChange={(value) => saveFields({ characterClass: value })} onOther={() => setAdding({ parameter: characterClass, apply: (value) => saveFields({ characterClass: value }) })} />
          ) : null}
          <div className="field">
            <label htmlFor="size">{t('blueprints.size')}</label>
            <select id="size" value={blueprint.size} onChange={(event) => saveFields({ size: event.target.value as Size })}>
              {configuration.sizes.map((size) => <option key={size.size} value={size.size}>{t(`sizes.${size.size}`)}</option>)}
            </select>
          </div>
          <TextField
            key={`qty-${blueprint.quantity}`}
            label={t('blueprints.quantity')}
            type="number"
            initial={blueprint.quantity.toString()}
            onCommit={(text) => saveFields({ quantity: Math.max(1, Number.parseInt(text, 10) || 1) })}
          />
        </div>
      </div>

      <div className="group">
        <span className="eyebrow">{t('blueprints.optional')}</span>
        <span className="hint">{t('blueprints.optionalHint')}</span>
        <div className="row">
          {optionals.map((parameter) => (
            <ListField
              key={parameter.key}
              parameter={parameter}
              value={optionalValue(blueprint, parameter.key)}
              optional
              disabled={editing}
              onChange={(value) => setOptional(parameter.key, value)}
              onOther={() => setAdding({ parameter, apply: (value) => setOptional(parameter.key, value) })}
            />
          ))}
        </div>
        {outOfList.map((parameter) => (
          <div key={parameter.key} className="notice warn">{t('blueprints.outOfListWarning', { value: parameter.value })}</div>
        ))}
        {diagnostics
          .filter((diagnostic) => !outOfList.some((parameter) => parameter.key === diagnostic.key))
          .map((diagnostic) => (
            <div key={`${diagnostic.key}-${diagnostic.value}`} className="notice warn">{t('blueprints.outOfListWarning', { value: diagnostic.value })}</div>
          ))}
      </div>

      <div className="group">
        <span className="eyebrow">{t('blueprints.free')}</span>
        <TextField
          key={`details-${blueprint.details}`}
          label={t('blueprints.details')}
          hint={t('blueprints.detailsHint')}
          initial={blueprint.details}
          disabled={editing}
          onCommit={(details) => saveFields({ details })}
        />

        <div className="field">
          <span className="lbl">{t('blueprints.subject')}</span>
          {editing ? (
            <>
              <TextField
                key={`subject-${blueprint.subjectClause}`}
                label={t('blueprints.subjectEdited')}
                initial={blueprint.subjectClause}
                multiline
                mono
                onCommit={(clause) => void apply(() => api.editSubjectClause(project.folder, blueprint.id, clause))}
              />
              <div className="notice info">
                <span>{t('blueprints.optionsInactive')}</span>
                {confirmReset ? (
                  <div className="row">
                    <span>{t('blueprints.resetConfirm')}</span>
                    <button
                      className="btn sm primary"
                      onClick={() => {
                        setConfirmReset(false);
                        setUnlocked(false);
                        void apply(() => api.resetSubjectClause(project.folder, blueprint.id));
                      }}
                    >
                      {t('blueprints.reset')}
                    </button>
                    <button className="btn sm" onClick={() => setConfirmReset(false)}>{t('common.cancel')}</button>
                  </div>
                ) : (
                  <div>
                    <button className="btn sm" onClick={() => (blueprint.subjectClauseEdited ? setConfirmReset(true) : setUnlocked(false))}>
                      {t('blueprints.reset')}
                    </button>
                  </div>
                )}
              </div>
            </>
          ) : (
            <>
              <div className="prompt" aria-label={t('blueprints.subject')}>🔒 {blueprint.subjectClause}</div>
              <span className="hint">{t('blueprints.subjectHint')}</span>
              <div>
                <button className="btn sm" onClick={() => setUnlocked(true)}>{t('blueprints.customise')}</button>
              </div>
            </>
          )}
        </div>
      </div>

      <div className="group">
        <span className="eyebrow">{t('blueprints.prompt')}</span>
        <span className="hint">{t('blueprints.promptHint')}</span>
        {/* Subject and style, each in its place: the framing clause is never shown (DEC-029), and the front assembles nothing (§I.0). */}
        <div className="prompt">
          <div className="eyebrow">{t('clauses.Subject')}</div>
          {blueprint.subjectClause}
          <div className="eyebrow" style={{ marginTop: '0.5rem' }}>{t('clauses.Style')}</div>
          {project.style.styleClause === '' ? <span className="muted">{t('blueprints.noStyle')}</span> : project.style.styleClause}
        </div>
      </div>

      <div className="group">
        {confirmDelete ? (
          <div className="notice warn">
            <span>{t('blueprints.deleteConfirm', { count: blueprint.candidates.length })}</span>
            <div className="row">
              <button
                className="btn danger"
                onClick={() => {
                  void run(() => api.removeBlueprint(project.folder, blueprint.id)).then(props.onRemoved);
                }}
              >
                {t('common.delete')}
              </button>
              <button className="btn" onClick={() => setConfirmDelete(false)}>{t('common.cancel')}</button>
            </div>
          </div>
        ) : (
          <div>
            <button className="btn danger" onClick={() => setConfirmDelete(true)}>{t('blueprints.delete')}</button>
          </div>
        )}
      </div>

      <NewEntryDialog
        parameter={adding?.parameter ?? null}
        onClose={() => setAdding(null)}
        onAdded={(value) => adding?.apply(value)}
      />
    </section>
  );
}
