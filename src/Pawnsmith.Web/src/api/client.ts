import type {
  ArchiveProfile,
  BlueprintDto,
  CatalogDto,
  ConfigurationDto,
  EditedBlueprintDto,
  GeneratorDto,
  JobDto,
  Labels,
  LogListDto,
  LogTailDto,
  OverridesDto,
  ParameterDto,
  ProjectDto,
  ProjectListingDto,
  SheetReportDto,
  Size,
  StyleDto,
  StylePresetDto,
  Geometry,
  Universe,
  CandidateStatus,
} from './types';

/**
 * An API refusal: a code, never a message (DEC-084). The interface translates
 * the code with the key `errors.{code}`.
 *
 * Two codes are made up here, for failures that never reach the API's error
 * middleware: NETWORK_ERROR when the server cannot be reached at all, and
 * HTTP_{status} for an answer that is not the `{ code }` shape — the `400` of
 * a refused Host or the `413` of an oversized body, both written by the
 * server itself (§G.3).
 */
export class ApiError extends Error {
  constructor(
    readonly code: string,
    readonly status: number,
  ) {
    super(code);
    this.name = 'ApiError';
  }
}

async function send(method: string, path: string, body?: unknown, contentType = 'application/json'): Promise<Response> {
  let response: Response;

  try {
    response = await fetch(path, {
      method,
      headers: body === undefined ? undefined : { 'Content-Type': contentType },
      body: body === undefined ? undefined : body instanceof Blob ? body : JSON.stringify(body),
    });
  } catch {
    throw new ApiError('NETWORK_ERROR', 0);
  }

  if (!response.ok) {
    throw new ApiError(await codeOf(response), response.status);
  }

  return response;
}

async function codeOf(response: Response): Promise<string> {
  try {
    const body: unknown = await response.json();
    if (typeof body === 'object' && body !== null && 'code' in body && typeof body.code === 'string') {
      return body.code;
    }
  } catch {
    // Not JSON: a response the server wrote itself.
  }

  return `HTTP_${response.status}`;
}

async function json<T>(method: string, path: string, body?: unknown): Promise<T> {
  const response = await send(method, path, body);
  return (await response.json()) as T;
}

/** The file name of an attachment, from Content-Disposition; the fallback otherwise. */
function fileNameOf(response: Response, fallback: string): string {
  const header = response.headers.get('Content-Disposition') ?? '';
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(header);
  return match?.[1] ? decodeURIComponent(match[1]) : fallback;
}

/** Hands a blob to the browser as a download. */
function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

const encode = encodeURIComponent;
const projectPath = (folder: string) => `/api/projects/${encode(folder)}`;
const blueprintPath = (folder: string, id: string) => `${projectPath(folder)}/blueprints/${encode(id)}`;

/** The body of a blueprint's fields, for creation and update. */
export interface BlueprintFields {
  race: string;
  characterClass: string;
  size: Size;
  optionalParameters: ParameterDto[];
  details: string;
  quantity: number;
}

/** The body of a project's settings (§G.6). The palette is sent back as received (DEC-110). */
export interface ProjectSettings {
  name: string;
  universe: Universe;
  geometry: Geometry;
  paperFormat: string;
  style: StyleDto;
  calibrationOverrides: OverridesDto;
}

/** Every route the front uses, one function each. Nothing here decides anything (§I.0). */
export const api = {
  configuration: () => json<ConfigurationDto>('GET', '/api/configuration'),
  catalog: (universe: Universe) => json<CatalogDto>('GET', `/api/universes/${universe}/catalog`),
  addCatalogEntry: (universe: Universe, entry: { key: string; value: string; labels: Labels; fragment: string }) =>
    json<CatalogDto>('POST', `/api/universes/${universe}/catalog/entries`, entry),
  removeCatalogEntry: (universe: Universe, key: string, value: string) =>
    json<CatalogDto>('DELETE', `/api/universes/${universe}/catalog/entries/${encode(key)}/${encode(value)}`),

  styles: (universe: Universe) => json<StylePresetDto[]>('GET', `/api/universes/${universe}/styles`),
  addStyle: (universe: Universe, style: { name: string; styleClause: string; negativeClause: string }) =>
    json<StylePresetDto[]>('POST', `/api/universes/${universe}/styles`, style),
  removeStyle: (universe: Universe, id: string) => json<StylePresetDto[]>('DELETE', `/api/universes/${universe}/styles/${encode(id)}`),

  generator: () => json<GeneratorDto>('GET', '/api/generator'),
  setGeneratorAddress: (address: string | null) => json<GeneratorDto>('PUT', '/api/generator', { address }),

  projects: () => json<ProjectListingDto[]>('GET', '/api/projects'),
  createProject: (body: { name: string; universe: Universe; geometry: Geometry; paperFormat: string }) =>
    json<ProjectDto>('POST', '/api/projects', body),
  project: (folder: string) => json<ProjectDto>('GET', projectPath(folder)),
  saveSettings: (folder: string, settings: ProjectSettings) => json<ProjectDto>('PUT', `${projectPath(folder)}/settings`, settings),
  duplicate: (folder: string, name: string, style: StyleDto | null) =>
    json<ProjectDto>('POST', `${projectPath(folder)}/duplicate`, { name, style }),

  addBlueprint: (folder: string, fields: BlueprintFields) => json<EditedBlueprintDto>('POST', `${projectPath(folder)}/blueprints`, fields),
  updateBlueprint: (folder: string, id: string, fields: BlueprintFields) => json<EditedBlueprintDto>('PUT', blueprintPath(folder, id), fields),
  editSubjectClause: (folder: string, id: string, clause: string) =>
    json<EditedBlueprintDto>('PUT', `${blueprintPath(folder, id)}/subject-clause`, { clause }),
  resetSubjectClause: (folder: string, id: string) => json<EditedBlueprintDto>('DELETE', `${blueprintPath(folder, id)}/subject-clause`),
  removeBlueprint: async (folder: string, id: string) => {
    await send('DELETE', blueprintPath(folder, id));
  },
  elect: (folder: string, id: string, candidateId: string | null) =>
    json<EditedBlueprintDto>('PUT', `${blueprintPath(folder, id)}/election`, { candidateId }),
  setStatus: (folder: string, id: string, candidateId: string, status: CandidateStatus) =>
    json<EditedBlueprintDto>('PUT', `${blueprintPath(folder, id)}/candidates/${encode(candidateId)}/status`, { status }),
  cutOut: (folder: string, id: string, candidateId: string) =>
    json<EditedBlueprintDto>('POST', `${blueprintPath(folder, id)}/candidates/${encode(candidateId)}/cutout`),

  /** The URL of a stored image path such as `images/{id}-front.png` (DEC-088). */
  imageUrl: (folder: string, stored: string) => `${projectPath(folder)}/${stored.split('/').map(encode).join('/')}`,

  startJob: (folder: string, blueprintId: string, body: { count?: number; seeds?: string[] }) =>
    json<JobDto>('POST', `${blueprintPath(folder, blueprintId)}/jobs`, body),
  jobs: () => json<JobDto[]>('GET', '/api/jobs'),
  cancelJob: (id: string) => json<JobDto>('POST', `/api/jobs/${encode(id)}/cancel`),

  sheetReport: (folder: string) => json<SheetReportDto>('GET', `${projectPath(folder)}/sheet/report`),
  sheetUrl: (folder: string, culture: string, inline: boolean) =>
    `${projectPath(folder)}/sheet.pdf?culture=${encode(culture)}${inline ? '&disposition=inline' : ''}`,

  exportArchive: async (folder: string, profile: ArchiveProfile) => {
    const response = await send('POST', `${projectPath(folder)}/archives?profile=${profile}`);
    saveBlob(await response.blob(), fileNameOf(response, `${folder}-${profile}.zip`));
  },
  importArchive: async (name: string, file: File) => {
    const response = await send('POST', `/api/projects/import?name=${encode(name)}`, file, 'application/zip');
    return (await response.json()) as ProjectDto;
  },

  logs: () => json<LogListDto>('GET', '/api/logs'),
  logTail: (name: string, lines: number) => json<LogTailDto>('GET', `/api/logs/${encode(name)}?lines=${lines}`),
};

/** Replaces one blueprint of a project with its edited version. */
export function withBlueprint(project: ProjectDto, blueprint: BlueprintDto): ProjectDto {
  return { ...project, blueprints: project.blueprints.map((each) => (each.id === blueprint.id ? blueprint : each)) };
}
