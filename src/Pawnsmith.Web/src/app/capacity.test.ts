import { describe, expect, it } from 'vitest';

import type { BlueprintDto, Size, SizeCapacityDto } from '../api/types';
import { sizesThatDoNotFit } from './capacity';

// I.15.6 n° 12 — the zero-capacity sizes the sheet would print (§15.4, DEC-117).

const capacities: SizeCapacityDto[] = [
  { size: 'Small', capacity: 6 },
  { size: 'Medium', capacity: 6 },
  { size: 'Large', capacity: 0 },
  { size: 'Huge', capacity: 2 },
  { size: 'Gargantuan', capacity: 0 },
];

const blueprint = (size: Size, kept: boolean): BlueprintDto => ({
  id: `${size}-${kept}`,
  race: 'goblin',
  characterClass: 'skirmisher',
  size,
  optionalParameters: [],
  details: '',
  subjectClause: 'a goblin skirmisher',
  subjectClauseEdited: false,
  resolvedPrompt: null,
  quantity: 1,
  electedCandidateId: kept ? 'c1' : null,
  candidates: [],
});

describe('sizesThatDoNotFit', () => {
  it('names a zero-capacity size that a kept proposal would print', () => {
    expect(sizesThatDoNotFit(capacities, [blueprint('Gargantuan', true), blueprint('Medium', true)])).toEqual(['Gargantuan']);
  });

  it('leaves out a zero-capacity size nothing prints', () => {
    // A Large blueprint without a kept proposal is skipped by the sheet (DEC-069).
    expect(sizesThatDoNotFit(capacities, [blueprint('Large', false), blueprint('Medium', true)])).toEqual([]);
  });

  it('names every such size, in the order of the sizes', () => {
    expect(sizesThatDoNotFit(capacities, [blueprint('Gargantuan', true), blueprint('Large', true)])).toEqual(['Large', 'Gargantuan']);
  });

  it('names nothing when every printed size fits', () => {
    expect(sizesThatDoNotFit(capacities, [blueprint('Small', true), blueprint('Huge', true)])).toEqual([]);
  });
});
