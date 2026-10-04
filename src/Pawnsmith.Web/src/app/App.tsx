import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { ErrorNotice } from '../components/ErrorNotice';
import { StepRail, TopBar } from '../components/Shell';
import { BlueprintsScreen } from '../screens/BlueprintsScreen';
import { CatalogScreen } from '../screens/CatalogScreen';
import { GenerationScreen } from '../screens/GenerationScreen';
import { GeneratorScreen } from '../screens/GeneratorScreen';
import { LayoutScreen } from '../screens/LayoutScreen';
import { LogsScreen } from '../screens/LogsScreen';
import { PrintScreen } from '../screens/PrintScreen';
import { ProjectScreen } from '../screens/ProjectScreen';
import { ProjectsScreen } from '../screens/ProjectsScreen';
import { AppDataProvider } from './AppData';
import { ProjectProvider } from './ProjectData';
import { useRoute } from './router';
import type { Route, Step } from './router';
import { SaveStatusProvider } from './SaveStatus';

/**
 * The application: a top bar, the five steps when a project is open, and the
 * screen of the current route (§I.10.1).
 */
export function App() {
  const { t, i18n } = useTranslation();
  const [route, navigate] = useRoute();
  const [projectName, setProjectName] = useState<string | null>(null);

  // index.html ships an empty <title> and lang; both follow the language.
  useEffect(() => {
    document.title = t('app.documentTitle');
    document.documentElement.lang = i18n.resolvedLanguage ?? 'fr';
  }, [t, i18n.resolvedLanguage]);

  const loading = <p className="work muted">{t('app.loading')}</p>;
  const failed = (code: string) => (
    <div className="work">
      <ErrorNotice code={code} />
    </div>
  );

  return (
    <AppDataProvider loading={loading} failed={failed}>
      <SaveStatusProvider>
        <TopBar route={route} projectName={route.name === 'project' ? projectName : null} navigate={navigate} />
        {route.name === 'project' ? (
          <ProjectProvider key={route.folder} folder={route.folder} loading={loading} failed={failed} onLoaded={setProjectName}>
            <StepRail folder={route.folder} step={route.step} navigate={navigate} />
            <main className="work">
              <Screen route={route} navigate={navigate} />
            </main>
          </ProjectProvider>
        ) : (
          <main className="work">
            <Screen route={route} navigate={navigate} />
          </main>
        )}
      </SaveStatusProvider>
    </AppDataProvider>
  );
}

function Screen(props: { route: Route; navigate: (route: Route) => void }) {
  const { route } = props;

  switch (route.name) {
    case 'project':
      return <StepScreen step={route.step} goTo={(step) => props.navigate({ name: 'project', folder: route.folder, step })} navigate={props.navigate} />;
    case 'projects':
      return <ProjectsScreen navigate={props.navigate} />;
    case 'catalog':
      return <CatalogScreen />;
    case 'generator':
      return <GeneratorScreen />;
    case 'logs':
      return <LogsScreen />;
  }
}

function StepScreen(props: { step: Step; goTo: (step: Step) => void; navigate: (route: Route) => void }) {
  switch (props.step) {
    case 'project':
      return <ProjectScreen navigate={props.navigate} />;
    case 'blueprints':
      return <BlueprintsScreen />;
    case 'generation':
      return <GenerationScreen />;
    case 'layout':
      return <LayoutScreen goTo={props.goTo} />;
    case 'print':
      return <PrintScreen />;
  }
}
