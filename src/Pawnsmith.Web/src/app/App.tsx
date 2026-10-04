import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { ErrorNotice } from '../components/ErrorNotice';
import { StepRail, TopBar } from '../components/Shell';
import { Placeholder } from '../screens/Placeholder';
import { AppDataProvider } from './AppData';
import { ProjectProvider } from './ProjectData';
import { useRoute } from './router';
import type { Route } from './router';
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
      return <Placeholder title={`steps.${route.step}`} />;
    case 'projects':
      return <Placeholder title="projects.title" />;
    default:
      return <Placeholder title={`${route.name}.title`} />;
  }
}
