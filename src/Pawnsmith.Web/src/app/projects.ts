import type { ProjectListingDto } from '../api/types';

/**
 * The project identifiers that more than one folder carries.
 *
 * Two folders with the same projectId are two copies — an archive imported
 * twice (DEC-047). The list says so on each of them, and asks nothing: there
 * is nothing to decide (§I.10.2). An unreadable project has no identifier and
 * is never counted.
 */
export function sharedProjectIds(listings: readonly ProjectListingDto[]): Set<string> {
  const seen = new Set<string>();
  const shared = new Set<string>();

  for (const listing of listings) {
    if (listing.projectId === null) {
      continue;
    }
    if (seen.has(listing.projectId)) {
      shared.add(listing.projectId);
    }
    seen.add(listing.projectId);
  }

  return shared;
}
