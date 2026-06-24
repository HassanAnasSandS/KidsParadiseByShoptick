import { useEffect, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import {
  findListScrollSnapshot,
  getScrollKey,
  getListScrollSnapshot,
  registerScrollContentReadyHandler,
  restoreScrollWithContent,
  wait,
} from '@/lib/scroll';

interface UseScrollRestoreOptions {
  ready?: boolean;
  items?: { id: number }[];
  hasNextPage?: boolean;
  isFetchingNextPage?: boolean;
  fetchNextPage?: () => Promise<unknown>;
  page?: number;
  setPage?: (page: number) => void;
  isLoading?: boolean;
}

/**
 * Restores list scroll (and product anchor) when returning from a detail page.
 */
export function useScrollRestore({
  ready = true,
  items = [],
  hasNextPage,
  isFetchingNextPage,
  fetchNextPage,
  page,
  setPage,
  isLoading,
}: UseScrollRestoreOptions) {
  const location = useLocation();
  const key = getScrollKey(location.pathname, location.search);
  const restoredRef = useRef(false);
  const fetchStateRef = useRef({
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
    items,
    page,
    setPage,
    isLoading,
  });
  fetchStateRef.current = {
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
    items,
    page,
    setPage,
    isLoading,
  };

  useEffect(() => {
    restoredRef.current = false;
  }, [key]);

  useEffect(() => {
    return registerScrollContentReadyHandler(key, async (_routeKey, snapshot) => {
      const anchorId = snapshot.anchorProductId;
      const targetCount = snapshot.loadedItemCount ?? 0;
      const targetPage = snapshot.page;
      let attempts = 0;

      while (attempts < 120) {
        const {
          items: currentItems,
          hasNextPage: hasMore,
          isFetchingNextPage: fetching,
          fetchNextPage: fetch,
          page: currentPage,
          setPage: restorePage,
          isLoading: loading,
        } = fetchStateRef.current;

        if (targetPage && targetPage > 1 && restorePage && currentPage !== targetPage) {
          if (!loading) restorePage(targetPage);
          await wait(150);
          attempts += 1;
          continue;
        }

        const hasAnchor = !anchorId || currentItems.some((item) => item.id === anchorId);
        const hasEnoughItems = targetCount <= 0 || currentItems.length >= targetCount;
        const maxScroll = document.documentElement.scrollHeight - window.innerHeight;
        const needsMoreHeight = snapshot.scrollY > maxScroll + 48;

        if (hasAnchor && hasEnoughItems && !needsMoreHeight) break;

        if (fetch) {
          if (!hasMore) break;
          if (fetching) {
            await wait(100);
            attempts += 1;
            continue;
          }
          await fetch();
          attempts += 1;
          await wait(120);
          continue;
        }

        if (loading) {
          await wait(100);
          attempts += 1;
          continue;
        }

        break;
      }
    });
  }, [key]);

  useEffect(() => {
    if (!ready || restoredRef.current) return;
    if (location.state?.scrollToTop) return;

    const saved = getListScrollSnapshot(key) ?? findListScrollSnapshot(location.pathname);
    if (!saved) return;

    restoredRef.current = true;
    void restoreScrollWithContent(key);
  }, [ready, key, location.state, items.length, page]);
}
