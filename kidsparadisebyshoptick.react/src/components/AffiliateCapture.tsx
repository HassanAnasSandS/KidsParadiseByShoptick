import { useLayoutEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  captureAffiliateFromSearch,
  getAffiliateCode,
} from '@/store/affiliate';

function ensureAffInUrl(pathname: string, search: string, hash: string): string | null {
  const code = getAffiliateCode();
  if (!code) return null;

  const params = new URLSearchParams(search.startsWith('?') ? search.slice(1) : search);
  if (params.has('aff') || params.has('ref')) return null;

  params.set('aff', code);
  const qs = params.toString();
  return `${pathname}?${qs}${hash || ''}`;
}

/** Silently captures ?aff= / ?ref= and keeps it on the URL after navigation — no UI. */
export function AffiliateCapture() {
  const location = useLocation();
  const navigate = useNavigate();

  // Sync before paint so address bar does not flash a bare /shop or /.
  // Skip partner ledger — that page uses ?code= for lookup; injecting ?aff= confuses partners.
  useLayoutEffect(() => {
    if (location.pathname.startsWith('/partner')) return;

    captureAffiliateFromSearch(location.search);
    captureAffiliateFromSearch(window.location.search);

    const next = ensureAffInUrl(location.pathname, location.search, location.hash);
    if (!next) return;

    // Must keep location.state — Order Now passes buyNow via state; dropping it empties checkout.
    navigate(next, { replace: true, state: location.state });
  }, [location.pathname, location.search, location.hash, location.state, navigate]);

  return null;
}
