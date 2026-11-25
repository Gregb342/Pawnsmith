import { afterEach, describe, expect, it, vi } from 'vitest';

import { codeOf } from '../app/SaveStatus';
import { api, ApiError, fileNameOf } from './client';

// I.15.6 n° 7 to 10 — how the front reads the API's answers and builds its
// requests. fetch is replaced by a stand-in that records what it was asked
// and answers what the test says; Response and File are Node's own.

type Answer = () => Promise<Response>;

function stubFetch(answer: Answer) {
  // Typed with fetch's own parameters, so that the recorded calls can be read.
  const fetch = vi.fn<(input: string, init?: RequestInit) => Promise<Response>>(() => answer());
  vi.stubGlobal('fetch', fetch);
  return fetch;
}

const json = (body: unknown, status: number) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });

async function refusal(call: () => Promise<unknown>): Promise<ApiError> {
  try {
    await call();
  } catch (error) {
    if (error instanceof ApiError) {
      return error;
    }
    throw error;
  }
  throw new Error('The call was expected to be refused.');
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('a refusal is a code (DEC-084)', () => {
  it('reads the code of a { code } body, with its status', async () => {
    stubFetch(() => Promise.resolve(json({ code: 'PROJECT_NOT_FOUND' }, 404)));

    const error = await refusal(() => api.project('nope'));

    expect(error.code).toBe('PROJECT_NOT_FOUND');
    expect(error.status).toBe(404);
  });

  it('names an answer without the { code } shape by its status', async () => {
    // The 413 Kestrel writes itself for an oversized body has no body at all.
    stubFetch(() => Promise.resolve(new Response(null, { status: 413 })));

    expect((await refusal(() => api.projects())).code).toBe('HTTP_413');
  });

  it('names a JSON body that is not { code } by its status too', async () => {
    stubFetch(() => Promise.resolve(json({ message: 'something' }, 400)));

    expect((await refusal(() => api.projects())).code).toBe('HTTP_400');
  });

  it('turns a server that cannot be reached into NETWORK_ERROR', async () => {
    stubFetch(() => Promise.reject(new TypeError('fetch failed')));

    const error = await refusal(() => api.projects());

    expect(error.code).toBe('NETWORK_ERROR');
    expect(error.status).toBe(0);
  });

  it('gives anything that is not an ApiError the code UNEXPECTED', () => {
    expect(codeOf(new ApiError('STYLE_FROZEN', 409))).toBe('STYLE_FROZEN');
    expect(codeOf(new Error('boom'))).toBe('UNEXPECTED');
    expect(codeOf('boom')).toBe('UNEXPECTED');
  });
});

describe('requests', () => {
  it('sends a write as JSON, to the folder of the project', async () => {
    const fetch = stubFetch(() => Promise.resolve(json({ blueprint: {}, compositionDiagnostics: [] }, 200)));

    await api.editSubjectClause('donjon', 'b1', 'a goblin');

    const [path, init] = fetch.mock.calls[0]!;
    expect(path).toBe('/api/projects/donjon/blueprints/b1/subject-clause');
    expect(init?.method).toBe('PUT');
    expect(init?.headers).toEqual({ 'Content-Type': 'application/json' });
    expect(init?.body).toBe(JSON.stringify({ clause: 'a goblin' }));
  });

  it('sends no body and no Content-Type on a read', async () => {
    const fetch = stubFetch(() => Promise.resolve(json([], 200)));

    await api.projects();

    const [, init] = fetch.mock.calls[0]!;
    expect(init?.method).toBe('GET');
    expect(init?.headers).toBeUndefined();
    expect(init?.body).toBeUndefined();
  });

  it('sends an archive raw, as application/zip, never as a form (§G.9)', async () => {
    const fetch = stubFetch(() => Promise.resolve(json({ folder: 'donjon-2' }, 201)));
    const archive = new File(['PK'], 'donjon.zip');

    await api.importArchive('Donjon 2', archive);

    const [path, init] = fetch.mock.calls[0]!;
    expect(path).toBe('/api/projects/import?name=Donjon%202');
    expect(init?.headers).toEqual({ 'Content-Type': 'application/zip' });
    expect(init?.body).toBe(archive);
  });

  it('encodes every segment of a stored image path (DEC-088)', () => {
    expect(api.imageUrl('donjon', 'images/abc-front.png')).toBe('/api/projects/donjon/images/abc-front.png');
    expect(api.imageUrl('donjon', 'images/a b#1.png')).toBe('/api/projects/donjon/images/a%20b%231.png');
  });

  it('asks for the PDF inline only for the preview', () => {
    expect(api.sheetUrl('donjon', 'fr', true)).toBe('/api/projects/donjon/sheet.pdf?culture=fr&disposition=inline');
    expect(api.sheetUrl('donjon', 'en', false)).toBe('/api/projects/donjon/sheet.pdf?culture=en');
  });
});

describe('the name of a downloaded file', () => {
  const withDisposition = (value: string | null) =>
    new Response(null, { headers: value === null ? {} : { 'Content-Disposition': value } });

  it('reads a plain file name', () => {
    expect(fileNameOf(withDisposition('attachment; filename=donjon-backup.zip'), 'fallback.zip')).toBe('donjon-backup.zip');
  });

  it('reads a quoted file name', () => {
    expect(fileNameOf(withDisposition('attachment; filename="donjon backup.zip"'), 'fallback.zip')).toBe('donjon backup.zip');
  });

  it('reads an encoded file name', () => {
    expect(fileNameOf(withDisposition("attachment; filename*=UTF-8''donjon%20%C3%A9t%C3%A9.zip"), 'fallback.zip')).toBe('donjon été.zip');
  });

  it('falls back when the header is missing', () => {
    expect(fileNameOf(withDisposition(null), 'fallback.zip')).toBe('fallback.zip');
  });
});
