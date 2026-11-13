const env = (import.meta as ImportMeta & { env: Record<string, string> }).env;

export const API_BASE = (env.VITE_API_BASE_URL || (env.DEV ? '/api' : 'https://astrohealth.runasp.net/api')).replace(/\/+$/, '');
// A separate API origin also hosts the hubs unless explicitly overridden.
export const HUB_BASE = (env.VITE_HUB_BASE_URL || `${API_BASE.replace(/\/api$/i, '')}/hubs`).replace(/\/+$/, '');
