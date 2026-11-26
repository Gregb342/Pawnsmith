import { describe, expect, it } from 'vitest';

import { format, parse, steps } from './router';
import type { Route } from './router';

// I.15.6 n° 6 — the routing reads and writes every route, both ways, and a
// malformed address never throws (the page would go blank, I.12).

describe('router', () => {
  const routes: Route[] = [
    { name: 'projects' },
    { name: 'generator' },
    { name: 'catalog' },
    { name: 'logs' },
    ...steps.map((step): Route => ({ name: 'project', folder: 'donjon-des-gobelins', step })),
  ];

  it.each(routes)('reads back what it writes: %o', (route) => {
    expect(parse(format(route))).toEqual(route);
  });

  it('writes the five steps under the project folder', () => {
    expect(format({ name: 'project', folder: 'donjon', step: 'layout' })).toBe('#/p/donjon/layout');
  });

  it('encodes a folder that is not a plain name, and decodes it back', () => {
    // The API only ever gives canonical folder names (DEC-083); an address
    // typed by hand is another matter.
    const route: Route = { name: 'project', folder: 'a b/c', step: 'print' };

    expect(format(route)).toBe('#/p/a%20b%2Fc/print');
    expect(parse(format(route))).toEqual(route);
  });

  it('opens the Project step when the step is missing or unknown', () => {
    expect(parse('#/p/donjon')).toEqual({ name: 'project', folder: 'donjon', step: 'project' });
    expect(parse('#/p/donjon/nowhere')).toEqual({ name: 'project', folder: 'donjon', step: 'project' });
  });

  it('falls back to the project list for anything else', () => {
    for (const hash of ['', '#', '#/', '#/projects', '#/nowhere', '#/p', '#/p/']) {
      expect(parse(hash)).toEqual({ name: 'projects' });
    }
  });

  it('keeps a malformed escape as written instead of throwing', () => {
    // decodeURIComponent('%E0') throws; the API answers PROJECT_NOT_FOUND.
    expect(parse('#/p/%E0/layout')).toEqual({ name: 'project', folder: '%E0', step: 'layout' });
  });
});
