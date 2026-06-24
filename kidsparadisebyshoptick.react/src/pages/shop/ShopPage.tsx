import { useEffect, useMemo, useRef, useState } from 'react';
import { useInfiniteQuery, useQuery } from '@tanstack/react-query';
import { useSearchParams, Link, useLocation } from 'react-router-dom';
import { Search, Filter, X, Loader2 } from 'lucide-react';
import { api } from '@/api/client';
import { ToyCard, ToyCardSkeleton } from '@/components/shop/ToyCard';
import { CategoryFilterSlider, CategoryFilterSliderSkeleton } from '@/components/shop/CategoryFilterSlider';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Input';
import { useSiteImages } from '@/hooks/useSiteImages';
import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO } from '@/lib/seo';
import {
  hasActiveShopFilters,
  parseShopSearchParams,
  filtersToSearchParams,
} from '@/lib/shopFilters';
import { clearScrollForPath, getScrollKey, migrateListScrollSnapshot, scrollToTop } from '@/lib/scroll';
import { useShopFiltersStore } from '@/store/shopFilters';
import { useScrollRestore } from '@/hooks/useScrollRestore';

export function ShopPage() {
  const { get } = useSiteImages();
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();
  const syncFromSearchParams = useShopFiltersStore((s) => s.syncFromSearchParams);
  const resetFiltersStore = useShopFiltersStore((s) => s.resetFilters);
  const restoredRef = useRef(false);
  const loadMoreRef = useRef<HTMLDivElement>(null);

  const urlFilters = useMemo(() => parseShopSearchParams(searchParams), [searchParams]);
  const search = urlFilters.search;
  const categoryId = urlFilters.categoryId;
  const saleFilter = searchParams.get('onSale');
  const onSale = urlFilters.onSale;
  const sort = urlFilters.sort;

  const [searchInput, setSearchInput] = useState(search);
  const [showFilters, setShowFilters] = useState(() => hasActiveShopFilters(urlFilters));

  useEffect(() => {
    setSearchInput(search);
  }, [search]);

  useEffect(() => {
    if (restoredRef.current) return;
    restoredRef.current = true;

    if (hasActiveShopFilters(urlFilters)) {
      syncFromSearchParams(searchParams);
      setShowFilters(true);
      return;
    }

    const stored = useShopFiltersStore.getState().filters;
    if (hasActiveShopFilters(stored)) {
      const fromKey = getScrollKey(location.pathname, location.search);
      const nextParams = filtersToSearchParams(stored);
      const toKey = getScrollKey('/shop', nextParams.toString() ? `?${nextParams.toString()}` : '');
      migrateListScrollSnapshot(fromKey, toKey);
      setSearchParams(nextParams, { replace: true });
      setShowFilters(true);
    }
  }, [searchParams, setSearchParams, syncFromSearchParams, urlFilters, location.pathname, location.search]);

  useEffect(() => {
    if (!restoredRef.current) return;
    syncFromSearchParams(searchParams);
    if (hasActiveShopFilters(parseShopSearchParams(searchParams))) {
      setShowFilters(true);
    }
  }, [searchParams, syncFromSearchParams]);

  const { data: categoriesData, isLoading: loadingCategories } = useQuery({
    queryKey: ['categories'],
    queryFn: () => api.getCategories({ page: 1, pageSize: 100 }),
  });
  const categories = categoriesData?.items;

  const {
    data,
    isLoading,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
  } = useInfiniteQuery({
    queryKey: ['toys', categoryId, search, saleFilter, sort],
    queryFn: ({ pageParam }) => api.getToys({
      categoryId,
      search: search || undefined,
      onSale,
      sort: sort !== 'newest' ? sort : undefined,
      page: pageParam,
    }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      const totalPages = Math.ceil(lastPage.totalCount / lastPage.pageSize);
      return lastPage.page < totalPages ? lastPage.page + 1 : undefined;
    },
    gcTime: 30 * 60 * 1000,
    staleTime: 2 * 60 * 1000,
  });

  const toys = useMemo(() => data?.pages.flatMap((page) => page.items) ?? [], [data]);
  const totalCount = data?.pages[0]?.totalCount ?? 0;
  const hasActiveFilters = hasActiveShopFilters(urlFilters);

  useEffect(() => {
    const target = loadMoreRef.current;
    if (!target || !hasNextPage || isFetchingNextPage) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting) fetchNextPage();
      },
      { rootMargin: '240px' },
    );

    observer.observe(target);
    return () => observer.disconnect();
  }, [hasNextPage, isFetchingNextPage, fetchNextPage]);

  useScrollRestore({
    ready: !isLoading && toys.length > 0,
    items: toys,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
  });

  const updateParams = (updates: Record<string, string | null>) => {
    const next = new URLSearchParams(searchParams);
    Object.entries(updates).forEach(([key, value]) => {
      if (value) next.set(key, value);
      else next.delete(key);
    });
    setSearchParams(next);
  };

  const clearFilters = () => {
    setSearchInput('');
    resetFiltersStore();
    setSearchParams({});
    setShowFilters(false);
    clearScrollForPath('/shop');
    scrollToTop();
  };

  const categoryFilterValue = categoryId ? String(categoryId) : 'All';

  const categoryOptions = useMemo(
    () => [
      { value: 'All', label: 'All Categories' },
      ...(categories?.map((c) => ({ value: String(c.id), label: c.name })) ?? []),
    ],
    [categories]
  );

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateParams({ search: searchInput.trim() || null });
  };

  return (
    <div>
      <SeoHead
        title={PAGE_SEO.shop.title}
        description={PAGE_SEO.shop.description}
        path={PAGE_SEO.shop.path}
      />
      <div className="relative h-40 md:h-52 overflow-hidden">
        <img
          src={get('shop_header')}
          alt=""
          className="w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-r from-brand-800/90 to-brand-600/60 flex items-center">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 w-full">
            <h1 className="text-3xl md:text-4xl font-extrabold text-white drop-shadow">Shop All Toys</h1>
            <p className="text-white/85 mt-1">{totalCount} unique items available</p>
          </div>
        </div>
        <div
          className="absolute inset-0 opacity-[0.06] pointer-events-none"
          style={{ backgroundImage: 'url(/watermark.svg)', backgroundRepeat: 'repeat', backgroundSize: '140px' }}
        />
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 py-8">
        <div className="bg-white rounded-2xl border border-slate-200 p-4 mb-6 shadow-sm">
          <form onSubmit={handleSearchSubmit} className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
              <input
                type="search"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="Search by toy name..."
                className="w-full pl-10 pr-4 py-3 rounded-xl border border-slate-200 text-base md:text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
              />
            </div>
            <Button type="submit" className="shrink-0 hidden sm:inline-flex">Search</Button>
            <button
              type="button"
              onClick={() => setShowFilters((v) => !v)}
              className={`shrink-0 px-3 py-2 rounded-xl border flex items-center gap-1.5 text-sm font-medium ${
                showFilters || hasActiveFilters ? 'border-brand-400 bg-brand-50 text-brand-700' : 'border-slate-200 text-slate-600'
              }`}
            >
              <Filter className="w-4 h-4" />
              <span className="hidden sm:inline">Filters</span>
            </button>
          </form>

          {(showFilters || hasActiveFilters) && (
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mt-4 pt-4 border-t border-slate-100">
              <Select
                label="Category"
                options={categoryOptions}
                value={categoryFilterValue}
                onChange={(e) => updateParams({ categoryId: e.target.value === 'All' ? null : e.target.value })}
              />
              <Select
                label="Sale"
                options={[
                  { value: 'all', label: 'All Prices' },
                  { value: 'sale', label: 'On Sale Only' },
                  { value: 'regular', label: 'Regular Prices' },
                ]}
                value={saleFilter === 'true' ? 'sale' : saleFilter === 'false' ? 'regular' : 'all'}
                onChange={(e) => {
                  const value = e.target.value;
                  updateParams({
                    onSale: value === 'sale' ? 'true' : value === 'regular' ? 'false' : null,
                  });
                }}
              />
              <Select
                label="Sort By"
                options={[
                  { value: 'newest', label: 'Newest First' },
                  { value: 'name', label: 'Name (A-Z)' },
                  { value: 'price-low', label: 'Price: Low to High' },
                  { value: 'price-high', label: 'Price: High to Low' },
                ]}
                value={sort}
                onChange={(e) => updateParams({ sort: e.target.value === 'newest' ? null : e.target.value })}
              />
            </div>
          )}

          <div className="flex flex-wrap items-center justify-between gap-2 mt-3">
            <p className="text-sm text-slate-500">
              Showing{' '}
              <span className="font-semibold text-slate-700">{toys.length}</span>
              {totalCount > toys.length && (
                <> of <span className="font-semibold text-slate-700">{totalCount}</span></>
              )}{' '}
              toys
            </p>
            {hasActiveFilters && (
              <button type="button" onClick={clearFilters} className="text-sm text-brand-600 font-medium flex items-center gap-1 hover:underline">
                <X className="w-3.5 h-3.5" /> Clear filters
              </button>
            )}
          </div>
        </div>

        {loadingCategories ? (
          <CategoryFilterSliderSkeleton />
        ) : categories && categories.length > 0 ? (
          <CategoryFilterSlider
            categories={categories}
            selectedCategoryId={categoryId}
            onSelect={(id) => updateParams({ categoryId: id ? String(id) : null })}
          />
        ) : null}

        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 md:gap-6">
          {isLoading
            ? Array.from({ length: 8 }).map((_, i) => <ToyCardSkeleton key={i} />)
            : toys.map((toy) => <ToyCard key={toy.id} toy={toy} listItemCount={toys.length} />)}
        </div>

        {!isLoading && toys.length === 0 && (
          <div className="text-center py-20 glass-card rounded-3xl">
            <div className="text-6xl mb-4">🧸</div>
            <h3 className="text-xl font-bold text-slate-700">
              {hasActiveFilters ? 'No toys match your filters' : 'No toys yet'}
            </h3>
            <p className="text-slate-500 mt-2">
              {hasActiveFilters ? 'Try changing your search or filters.' : 'New toys will be added soon. Check back later!'}
            </p>
            {hasActiveFilters ? (
              <Button variant="ghost" className="mt-4" onClick={clearFilters}>Clear filters</Button>
            ) : (
              <Link to="/" className="inline-block mt-4 text-brand-600 font-semibold hover:underline">← Back to Home</Link>
            )}
          </div>
        )}

        <div ref={loadMoreRef} className="h-1" aria-hidden />

        {isFetchingNextPage && (
          <div className="flex flex-col items-center gap-3 py-8 text-slate-500">
            <Loader2 className="w-6 h-6 animate-spin text-brand-600" />
            <p className="text-sm">Loading more toys...</p>
          </div>
        )}

        {!isLoading && toys.length > 0 && !hasNextPage && (
          <p className="text-center text-sm text-slate-500 py-8">You&apos;ve seen all toys</p>
        )}
      </div>
    </div>
  );
}
