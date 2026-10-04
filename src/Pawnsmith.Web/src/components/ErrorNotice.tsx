import { useErrorText } from './text';

/** A refused request, said in words, with its code for whoever reads the logs. */
export function ErrorNotice(props: { code: string | null }) {
  const text = useErrorText();

  if (props.code === null) {
    return null;
  }

  return (
    <div className="notice err" role="alert">
      <span>{text(props.code)}</span>
      <span className="mono small muted">{props.code}</span>
    </div>
  );
}
