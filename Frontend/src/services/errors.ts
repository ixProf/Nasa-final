export function reportError(error: unknown) {
  const message = error instanceof Error ? error.message : String(error);
  window.dispatchEvent(new CustomEvent('app:error', { detail: message }));
}
