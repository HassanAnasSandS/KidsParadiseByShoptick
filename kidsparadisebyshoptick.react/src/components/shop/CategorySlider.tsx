import { useRef, useState, useCallback, useEffect } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import type { Category } from '@/api/client';
import { CategoryCard } from '@/components/shop/CategoryCard';

interface CategorySliderProps {
  categories: Category[];
}

export function CategorySlider({ categories }: CategorySliderProps) {
  const trackRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);

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
    const amount = Math.max(el.clientWidth * 0.75, 280);
    el.scrollBy({ left: direction === 'left' ? -amount : amount, behavior: 'smooth' });
  };

  return (
    <div className="relative group/slider">
      {canScrollLeft && (
        <button
          type="button"
          onClick={() => scrollBy('left')}
          aria-label="Previous categories"
          className="hidden sm:flex absolute left-0 top-1/2 -translate-y-1/2 z-10 w-10 h-10 items-center justify-center rounded-full bg-white/95 shadow-lg border border-slate-200 text-slate-700 hover:bg-brand-50 hover:text-brand-600 hover:border-brand-200 transition-all -translate-x-1/2"
        >
          <ChevronLeft className="w-5 h-5" />
        </button>
      )}

      {canScrollRight && (
        <button
          type="button"
          onClick={() => scrollBy('right')}
          aria-label="Next categories"
          className="hidden sm:flex absolute right-0 top-1/2 -translate-y-1/2 z-10 w-10 h-10 items-center justify-center rounded-full bg-white/95 shadow-lg border border-slate-200 text-slate-700 hover:bg-brand-50 hover:text-brand-600 hover:border-brand-200 transition-all translate-x-1/2"
        >
          <ChevronRight className="w-5 h-5" />
        </button>
      )}

      <div
        ref={trackRef}
        className="flex gap-4 overflow-x-auto pb-2 -mx-1 px-1 snap-x snap-mandatory scroll-smooth scrollbar-hide touch-pan-x"
        style={{ WebkitOverflowScrolling: 'touch' }}
      >
        {categories.map((cat, i) => (
          <div
            key={cat.id}
            className="shrink-0 w-[72vw] sm:w-[240px] md:w-[260px] lg:w-[280px] snap-start"
          >
            <CategoryCard category={cat} index={i} />
          </div>
        ))}
      </div>

      {canScrollRight && (
        <div
          className="pointer-events-none absolute right-0 top-0 bottom-2 w-12 bg-gradient-to-l from-[#fff7fb] to-transparent sm:from-white/90"
          aria-hidden
        />
      )}
    </div>
  );
}

export function CategorySliderSkeleton() {
  return (
    <div className="flex gap-4 overflow-hidden">
      {Array.from({ length: 4 }).map((_, i) => (
        <div key={i} className="shrink-0 w-[72vw] sm:w-[240px] aspect-[4/3] skeleton rounded-2xl" />
      ))}
    </div>
  );
}
