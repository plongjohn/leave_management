const lockName = 'lm-token-refresh';

/**
 * Runs the task while holding a lock shared by all tabs of the app (Web Locks API).
 * Every refresh replaces the refresh cookie, and the backend treats a reused token as stolen
 * (ADR 0004) – so two tabs must never refresh with the same cookie at the same time. A tab that
 * waits for the lock sends the cookie the other tab has just received.
 *
 * Browsers without the API (and the test environment) run the task directly.
 */
export function withRefreshLock<T>(task: () => Promise<T>): Promise<T> {
  const locks = globalThis.navigator?.locks as LockManager | undefined;
  return locks ? locks.request(lockName, task) : task();
}
