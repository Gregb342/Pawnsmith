import { describe, expect, it } from 'vitest';

import type { ProjectListingDto } from '../api/types';
import { sharedProjectIds } from './projects';

// I.15.6 n° 11 — two folders with one projectId are copies (DEC-047).

const listing = (folder: string, projectId: string | null): ProjectListingDto => ({
  folder,
  projectId,
  name: projectId === null ? null : folder,
  modifiedAt: projectId === null ? null : '2026-11-25T15:00:00Z',
  errorCode: projectId === null ? 'PROJECT_INVALID' : null,
});

describe('sharedProjectIds', () => {
  it('finds an identifier carried by two folders, and only that one', () => {
    const shared = sharedProjectIds([listing('donjon', 'a'), listing('donjon-2', 'a'), listing('foret', 'b')]);

    expect([...shared]).toEqual(['a']);
  });

  it('counts three copies once', () => {
    expect([...sharedProjectIds([listing('x', 'a'), listing('y', 'a'), listing('z', 'a')])]).toEqual(['a']);
  });

  it('never counts unreadable projects, which have no identifier', () => {
    expect(sharedProjectIds([listing('broken', null), listing('also-broken', null)]).size).toBe(0);
  });

  it('finds nothing in an empty list', () => {
    expect(sharedProjectIds([]).size).toBe(0);
  });
});
