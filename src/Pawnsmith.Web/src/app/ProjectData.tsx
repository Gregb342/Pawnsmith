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
}

const Context = createContext<ProjectData | null>(null);

export function useProject(): ProjectData {
  const data = useContext(Context);
  if (data === null) {
    throw new Error('useProject is called outside ProjectProvider.');
  }
  return data;
}

type Loading = { state: 'loading' } | { state: 'failed'; code: string } | { state: 'ready'; project: ProjectDto };

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
      setLoading({ state: 'ready', project });
      onLoaded(project.name);
    } catch (error) {
      setLoading({ state: 'failed', code: codeOf(error) });
      onLoaded(null);
    }
  }, [folder, onLoaded]);

  // The provider is keyed by its folder, so a new folder mounts a new one,
  // which starts in the loading state: nothing to reset here.
  useEffect(() => {
    api.project(folder).then(
      (project) => {
        setLoading({ state: 'ready', project });
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
      setLoading({ state: 'ready', project });
      onLoaded(project.name);
    },
    reload,
  };

  return <Context.Provider value={value}>{props.children}</Context.Provider>;
}
