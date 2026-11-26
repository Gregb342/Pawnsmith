import { useCallback, useSyncExternalStore } from 'react';

/**
 * Routing on the URL fragment (`#/…`), written by hand (DEC-114).
 *
 * Why the fragment: the server hands every unknown path back to index.html
 * (A.6), but a fragment never reaches the server at all, so a reload or a
 * bookmark always lands on the application. Why by hand: five steps and four
 * pages fit in thirty lines, and a routing library would be a dependency for
 * them (§3 of CLAUDE.md).
 */

export const steps = ['project', 'blueprints', 'generation', 'layout', 'print'] as const;
export type Step = (typeof steps)[number];

export type Route =
  | { name: 'projects' }
  | { name: 'project'; folder: string; step: Step }
  | { name: 'generator' }
  | { name: 'catalog' }
  | { name: 'logs' };

/** A malformed escape such as `%E0` would make decodeURIComponent throw, and the whole page with it. */
function decode(part: string): string {
  try {
    return decodeURIComponent(part);
  } catch {
    return part;
  }
}

export function parse(hash: string): Route {
  const parts = hash.replace(/^#\/?/, '').split('/').filter((part) => part.length > 0).map(decode);

  if (parts[0] === 'p' && parts[1] !== undefined) {
    const step = steps.find((each) => each === parts[2]) ?? 'project';
    return { name: 'project', folder: parts[1], step };
  }

  if (parts[0] === 'generator' || parts[0] === 'catalog' || parts[0] === 'logs') {
    return { name: parts[0] };
  }

  return { name: 'projects' };
}

export function format(route: Route): string {
  switch (route.name) {
    case 'project':
      return `#/p/${encodeURIComponent(route.folder)}/${route.step}`;
    case 'projects':
      return '#/projects';
    default:
      return `#/${route.name}`;
  }
}

function subscribe(onChange: () => void): () => void {
  window.addEventListener('hashchange', onChange);
  return () => window.removeEventListener('hashchange', onChange);
}

/** The current route, and a function to go to another one. */
export function useRoute(): [Route, (route: Route) => void] {
  const hash = useSyncExternalStore(subscribe, () => window.location.hash);
  const navigate = useCallback((route: Route) => {
    window.location.hash = format(route);
  }, []);

  return [parse(hash), navigate];
}
