import { useRef, useState, useCallback, useEffect } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import type { Category } from '@/api/client';

interface CategoryFilterSliderProps {
  categories: Category[];
  selectedCategoryId?: number;
  onSelect: (categoryId: number | null) => void;
}

function pillClass(active: boolean) {
  return `shrink-0 px-4 py-2 rounded-full text-sm font-semibold transition-all whitespace-nowrap ${
    active
      ? 'bg-brand-600 text-white shadow-md shadow-brand-500/30'
      : 'glass-card text-slate-600 hover:border-brand-300'
  }`;
}

export function CategoryFilterSlider({
  categories,
  selectedCategoryId,
  onSelect,
}: CategoryFilterSliderProps) {
  const trackRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);

  const totalCount = categories.reduce((sum, c) => sum + c.toyCount, 0);

  const updateScrollState = useCallback(() => {
    const el = trackRef.current;
    if (!el) return;
    const { scrollLeft, scrollWidth, clientWidth } = el;
    setCanScrollLeft(scrollLeft > 4);
    setCanScrollRight(scrollLeft + clientWidth < scrollWidth - 4);
  }, []);

  useEffect(() => {
    updateScrollState();
    const el = trackRef.current;
    if (!el) return;

    el.addEventListener('scroll', updateScrollState, { passive: true });
    window.addEventListener('resize', updateScrollState);
    return () => {
      el.removeEventListener('scroll', updateScrollState);
      window.removeEventListener('resize', updateScrollState);
    };
  }, [categories, updateScrollState]);

  const scrollBy = (direction: 'left' | 'right') => {
    const el = trackRef.current;
    if (!el) return;
    const amount = Math.max(el.clientWidth * 0.6, 200);
    el.scrollBy({ left: direction === 'left' ? -amount : amount, behavior: 'smooth' });
  };

  return (
    <div className="relative group/slider mb-8">
      {canScrollLeft && (
        <button
          type="button"
          onClick={() => scrollBy('left')}
          aria-label="Previous categories"
          className="hidden sm:flex absolute left-0 top-1/2 -translate-y-1/2 z-10 w-9 h-9 items-center justify-center rounded-full bg-white/95 shadow-md border border-slate-200 text-slate-700 hover:bg-brand-50 hover:text-brand-600 hover:border-brand-200 transition-all -translate-x-1/2"
        >
          <ChevronLeft className="w-4 h-4" />
        </button>
      )}

      {canScrollRight && (
        <button
          type="button"
          onClick={() => scrollBy('right')}
          aria-label="Next categories"
          className="hidden sm:flex absolute right-0 top-1/2 -translate-y-1/2 z-10 w-9 h-9 items-center justify-center rounded-full bg-white/95 shadow-md border border-slate-200 text-slate-700 hover:bg-brand-50 hover:text-brand-600 hover:border-brand-200 transition-all translate-x-1/2"
        >
          <ChevronRight className="w-4 h-4" />
        </button>
      )}

      <div
        ref={trackRef}
        className="flex gap-2 overflow-x-auto pb-1 scroll-smooth scrollbar-hide touch-pan-x"
        style={{ WebkitOverflowScrolling: 'touch' }}
      >
        <button
          type="button"
          onClick={() => onSelect(null)}
          className={pillClass(!selectedCategoryId)}
        >
          All{totalCount > 0 && ` (${totalCount})`}
        </button>
        {categories.map((cat) => (
          <button
            key={cat.id}
            type="button"
            onClick={() => onSelect(cat.id)}
            className={pillClass(selectedCategoryId === cat.id)}
          >
            {cat.name} ({cat.toyCount})
          </button>
        ))}
      </div>

      {canScrollRight && (
        <div
          className="pointer-events-none absolute right-0 top-0 bottom-1 w-10 bg-gradient-to-l from-[#fff7fb] to-transparent sm:from-white/80"
          aria-hidden
        />
      )}
    </div>
  );
}

export function CategoryFilterSliderSkeleton() {
  return (
    <div className="flex gap-2 overflow-hidden mb-8">
      {Array.from({ length: 6 }).map((_, i) => (
        <div key={i} className="shrink-0 h-9 w-28 skeleton rounded-full" />
      ))}
    </div>
  );
}
