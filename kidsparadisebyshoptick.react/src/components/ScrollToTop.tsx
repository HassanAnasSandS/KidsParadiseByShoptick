import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import {
  getScrollKey,
  hasSavedListScroll,
  hydrateScrollCache,
  saveScrollPosition,
  scrollToHash,
  scrollToTop,
} from '@/lib/scroll';

/**
 * Saves scroll per route. Scrolls to top only on fresh visits without saved list state.
 */
export function ScrollToTop() {
  const location = useLocation();

  useEffect(() => {
    hydrateScrollCache();
    if ('scrollRestoration' in history) {
      history.scrollRestoration = 'manual';
    }
  }, []);

  useEffect(() => {
    const key = getScrollKey(location.pathname, location.search);

    let ticking = false;
    const onScroll = () => {
      if (ticking) return;
      ticking = true;
      requestAnimationFrame(() => {
        saveScrollPosition(key, window.scrollY);
        ticking = false;
      });
    };

    window.addEventListener('scroll', onScroll, { passive: true });
    return () => {
      window.removeEventListener('scroll', onScroll);
      saveScrollPosition(key, window.scrollY);
    };
  }, [location.pathname, location.search]);

  useEffect(() => {
    if (location.hash) {
      requestAnimationFrame(() => scrollToHash(location.hash));
      return;
    }

    if (location.state?.scrollToTop) {
      scrollToTop();
      return;
    }

    if (hasSavedListScroll(location.pathname, location.search)) return;

    scrollToTop();
  }, [location.pathname, location.search, location.hash, location.key, location.state]);

  return null;
}
