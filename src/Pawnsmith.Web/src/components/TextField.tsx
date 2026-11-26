import { useId, useState } from 'react';

/**
 * A text field saved when it is left (DEC-111). It holds its own draft while
 * the user types and calls onCommit on blur, only when the text changed.
 *
 * The parent gives it a `key` made of the saved value: when the API answers
 * with a new value, the field is remounted with it. That is how the draft is
 * reset without an effect that sets state.
 */
export function TextField(props: {
  label: string;
  initial: string;
  onCommit: (value: string) => void;
  hint?: string;
  multiline?: boolean;
  disabled?: boolean;
  mono?: boolean;
  placeholder?: string;
  type?: 'text' | 'number';
}) {
  const id = useId();
  const [draft, setDraft] = useState(props.initial);

  function commit() {
    if (draft !== props.initial) {
      props.onCommit(draft);
    }
  }

  const common = {
    id,
    value: draft,
    disabled: props.disabled,
    placeholder: props.placeholder,
    className: props.mono ? 'mono' : undefined,
    onBlur: commit,
  };

  return (
    <div className="field">
      <label htmlFor={id}>{props.label}</label>
      {props.multiline ? (
        <textarea {...common} onChange={(event) => setDraft(event.target.value)} />
      ) : (
        <input {...common} type={props.type ?? 'text'} onChange={(event) => setDraft(event.target.value)} />
      )}
      {props.hint ? <span className="hint">{props.hint}</span> : null}
    </div>
  );
}
