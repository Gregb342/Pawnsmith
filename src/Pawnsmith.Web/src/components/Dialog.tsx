import { useEffect, useRef } from 'react';
import type { ReactNode } from 'react';

/**
 * A modal dialog on the browser's own <dialog> element: focus is kept inside,
 * Escape closes it, and the page behind is inert — without a library.
 */
export function Dialog(props: { open: boolean; title: string; onClose: () => void; children: ReactNode }) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (dialog === null) {
      return;
    }
    if (props.open && !dialog.open) {
      dialog.showModal();
    } else if (!props.open && dialog.open) {
      dialog.close();
    }
  }, [props.open]);

  return (
    <dialog ref={ref} onClose={props.onClose} aria-label={props.title}>
      <h3>{props.title}</h3>
      {props.open ? props.children : null}
    </dialog>
  );
}
