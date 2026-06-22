import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { scrollToTop } from '@/lib/scroll';

/** Scroll to top on every route / query change (SPA navigation). */
export function ScrollToTop() {
  const location = useLocation();

  useEffect(() => {
    if ('scrollRestoration' in history) {
      history.scrollRestoration = 'manual';
    }
  }, []);

  useEffect(() => {
    scrollToTop();
  }, [location.pathname, location.search, location.hash, location.key]);

  return null;
}
