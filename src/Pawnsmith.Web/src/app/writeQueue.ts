/**
 * A queue of writes: each task starts once the one before has settled.
 *
 * The interface writes whole objects — a blueprint, a project's settings —
 * built from what the API last answered. Two writes in flight would both
 * start from the same answer, and the second would undo the first. Queued,
 * each one builds its body when its turn comes (SaveStatus, DEC-111).
 *
 * A task that fails rejects its own promise and does not stop the queue:
 * the next one runs all the same.
 */
export function writeQueue(): <T>(task: () => Promise<T>) => Promise<T> {
  let tail: Promise<unknown> = Promise.resolve();

  return <T,>(task: () => Promise<T>): Promise<T> => {
    const next = tail.then(task);
    tail = next.catch(() => undefined);
    return next;
  };
}
