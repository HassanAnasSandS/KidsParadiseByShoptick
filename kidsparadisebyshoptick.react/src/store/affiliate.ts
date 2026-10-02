import { create } from 'zustand';
import { persist } from 'zustand/middleware';

const STORAGE_KEY = 'kids-paradise-aff';

interface AffiliateState {
  code: string | null;
  setCode: (code: string | null) => void;
}

function normalizeCode(code: string | null | undefined): string | null {
  const trimmed = code?.trim();
  // Match API/DB: codes are stored uppercase hex.
  return trimmed ? trimmed.toUpperCase() : null;
}

function readStoredCode(): string | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as { state?: { code?: string | null }; code?: string | null };
    return normalizeCode(parsed?.state?.code ?? parsed?.code ?? null);
  } catch {
    return null;
  }
}

function writeStoredCode(code: string): void {
  try {
    localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ state: { code: normalizeCode(code) }, version: 0 }),
    );
  } catch {
    // ignore quota / private mode
  }
}

/** Internal only — never surface affiliate codes in UI. */
export const useAffiliateStore = create<AffiliateState>()(
  persist(
    (set) => ({
      // Prefer any already-captured URL code written synchronously before React boots.
      code: readStoredCode(),
      setCode: (code) => {
        const next = normalizeCode(code);
        if (next) writeStoredCode(next);
        set({ code: next });
      },
    }),
    {
      name: STORAGE_KEY,
      // If URL capture set a code before rehydrate finishes, do not wipe it with null.
      merge: (persisted, current) => {
        const p = (persisted ?? {}) as Partial<AffiliateState>;
        return {
          ...current,
          ...p,
          code: normalizeCode(current.code) ?? normalizeCode(p.code) ?? null,
        };
      },
    },
  ),
);

/** Capture ?aff= / ?ref= and persist immediately (survives shop redirects). */
export function captureAffiliateFromSearch(search: string): void {
  const query = search.startsWith('?') ? search.slice(1) : search;
  const params = new URLSearchParams(query);
  const code = normalizeCode(params.get('aff') ?? params.get('ref'));
  if (!code) return;

  writeStoredCode(code);
  useAffiliateStore.setState({ code });
}

/** Always read at order-submit time (store + localStorage fallback). */
export function getAffiliateCode(): string | null {
  return normalizeCode(useAffiliateStore.getState().code) ?? readStoredCode();
}

/** Keep aff on shop filter URL rewrites so refresh/share still works. */
export function appendAffiliateToParams(params: URLSearchParams): URLSearchParams {
  const code = getAffiliateCode();
  if (code && !params.has('aff') && !params.has('ref')) {
    params.set('aff', code);
  }
  return params;
}

/** Append ?aff= to any internal path so nav links keep the partner URL visible. */
export function withAffiliatePath(path: string): string {
  const code = getAffiliateCode();
  if (!code) return path;

  const hashIndex = path.indexOf('#');
  const hash = hashIndex >= 0 ? path.slice(hashIndex) : '';
  const withoutHash = hashIndex >= 0 ? path.slice(0, hashIndex) : path;
  const qIndex = withoutHash.indexOf('?');
  const pathname = qIndex >= 0 ? withoutHash.slice(0, qIndex) : withoutHash;
  const params = new URLSearchParams(qIndex >= 0 ? withoutHash.slice(qIndex + 1) : '');
  if (!params.has('aff') && !params.has('ref')) params.set('aff', code);
  const qs = params.toString();
  return `${pathname}${qs ? `?${qs}` : ''}${hash}`;
}
