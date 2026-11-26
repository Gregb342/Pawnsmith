import { describe, expect, it } from 'vitest';

import { writeQueue } from './writeQueue';

// The writes of the interface run one after the other (SaveStatus): the
// second builds its body from the first's answer, or one change is lost.

/** A promise the test settles by hand. */
function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (error: unknown) => void;
  const promise = new Promise<T>((ok, ko) => {
    resolve = ok;
    reject = ko;
  });
  return { promise, resolve, reject };
}

describe('writeQueue', () => {
  it('starts a task only once the one before has answered', async () => {
    const enqueue = writeQueue();
    const first = deferred<string>();
    const started: string[] = [];

    const one = enqueue(() => {
      started.push('one');
      return first.promise;
    });
    const two = enqueue(() => {
      started.push('two');
      return Promise.resolve('second');
    });

    await Promise.resolve();
    expect(started).toEqual(['one']);

    first.resolve('first');
    expect(await one).toBe('first');
    expect(await two).toBe('second');
    expect(started).toEqual(['one', 'two']);
  });

  it('lets the second task read what the first one wrote', async () => {
    const enqueue = writeQueue();
    let saved = { details: '', weapon: '' };

    // Each task builds its body from `saved` when it runs, as the screens do.
    const write = (change: Partial<typeof saved>) =>
      enqueue(async () => {
        await Promise.resolve();
        saved = { ...saved, ...change };
        return saved;
      });

    await Promise.all([write({ details: 'one ear torn' }), write({ weapon: 'spear' })]);

    expect(saved).toEqual({ details: 'one ear torn', weapon: 'spear' });
  });

  it('runs the next task after one that failed', async () => {
    const enqueue = writeQueue();

    const failed = enqueue(() => Promise.reject(new Error('refused')));
    const next = enqueue(() => Promise.resolve('ran'));

    await expect(failed).rejects.toThrow('refused');
    expect(await next).toBe('ran');
  });
});
