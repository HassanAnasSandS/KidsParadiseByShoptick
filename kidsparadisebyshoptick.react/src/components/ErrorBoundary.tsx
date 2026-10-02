import { Component, type ErrorInfo, type ReactNode } from 'react';

type Props = { children: ReactNode };
type State = { error: Error | null };

/** Shows a recoverable message instead of a blank pink screen when React crashes. */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Kids Paradise UI crash:', error, info.componentStack);
  }

  render() {
    if (!this.state.error) return this.props.children;

    return (
      <div className="min-h-screen flex items-center justify-center px-4">
        <div className="max-w-md w-full bg-white border border-slate-200 rounded-2xl p-6 shadow-sm text-center">
          <h1 className="text-xl font-bold text-slate-800">Something went wrong</h1>
          <p className="text-sm text-slate-500 mt-2 break-words">{this.state.error.message}</p>
          <button
            type="button"
            className="mt-5 px-5 py-2.5 rounded-xl bg-brand-600 text-white text-sm font-semibold"
            onClick={() => {
              try {
                localStorage.removeItem('kids-paradise-aff');
              } catch {
                /* ignore */
              }
              window.location.href = '/';
            }}
          >
            Reload site
          </button>
        </div>
      </div>
    );
  }
}
