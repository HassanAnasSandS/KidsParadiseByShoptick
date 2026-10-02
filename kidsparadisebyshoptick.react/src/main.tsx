import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App';
import { ErrorBoundary } from '@/components/ErrorBoundary';
import { captureAffiliateFromSearch } from '@/store/affiliate';

// Capture affiliate token before React mounts / zustand rehydrates (avoids losing ?aff=).
captureAffiliateFromSearch(window.location.search);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary>
      <App />
    </ErrorBoundary>
  </StrictMode>
);
