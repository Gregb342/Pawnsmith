import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';

import { api } from '../api/client';
import type { CatalogDto, ConfigurationDto, GeneratorDto, StylePresetDto } from '../api/types';
import { codeOf } from './SaveStatus';

/**
 * What every screen reads and few change: the configuration, the catalogue,
 * the style library and the generator's state. Loaded once; the catalogue,
 * the styles and the generator are replaced by whatever their routes answer.
 */
export interface AppData {
  configuration: ConfigurationDto;
  catalog: CatalogDto;
  setCatalog: (catalog: CatalogDto) => void;
  styles: StylePresetDto[];
  setStyles: (styles: StylePresetDto[]) => void;
  generator: GeneratorDto | null;
  setGenerator: (generator: GeneratorDto) => void;
  refreshGenerator: () => Promise<void>;
}

/** How often the generator's state is checked again while the interface is open. */
const GENERATOR_CHECK_MS = 30_000;

const Context = createContext<AppData | null>(null);

export function useAppData(): AppData {
  const data = useContext(Context);
  if (data === null) {
    throw new Error('useAppData is called outside AppDataProvider.');
  }
  return data;
}

type Loading = { state: 'loading' } | { state: 'failed'; code: string } | { state: 'ready'; configuration: ConfigurationDto; catalog: CatalogDto; styles: StylePresetDto[] };

/** Loads the shared data, then renders its children; or renders the fallbacks while loading or failed. */
export function AppDataProvider(props: { children: ReactNode; loading: ReactNode; failed: (code: string) => ReactNode }) {
  const [loading, setLoading] = useState<Loading>({ state: 'loading' });
  const [generator, setGenerator] = useState<GeneratorDto | null>(null);

  const refreshGenerator = useCallback(async () => {
    try {
      setGenerator(await api.generator());
    } catch {
      // The chip shows "unknown" rather than an error banner: the generator
      // is checked live and may simply be slow to answer.
      setGenerator(null);
    }
  }, []);

  useEffect(() => {
    async function load() {
      try {
        const configuration = await api.configuration();
        const universe = configuration.universes[0] ?? 'Fantasy';
        const [catalog, styles] = await Promise.all([api.catalog(universe), api.styles(universe)]);
        setLoading({ state: 'ready', configuration, catalog, styles });
      } catch (error) {
        // A code, never an exception's message: an answer that is not JSON
        // would otherwise put an English parser message on the screen.
        setLoading({ state: 'failed', code: codeOf(error) });
      }
    }

    void load();

    // A promise callback rather than refreshGenerator: an effect does not set
    // state synchronously (react-hooks/set-state-in-effect).
    const check = () => api.generator().then(setGenerator, () => setGenerator(null));
    void check();

    // Checked again now and then: ComfyUI started after the interface must
    // not stay "unreachable" in the bar until a reload. Each check is one
    // live call to the generator (§E.7.1), hence the slow pace.
    const timer = window.setInterval(() => void check(), GENERATOR_CHECK_MS);
    return () => window.clearInterval(timer);
  }, []);

  if (loading.state === 'loading') {
    return <>{props.loading}</>;
  }

  if (loading.state === 'failed') {
    return <>{props.failed(loading.code)}</>;
  }

  const value: AppData = {
    configuration: loading.configuration,
    catalog: loading.catalog,
    setCatalog: (catalog) => setLoading({ ...loading, catalog }),
    styles: loading.styles,
    setStyles: (styles) => setLoading({ ...loading, styles }),
    generator,
    setGenerator,
    refreshGenerator,
  };

  return <Context.Provider value={value}>{props.children}</Context.Provider>;
}
