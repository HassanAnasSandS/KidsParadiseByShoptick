import { useCallback, useMemo } from 'react';
import { useInfiniteQuery, useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { ArrowRight, Truck, Shield, Star, Sparkles, Gift, Loader2 } from 'lucide-react';
import { api } from '@/api/client';
import { ToyCard, ToyCardSkeleton } from '@/components/shop/ToyCard';
import { CategorySlider, CategorySliderSkeleton } from '@/components/shop/CategorySlider';
import { HeroSlider } from '@/components/shop/HeroSlider';
import { Button } from '@/components/ui/Button';
import { PAYMENT_POLICY } from '@/lib/utils';
import { useSiteImages } from '@/hooks/useSiteImages';
import { useInfiniteScroll } from '@/hooks/useInfiniteScroll';
import { useScrollRestore } from '@/hooks/useScrollRestore';
import { useShopPath } from '@/store/shopFilters';
import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO, buildOrganizationJsonLd, buildWebSiteJsonLd } from '@/lib/seo';

export function HomePage() {
  const { get } = useSiteImages();
  const shopPath = useShopPath();

  const { data: categoriesData, isLoading: loadingCategories } = useQuery({
    queryKey: ['categories'],
    queryFn: () => api.getCategories({ page: 1, pageSize: 100 }),
  });
  const categories = categoriesData?.items ?? [];
  const totalCategories = categories.length;

  const {
    data: productData,
    isLoading: loadingProducts,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
  } = useInfiniteQuery({
    queryKey: ['home-toys'],
    queryFn: ({ pageParam }) => api.getToys({ page: pageParam }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      const totalPages = Math.ceil(lastPage.totalCount / lastPage.pageSize);
      return lastPage.page < totalPages ? lastPage.page + 1 : undefined;
    },
    gcTime: 30 * 60 * 1000,
    staleTime: 2 * 60 * 1000,
  });

  const products = useMemo(
    () => productData?.pages.flatMap((page) => page.items) ?? [],
    [productData],
  );
  const totalProducts = productData?.pages[0]?.totalCount ?? 0;

  const loadMoreProducts = useCallback(() => {
    if (hasNextPage && !isFetchingNextPage) fetchNextPage();
  }, [hasNextPage, isFetchingNextPage, fetchNextPage]);

  const productsScrollRef = useInfiniteScroll(loadMoreProducts, !!hasNextPage && !isFetchingNextPage);

  useScrollRestore({
    ready: !loadingProducts && products.length > 0,
    items: products,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
  });

  return (
    <div>
      <SeoHead
        description={PAGE_SEO.home.description}
        path={PAGE_SEO.home.path}
        jsonLd={[buildOrganizationJsonLd(), buildWebSiteJsonLd()]}
      />
      <HeroSlider />

      <section className="max-w-7xl mx-auto px-4 sm:px-6 py-10">
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 -mt-2">
          {[
            { icon: Truck, title: 'Fast Delivery', desc: 'Karachi Rs.300 | Others Rs.400', color: 'bg-blue-50 text-blue-600' },
            { icon: Shield, title: 'Unique Items', desc: 'Each toy available once only', color: 'bg-emerald-50 text-emerald-600' },
            { icon: Star, title: 'Easy Payment', desc: '10% advance · balance on delivery', color: 'bg-amber-50 text-amber-600' },
          ].map(({ icon: Icon, title, desc, color }) => (
            <div key={title} className="glass-card rounded-2xl p-5 shadow-sm flex items-center gap-4 hover:shadow-md transition-shadow">
              <div className={`w-14 h-14 rounded-2xl flex items-center justify-center shrink-0 ${color}`}>
                <Icon className="w-7 h-7" />
              </div>
              <div>
                <h3 className="font-bold text-slate-800">{title}</h3>
                <p className="text-sm text-slate-500 mt-0.5">{desc}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-4 sm:px-6 py-6">
        <div className="grid md:grid-cols-2 gap-4">
          <div className="relative rounded-3xl overflow-hidden h-48 md:h-56 group">
            <img
              src={get('banner_new_arrivals')}
              alt=""
              className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
            />
            <div className="absolute inset-0 bg-gradient-to-r from-pink-600/80 to-transparent flex items-center p-8">
              <div>
                <Sparkles className="w-8 h-8 text-white mb-2" />
                <h3 className="text-2xl font-bold text-white">New Arrivals</h3>
                <p className="text-white/90 text-sm mt-1">Fresh toys added regularly</p>
                <Link to={shopPath} className="inline-block mt-3 text-sm font-semibold text-white underline">Shop now →</Link>
              </div>
            </div>
          </div>
          <div className="relative rounded-3xl overflow-hidden h-48 md:h-56 group">
            <img
              src={get('banner_perfect_gifts')}
              alt=""
              className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
            />
            <div className="absolute inset-0 bg-gradient-to-r from-brand-700/80 to-transparent flex items-center p-8">
              <div>
                <Gift className="w-8 h-8 text-white mb-2" />
                <h3 className="text-2xl font-bold text-white">Perfect Gifts</h3>
                <p className="text-white/90 text-sm mt-1">Make every birthday special</p>
                <Link to={shopPath} className="inline-block mt-3 text-sm font-semibold text-white underline">Find gifts →</Link>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-4 sm:px-6 py-12">
        <div className="flex items-center justify-between mb-2">
          <h2 className="text-2xl md:text-3xl font-bold text-slate-800 section-title">Shop by Category</h2>
          <Link to={shopPath} className="text-brand-600 text-sm font-semibold hover:underline flex items-center gap-1">
            View All <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
        {totalCategories > 0 && (
          <p className="text-sm text-slate-500 mb-6">
            <span className="font-semibold text-slate-700">{totalCategories}</span> categories — swipe to explore
          </p>
        )}

        {loadingCategories ? (
          <CategorySliderSkeleton />
        ) : categories.length > 0 ? (
          <CategorySlider categories={categories} />
        ) : (
          <div className="text-center py-16 glass-card rounded-3xl">
            <div className="text-5xl mb-3">📦</div>
            <p className="text-slate-600 font-medium">Categories coming soon!</p>
            <p className="text-slate-400 text-sm mt-1">Check back shortly for amazing toys.</p>
          </div>
        )}
      </section>

      <section className="py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="flex items-center justify-between mb-2">
            <h2 className="text-2xl md:text-3xl font-bold text-slate-800 section-title">Latest Toys</h2>
            <Link to={shopPath} className="text-brand-600 text-sm font-semibold hover:underline flex items-center gap-1">
              See All <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
          {totalProducts > 0 && (
            <p className="text-sm text-slate-500 mb-6">
              Showing <span className="font-semibold text-slate-700">{products.length}</span>
              {products.length < totalProducts && (
                <> of <span className="font-semibold text-slate-700">{totalProducts}</span></>
              )}{' '}
              toys
            </p>
          )}

          {loadingProducts ? (
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 md:gap-6">
              {Array.from({ length: 8 }).map((_, i) => <ToyCardSkeleton key={i} />)}
            </div>
          ) : products.length > 0 ? (
            <>
              <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 md:gap-6">
                {products.map((toy) => <ToyCard key={toy.id} toy={toy} listItemCount={products.length} />)}
              </div>
              <div ref={productsScrollRef} className="h-1" aria-hidden />
              {isFetchingNextPage && (
                <div className="flex flex-col items-center gap-2 py-8 text-slate-500">
                  <Loader2 className="w-6 h-6 animate-spin text-brand-600" />
                  <p className="text-sm">Loading more toys...</p>
                </div>
              )}
              {!hasNextPage && products.length > 12 && (
                <p className="text-center text-sm text-slate-500 py-6">You&apos;ve seen all latest toys</p>
              )}
            </>
          ) : (
            <div className="text-center py-16 glass-card rounded-3xl">
              <div className="text-5xl mb-3">🧸</div>
              <p className="text-slate-600 font-medium">No toys listed yet</p>
              <p className="text-slate-400 text-sm mt-1">New arrivals will appear here soon.</p>
            </div>
          )}
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-4 sm:px-6 pb-16">
        <div className="relative rounded-3xl overflow-hidden bg-gradient-to-r from-brand-600 to-brand-700 p-8 md:p-12 text-center text-white shadow-xl">
          <div
            className="absolute inset-0 opacity-10 pointer-events-none"
            style={{ backgroundImage: 'url(/watermark.svg)', backgroundRepeat: 'repeat', backgroundSize: '150px' }}
          />
          <h2 className="text-2xl md:text-3xl font-bold relative z-10">Ready to make your child smile?</h2>
          <p className="text-brand-100 mt-2 relative z-10 max-w-md mx-auto">
            Browse our unique collection. {PAYMENT_POLICY}.
          </p>
          <Link to={shopPath} className="inline-block mt-6 relative z-10">
            <Button size="lg" className="bg-white text-brand-600 hover:bg-brand-50 shadow-lg">
              Start Shopping <ArrowRight className="w-4 h-4" />
            </Button>
          </Link>
        </div>
      </section>
    </div>
  );
}
