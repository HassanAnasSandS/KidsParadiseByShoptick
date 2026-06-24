import { Button } from '@/components/ui/Button';

interface PaginationBarProps {
  page: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  className?: string;
}

export function PaginationBar({ page, totalCount, pageSize, onPageChange, className = '' }: PaginationBarProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  if (totalCount <= pageSize) return null;

  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  return (
    <div className={`flex flex-col sm:flex-row items-center justify-between gap-3 pt-4 ${className}`}>
      <p className="text-sm text-slate-500">
        Showing {from}–{to} of {totalCount}
      </p>
      <div className="flex items-center gap-2">
        <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
          Previous
        </Button>
        <span className="text-sm text-slate-600 px-2">
          Page {page} of {totalPages}
        </span>
        <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
          Next
        </Button>
      </div>
    </div>
  );
}
