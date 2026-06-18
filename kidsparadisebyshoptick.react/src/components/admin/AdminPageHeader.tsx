import type { ReactNode } from 'react';
import { useEffect } from 'react';
import { X } from 'lucide-react';
import { Button } from '@/components/ui/Button';

interface AdminPageHeaderProps {
  title: string;
  action?: ReactNode;
}

export function AdminPageHeader({ title, action }: AdminPageHeaderProps) {
  return (
    <div className="mb-4 md:mb-6">
      <div className="hidden md:flex flex-row items-center justify-between gap-3">
        <h1 className="text-2xl font-bold text-slate-800">{title}</h1>
        {action && <div>{action}</div>}
      </div>
      {action && (
        <div className="md:hidden fixed bottom-[4.5rem] right-4 z-40">
          {action}
        </div>
      )}
    </div>
  );
}

interface AdminFormCardProps {
  title: string;
  children: ReactNode;
  onCancel: () => void;
  onSubmit: () => void;
  submitLabel: string;
  submitDisabled?: boolean;
}

function FormBody({ title, children, onCancel, onSubmit, submitLabel, submitDisabled }: AdminFormCardProps) {
  return (
    <>
      <h2 className="hidden md:block font-semibold text-slate-800 mb-4 text-lg">{title}</h2>
      {children}
      <div className="flex flex-col gap-2 mt-5 sticky bottom-0 bg-white pt-3 border-t border-slate-100 md:border-0 md:static md:pt-0">
        <Button onClick={onSubmit} disabled={submitDisabled} className="w-full">
          {submitLabel}
        </Button>
        <Button variant="ghost" onClick={onCancel} className="w-full">Cancel</Button>
      </div>
    </>
  );
}

export function AdminFormCard(props: AdminFormCardProps) {
  const { onCancel } = props;

  useEffect(() => {
    const mq = window.matchMedia('(max-width: 767px)');
    if (!mq.matches) return;
    document.body.style.overflow = 'hidden';
    return () => { document.body.style.overflow = ''; };
  }, []);

  return (
    <>
      {/* Mobile full-screen sheet */}
      <div className="md:hidden fixed inset-0 z-[60] flex flex-col">
        <button
          type="button"
          className="absolute inset-0 bg-black/50"
          onClick={onCancel}
          aria-label="Close form"
        />
        <div className="relative mt-auto bg-white rounded-t-3xl shadow-2xl max-h-[90dvh] flex flex-col animate-fade-in">
          <div className="flex items-center justify-between px-4 py-3 border-b border-slate-100 shrink-0">
            <span className="font-semibold text-slate-800">{props.title}</span>
            <button type="button" onClick={onCancel} className="p-2 rounded-full hover:bg-slate-100">
              <X className="w-5 h-5 text-slate-600" />
            </button>
          </div>
          <div className="overflow-y-auto overscroll-contain px-4 py-4 pb-6 flex-1">
            <FormBody {...props} />
          </div>
        </div>
      </div>

      {/* Desktop inline card */}
      <div className="hidden md:block bg-white rounded-2xl p-6 border border-slate-200 mb-6 shadow-sm">
        <FormBody {...props} />
      </div>
    </>
  );
}
