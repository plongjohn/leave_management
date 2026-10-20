/**
 * Waits until pending promises and microtasks have run, e.g. a refresh that goes through the
 * Web Locks API before its HTTP request is sent.
 */
export function settle(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve));
}
