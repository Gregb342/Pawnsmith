import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';

import { api } from '../api/client';
import type { ProjectDto } from '../api/types';
import { codeOf } from './SaveStatus';

/**
 * The open project, as the API last returned it. Every write answers with the
 * project or the blueprint after the change, and the screens put that answer
 * here: what is shown is always what was saved, never a local guess.
 */
interface ProjectData {
  project: ProjectDto;
  setProject: (project: ProjectDto) => void;
  reload: () => Promise<void>;
  /** Why the last reload failed, while the project it could not replace stays shown; null otherwise. */
  reloadError: string | null;
}

const Context = createContext<ProjectData | null>(null);

export function useProject(): ProjectData {
  const data = useContext(Context);
  if (data === null) {
    throw new Error('useProject is called outside ProjectProvider.');
  }
  return data;
}

type Loading =
  | { state: 'loading' }
  | { state: 'failed'; code: string }
  | { state: 'ready'; project: ProjectDto; reloadError: string | null };

export function ProjectProvider(props: {
  folder: string;
  children: ReactNode;
  loading: ReactNode;
  failed: (code: string) => ReactNode;
  onLoaded: (name: string | null) => void;
}) {
  const { folder, onLoaded } = props;
  const [loading, setLoading] = useState<Loading>({ state: 'loading' });

  const reload = useCallback(async () => {
    try {
      const project = await api.project(folder);
      setLoading({ state: 'ready', project, reloadError: null });
      onLoaded(project.name);
    } catch (error) {
      // A project already on screen stays there: a server stopped for a
      // moment, or a network that drops, must not throw the user back to an
      // error page and the list (I.15.5). Only a first load that fails has
      // nothing to show but its error.
      const code = codeOf(error);
      setLoading((current) => (current.state === 'ready' ? { ...current, reloadError: code } : { state: 'failed', code }));
    }
  }, [folder, onLoaded]);

  // The provider is keyed by its folder, so a new folder mounts a new one,
  // which starts in the loading state: nothing to reset here.
  useEffect(() => {
    api.project(folder).then(
      (project) => {
        setLoading({ state: 'ready', project, reloadError: null });
        onLoaded(project.name);
      },
      (error: unknown) => {
        setLoading({ state: 'failed', code: codeOf(error) });
        onLoaded(null);
      },
    );
  }, [folder, onLoaded]);

  if (loading.state === 'loading') {
    return <>{props.loading}</>;
  }

  if (loading.state === 'failed') {
    return <>{props.failed(loading.code)}</>;
  }

  const value: ProjectData = {
    project: loading.project,
    setProject: (project) => {
      setLoading({ state: 'ready', project, reloadError: null });
      onLoaded(project.name);
    },
    reload,
    reloadError: loading.reloadError,
  };

  return <Context.Provider value={value}>{props.children}</Context.Provider>;
}
