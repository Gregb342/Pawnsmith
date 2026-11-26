import type { BlueprintDto, Size, SizeCapacityDto } from '../api/types';

/**
 * The sizes a page of the project cannot hold at all, among those it prints
 * (§15.4, DEC-117).
 *
 * The report of the sheet refuses such a project with SHEET_CAPACITY_EXCEEDED
 * and nothing else (DEC-084). The capacities route says which size has a
 * capacity of zero; this keeps the ones a kept proposal would put on the
 * sheet, so that the notice names a size the user is actually printing.
 *
 * Nothing is computed here: the zero comes from the API, and "printed" is
 * "has a kept proposal", which the API already requires to be cut out
 * (DEC-071). The order is the API's, Small to Gargantuan.
 */
export function sizesThatDoNotFit(capacities: readonly SizeCapacityDto[], blueprints: readonly BlueprintDto[]): Size[] {
  const printed = new Set(blueprints.filter((blueprint) => blueprint.electedCandidateId !== null).map((blueprint) => blueprint.size));

  return capacities.filter((line) => line.capacity === 0 && printed.has(line.size)).map((line) => line.size);
}
