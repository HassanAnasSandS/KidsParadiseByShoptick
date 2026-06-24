const SCROLL_STORAGE_KEY = 'kidsparadise-scroll-positions';
const MAX_RESTORE_ATTEMPTS = 80;

export interface ListScrollSnapshot {
  scrollY: number;
  anchorProductId?: number;
  loadedItemCount?: number;
  page?: number;
}

const memoryCache = new Map<string, ListScrollSnapshot>();

function loadFromStorage(): Record<string, ListScrollSnapshot> {
  try {
    return JSON.parse(sessionStorage.getItem(SCROLL_STORAGE_KEY) ?? '{}') as Record<string, ListScrollSnapshot>;
  } catch {
    return {};
  }
}

function persistToStorage() {
  try {
    sessionStorage.setItem(SCROLL_STORAGE_KEY, JSON.stringify(Object.fromEntries(memoryCache.entries())));
  } catch {
    // sessionStorage unavailable
  }
}

export function hydrateScrollCache() {
  if (memoryCache.size > 0) return;
  for (const [key, value] of Object.entries(loadFromStorage())) {
    if (value && typeof value.scrollY === 'number' && value.scrollY >= 0) {
      memoryCache.set(key, value);
    }
  }
}

export function getScrollKey(pathname: string, search: string): string {
  return `${pathname}${search}`;
}

export function saveListScrollSnapshot(key: string, snapshot: ListScrollSnapshot) {
  memoryCache.set(key, snapshot);
  persistToStorage();
}

export function getListScrollSnapshot(key: string): ListScrollSnapshot | undefined {
  hydrateScrollCache();
  return memoryCache.get(key);
}

export function migrateListScrollSnapshot(fromKey: string, toKey: string) {
  if (fromKey === toKey) return;
  hydrateScrollCache();
  const snapshot = memoryCache.get(fromKey);
  if (!snapshot || memoryCache.has(toKey)) return;
  memoryCache.set(toKey, snapshot);
  memoryCache.delete(fromKey);
  persistToStorage();
}

export function findListScrollSnapshot(pathname: string): ListScrollSnapshot | undefined {
  hydrateScrollCache();
  for (const [key, snapshot] of memoryCache.entries()) {
    if (key === pathname || key.startsWith(`${pathname}?`)) return snapshot;
  }
  return undefined;
}

export function hasSavedListScroll(pathname: string, search: string): boolean {
  const key = getScrollKey(pathname, search);
  return getListScrollSnapshot(key) !== undefined || findListScrollSnapshot(pathname) !== undefined;
}

export function saveScrollPosition(key: string, y: number) {
  const existing = getListScrollSnapshot(key);
  saveListScrollSnapshot(key, { ...existing, scrollY: y });
}

export function getScrollPosition(key: string): number | undefined {
  return getListScrollSnapshot(key)?.scrollY;
}

export function scrollToTop() {
  window.scrollTo(0, 0);
  document.documentElement.scrollTop = 0;
  document.body.scrollTop = 0;
}

export function scrollToProductAnchor(productId: number): boolean {
  const el = document.getElementById(`product-${productId}`);
  if (!el) return false;
  const top = Math.max(0, el.getBoundingClientRect().top + window.scrollY - 96);
  window.scrollTo(0, top);
  document.documentElement.scrollTop = top;
  document.body.scrollTop = top;
  return true;
}

export function restoreScrollPosition(key: string): boolean {
  const snapshot = getListScrollSnapshot(key) ?? findListScrollSnapshot(key.split('?')[0] ?? key);
  if (!snapshot) return false;

  if (snapshot.anchorProductId && scrollToProductAnchor(snapshot.anchorProductId)) {
    saveListScrollSnapshot(key, { ...snapshot, scrollY: window.scrollY });
    return true;
  }

  const y = snapshot.scrollY;
  let attempts = 0;

  const tryRestore = () => {
    const maxScroll = Math.max(0, document.documentElement.scrollHeight - window.innerHeight);
    const target = Math.min(y, maxScroll);
    window.scrollTo(0, target);
    document.documentElement.scrollTop = target;
    document.body.scrollTop = target;

    if (Math.abs(window.scrollY - target) < 4 || attempts >= MAX_RESTORE_ATTEMPTS) return;

    attempts += 1;
    requestAnimationFrame(tryRestore);
  };

  requestAnimationFrame(tryRestore);
  return true;
}

export function scrollToHash(hash: string) {
  const id = hash.replace(/^#/, '');
  if (!id) return;
  const el = document.getElementById(id);
  if (el) el.scrollIntoView({ block: 'start' });
  else scrollToTop();
}

export function clearScrollPosition(key: string) {
  memoryCache.delete(key);
  persistToStorage();
}

export function clearScrollForPath(pathname: string) {
  hydrateScrollCache();
  for (const key of [...memoryCache.keys()]) {
    if (key === pathname || key.startsWith(`${pathname}?`)) {
      memoryCache.delete(key);
    }
  }
  persistToStorage();
}

export type ScrollContentReadyHandler = (
  key: string,
  snapshot: ListScrollSnapshot,
) => Promise<void> | void;

const contentReadyHandlers = new Map<string, ScrollContentReadyHandler>();

export function registerScrollContentReadyHandler(
  key: string,
  handler: ScrollContentReadyHandler,
) {
  contentReadyHandlers.set(key, handler);
  return () => {
    contentReadyHandlers.delete(key);
  };
}

export async function restoreScrollWithContent(key: string): Promise<boolean> {
  const snapshot = getListScrollSnapshot(key) ?? findListScrollSnapshot(key.split('?')[0] ?? '/shop');
  if (!snapshot) return false;

  const handler = contentReadyHandlers.get(key);
  if (handler) {
    try {
      await handler(key, snapshot);
    } catch {
      // Continue with basic restore.
    }
  }

  await new Promise<void>((resolve) => {
    requestAnimationFrame(() => requestAnimationFrame(() => resolve()));
  });

  restoreScrollPosition(key);
  return true;
}

export function saveCurrentScroll(pathname: string, search: string, extra?: Partial<ListScrollSnapshot>) {
  const key = getScrollKey(pathname, search);
  const existing = getListScrollSnapshot(key);
  saveListScrollSnapshot(key, {
    scrollY: window.scrollY,
    ...existing,
    ...extra,
  });
}

export function saveProductListAnchor(
  pathname: string,
  search: string,
  productId: number,
  loadedItemCount: number,
  page?: number,
) {
  saveListScrollSnapshot(getScrollKey(pathname, search), {
    scrollY: window.scrollY,
    anchorProductId: productId,
    loadedItemCount,
    page,
  });
}

export async function wait(ms: number) {
  await new Promise<void>((resolve) => setTimeout(resolve, ms));
}
