import { useEffect, useRef } from 'react';

export function useInfiniteScroll(onLoadMore: () => void, enabled: boolean) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const target = ref.current;
    if (!target || !enabled) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting) onLoadMore();
      },
      { rootMargin: '240px' },
    );

    observer.observe(target);
    return () => observer.disconnect();
  }, [enabled, onLoadMore]);

  return ref;
}
