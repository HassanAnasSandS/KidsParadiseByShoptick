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
import { withAffiliatePath } from '@/store/affiliate';
import { resolveSiteColor } from '@/lib/siteImages';
import { SeoHead } from '@/components/seo/SeoHead';
import {
  PAGE_SEO,
  HOME_FAQS,
  buildFaqJsonLd,
  buildLocalBusinessJsonLd,
  buildOrganizationJsonLd,
  buildWebSiteJsonLd,
} from '@/lib/seo';
import { useDeliveryRates } from '@/hooks/useDeliveryRates';

export function HomePage() {
  const { getContent } = useSiteImages();
  const shopPath = useShopPath();
  const { label: deliveryLabel, rates } = useDeliveryRates();
  const homeFaqs = [
    {
      question: 'Do you deliver kids toys across Pakistan?',
      answer: `Yes. Kids Paradise by Shoptick delivers nationwide. Karachi delivery is Rs.${rates.karachi.toLocaleString('en-PK')} and other cities are Rs.${rates.otherCities.toLocaleString('en-PK')}. Pay 10% advance and the balance on delivery.`,
    },
    ...HOME_FAQS.slice(1),
  ];

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
        jsonLd={[
          buildOrganizationJsonLd(),
          buildWebSiteJsonLd(),
          buildLocalBusinessJsonLd(),
          buildFaqJsonLd(homeFaqs),
        ]}
      />
      <HeroSlider />

      <section className="max-w-7xl mx-auto px-4 sm:px-6 py-10">
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 -mt-2">
          {[
            { icon: Truck, title: 'Fast Delivery', desc: deliveryLabel, color: 'bg-blue-50 text-blue-600' },
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
          {([
            { key: 'banner_new_arrivals' as const, Icon: Sparkles, gradient: 'from-pink-600/80' },
            { key: 'banner_perfect_gifts' as const, Icon: Gift, gradient: 'from-brand-700/80' },
          ]).map(({ key, Icon, gradient }) => {
            const content = getContent(key);
            const href = withAffiliatePath(content.linkUrl?.trim() || shopPath);
            const title = content.title?.trim() || '';
            const subtitle = content.subtitle?.trim() || '';
            const cta = content.ctaText?.trim() || '';
            const titleColor = resolveSiteColor(content.titleColor);
            const subtitleColor = resolveSiteColor(content.subtitleColor, '#FFFFFFE6');
            const ctaColor = resolveSiteColor(content.ctaColor);
            return (
              <div key={key} className="relative rounded-3xl overflow-hidden h-48 md:h-56 group">
                <img
                  src={content.imageUrl}
                  alt=""
                  className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                />
                <div className={`absolute inset-0 bg-gradient-to-r ${gradient} to-transparent flex items-center p-5 sm:p-8`}>
                  {(title || subtitle || cta) ? (
                    <div>
                      <Icon className="w-8 h-8 mb-2" style={{ color: titleColor }} />
                      {title ? (
                        <h3 className="text-2xl font-bold" style={{ color: titleColor }}>{title}</h3>
                      ) : null}
                      {subtitle ? (
                        <p className="text-sm mt-1" style={{ color: subtitleColor }}>{subtitle}</p>
                      ) : null}
                      {cta ? (
                        <Link to={href} className="inline-block mt-3 text-sm font-semibold underline" style={{ color: ctaColor }}>
                          {cta}
                        </Link>
                      ) : null}
                    </div>
                  ) : null}
                </div>
              </div>
            );
          })}
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

      <section className="max-w-7xl mx-auto px-4 sm:px-6 pb-12">
        <h2 className="text-2xl md:text-3xl font-bold text-slate-800 section-title mb-6">
          Buying toys online in Pakistan — FAQs
        </h2>
        <div className="space-y-4">
          {homeFaqs.map((faq) => (
            <details
              key={faq.question}
              className="group glass-card rounded-2xl p-5 open:shadow-md"
            >
              <summary className="cursor-pointer font-semibold text-slate-800 list-none flex items-center justify-between gap-3">
                {faq.question}
                <span className="text-brand-500 text-lg group-open:rotate-45 transition-transform">+</span>
              </summary>
              <p className="mt-3 text-sm text-slate-600 leading-relaxed">{faq.answer}</p>
            </details>
          ))}
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
