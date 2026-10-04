import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';

import { ApiError } from '../api/client';

/**
 * The autosave indicator (DEC-111). Every write of the interface goes through
 * `run`, which shows "Saving…", then "Saved", or the translated error code.
 * There is no Save button: a field is written when it is left.
 */
export type SaveState = { kind: 'idle' } | { kind: 'saving' } | { kind: 'saved' } | { kind: 'failed'; code: string };

interface SaveStatus {
  state: SaveState;
  /** Runs one write and reports it; resolves to its result, or to undefined when it failed. */
  run: <T>(write: () => Promise<T>) => Promise<T | undefined>;
}

const Context = createContext<SaveStatus | null>(null);

export function useSaveStatus(): SaveStatus {
  const status = useContext(Context);
  if (status === null) {
    throw new Error('useSaveStatus is called outside SaveStatusProvider.');
  }
  return status;
}

export function codeOf(error: unknown): string {
  return error instanceof ApiError ? error.code : 'UNEXPECTED';
}

export function SaveStatusProvider(props: { children: ReactNode }) {
  const [state, setState] = useState<SaveState>({ kind: 'idle' });

  const run = useCallback(async <T,>(write: () => Promise<T>): Promise<T | undefined> => {
    setState({ kind: 'saving' });
    try {
      const result = await write();
      setState({ kind: 'saved' });
      return result;
    } catch (error) {
      setState({ kind: 'failed', code: codeOf(error) });
      return undefined;
    }
  }, []);

  const value = useMemo(() => ({ state, run }), [state, run]);

  return <Context.Provider value={value}>{props.children}</Context.Provider>;
}
