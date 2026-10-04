/**
 * The shapes the API answers with — a hand-written mirror of the C# DTOs of
 * Pawnsmith.Api/Contracts, field for field. No generator: a mapping that
 * writes itself is the kind of hidden code DEC-021 rules out, and these types
 * are short enough to read against their C# counterparts.
 *
 * Enumerations travel as their names (§G.4), so they are string unions here.
 */

export type Size = 'Small' | 'Medium' | 'Large' | 'Huge' | 'Gargantuan';
export type Geometry = 'FoldedTent' | 'TabAndSocket' | 'NoSupport';
export type Universe = 'Fantasy';
export type CandidateStatus = 'Draft' | 'Valid' | 'Rejected';
export type ClauseKind = 'Framing' | 'Subject' | 'Style';
export type Origin = 'Shipped' | 'Personal';
export type JobState = 'Queued' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';
export type GeneratorState = 'Available' | 'Unreachable' | 'Unhealthy' | 'NotConfigured' | 'Misconfigured';
export type ArchiveProfile = 'Backup' | 'Share';

/** A label per interface culture: { en: …, fr: … }. */
export type Labels = Record<string, string>;

export interface PaperFormatDto { name: string; widthMm: number; heightMm: number }
export interface SizeDto { size: Size; gridFootprintMm: number; pawnWidthMm: number; pawnHeightMm: number }
export interface ConfigurationDto {
  paperFormats: PaperFormatDto[];
  sizes: SizeDto[];
  geometries: Geometry[];
  universes: Universe[];
  cultures: string[];
}

export interface CatalogEntryDto { value: string; fragment: string; labels: Labels; origin: Origin }
export interface CatalogParameterDto { key: string; labels: Labels; entries: CatalogEntryDto[] }
export interface CatalogDto { universe: Universe; parameters: CatalogParameterDto[] }

export interface StylePresetDto { id: string; names: Labels; styleClause: string; negativeClause: string; origin: Origin }

export interface GeneratorDto { state: GeneratorState; code: string | null; address: string | null; framingClause: string | null }

export interface StyleDto { name: string; styleClause: string; negativeClause: string; palette: string }
export interface OverridesDto { tabWidthMm: number | null; tabHeightMm: number | null }
export interface ParameterDto { key: string; value: string }

export interface CandidateDto {
  id: string;
  /** A decimal string, never a number: past 2^53 JavaScript would round it (§G.4). */
  seed: string;
  status: CandidateStatus;
  /** null when the misalignment is unknown — no workflow is read. */
  misalignedClauses: ClauseKind[] | null;
  subjectClauseUsed: string;
  styleClauseUsed: string;
  pairedImage: string | null;
  frontImage: string | null;
  backImage: string | null;
  generatedAt: string;
}

export interface BlueprintDto {
  id: string;
  race: string;
  characterClass: string;
  size: Size;
  optionalParameters: ParameterDto[];
  details: string;
  subjectClause: string;
  subjectClauseEdited: boolean;
  resolvedPrompt: string | null;
  quantity: number;
  electedCandidateId: string | null;
  candidates: CandidateDto[];
}

export interface DiagnosticDto { kind: string; field: string }

export interface ProjectDto {
  folder: string;
  projectId: string;
  name: string;
  universe: Universe;
  geometry: Geometry;
  paperFormat: string;
  style: StyleDto;
  calibrationOverrides: OverridesDto;
  blueprints: BlueprintDto[];
  createdAt: string;
  modifiedAt: string;
  diagnostics: DiagnosticDto[];
  misalignmentKnown: boolean;
  universeAndStyleFrozen: boolean;
}

export interface ProjectListingDto {
  folder: string;
  projectId: string | null;
  name: string | null;
  modifiedAt: string | null;
  errorCode: string | null;
}

export interface CompositionDiagnosticDto { key: string; value: string }
export interface EditedBlueprintDto { blueprint: BlueprintDto; compositionDiagnostics: CompositionDiagnosticDto[] }

export interface CutoutFailureDto { candidateId: string; code: string }
export interface JobDto {
  id: string;
  folder: string;
  blueprintId: string;
  state: JobState;
  requested: number;
  produced: string[];
  failureCode: string | null;
  cutoutFailures: CutoutFailureDto[];
}

export interface PageDto { number: number; size: Size; capacity: number; used: number }
export interface SkippedDto { blueprintId: string; reason: string }
export interface MisalignedElectionDto { blueprintId: string; candidateId: string; clauses: ClauseKind[] }
export interface WidthLimitedDto { name: string; size: Size; printedHeightMm: number; availableHeightMm: number; heightUsage: number }
export interface SheetReportDto {
  pages: PageDto[];
  skipped: SkippedDto[];
  misalignedElections: MisalignedElectionDto[];
  misalignmentKnown: boolean;
  widthLimited: WidthLimitedDto[];
}

export interface LogFileDto { name: string; sizeBytes: number; lastWriteUtc: string }
export interface LogListDto { enabled: boolean; files: LogFileDto[] }
export interface LogTailDto { name: string; lines: string[]; truncated: boolean }
