import { useEffect, useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSearchParams, Link } from 'react-router-dom';
import { Search, Filter, X } from 'lucide-react';
import { api } from '@/api/client';
import { ToyCard, ToyCardSkeleton } from '@/components/shop/ToyCard';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Input';

export function ShopPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const search = searchParams.get('search') || '';
  const categoryId = searchParams.get('categoryId') ? Number(searchParams.get('categoryId')) : undefined;
  const saleFilter = searchParams.get('onSale');
  const onSale = saleFilter === 'true' ? true : saleFilter === 'false' ? false : undefined;
  const sort = searchParams.get('sort') || 'newest';
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState(search);
  const [showFilters, setShowFilters] = useState(false);

  useEffect(() => {
    setSearchInput(search);
  }, [search]);

  const { data: categories } = useQuery({ queryKey: ['categories'], queryFn: api.getCategories });
  const { data, isLoading } = useQuery({
    queryKey: ['toys', categoryId, search, saleFilter, sort, page],
    queryFn: () => api.getToys({
      categoryId,
      search: search || undefined,
      onSale,
      sort: sort !== 'newest' ? sort : undefined,
      page,
    }),
  });

  const totalPages = data ? Math.ceil(data.totalCount / data.pageSize) : 1;

  const hasActiveFilters =
    search.trim() !== '' ||
    !!categoryId ||
    saleFilter !== null ||
    sort !== 'newest';

  const updateParams = (updates: Record<string, string | null>) => {
    const next = new URLSearchParams(searchParams);
    Object.entries(updates).forEach(([key, value]) => {
      if (value) next.set(key, value);
      else next.delete(key);
    });
    setSearchParams(next);
    setPage(1);
  };

  const clearFilters = () => {
    setSearchInput('');
    setSearchParams({});
    setPage(1);
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
      <div className="relative h-40 md:h-52 overflow-hidden">
        <img
          src="/hero/slide-1.jpg"
          alt=""
          className="w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-r from-brand-800/90 to-brand-600/60 flex items-center">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 w-full">
            <h1 className="text-3xl md:text-4xl font-extrabold text-white drop-shadow">Shop All Toys</h1>
            <p className="text-white/85 mt-1">{data?.totalCount ?? 0} unique items available</p>
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
              Showing <span className="font-semibold text-slate-700">{data?.totalCount ?? 0}</span> toys
            </p>
            {hasActiveFilters && (
              <button type="button" onClick={clearFilters} className="text-sm text-brand-600 font-medium flex items-center gap-1 hover:underline">
                <X className="w-3.5 h-3.5" /> Clear filters
              </button>
            )}
          </div>
        </div>

        <div className="flex flex-wrap gap-2 mb-8">
          <button
            type="button"
            onClick={() => updateParams({ categoryId: null })}
            className={`px-4 py-2 rounded-full text-sm font-semibold transition-all ${!categoryId ? 'bg-brand-600 text-white shadow-md shadow-brand-500/30' : 'glass-card text-slate-600 hover:border-brand-300'}`}
          >
            All
          </button>
          {categories?.map((cat) => (
            <button
              key={cat.id}
              type="button"
              onClick={() => updateParams({ categoryId: String(cat.id) })}
              className={`px-4 py-2 rounded-full text-sm font-semibold transition-all ${categoryId === cat.id ? 'bg-brand-600 text-white shadow-md shadow-brand-500/30' : 'glass-card text-slate-600 hover:border-brand-300'}`}
            >
              {cat.name}
            </button>
          ))}
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 md:gap-6">
          {isLoading
            ? Array.from({ length: 8 }).map((_, i) => <ToyCardSkeleton key={i} />)
            : data?.items.map((toy) => <ToyCard key={toy.id} toy={toy} />)}
        </div>

        {!isLoading && data?.items.length === 0 && (
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

        {totalPages > 1 && (
          <div className="flex justify-center gap-2 mt-8">
            <Button variant="outline" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
            <span className="flex items-center px-4 text-sm text-slate-600">Page {page} of {totalPages}</span>
            <Button variant="outline" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Next</Button>
          </div>
        )}
      </div>
    </div>
  );
}
