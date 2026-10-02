import { useCallback, useMemo, useState } from 'react';
import { useInfiniteQuery, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, MessageSquare } from 'lucide-react';
import { api } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { PaginationBar } from '@/components/ui/PaginationBar';
import { ReviewForm, StarRatingDisplay } from '@/components/shop/ReviewForm';
import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO } from '@/lib/seo';
import { ImageLightbox } from '@/components/shop/ImageLightbox';
import { useInfiniteScroll } from '@/hooks/useInfiniteScroll';

const REVIEWS_PAGE_SIZE = 12;

export function ReviewsPage() {
  const queryClient = useQueryClient();
  const [whatsapp, setWhatsapp] = useState('');
  const [lookupWhatsapp, setLookupWhatsapp] = useState('');
  const [pendingPage, setPendingPage] = useState(1);
  const [lightbox, setLightbox] = useState<{ images: string[]; alt: string } | null>(null);

  const {
    data,
    isLoading,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
  } = useInfiniteQuery({
    queryKey: ['reviews'],
    queryFn: ({ pageParam }) => api.getAllReviews({ page: pageParam, pageSize: REVIEWS_PAGE_SIZE }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      const totalPages = Math.ceil(lastPage.totalCount / lastPage.pageSize);
      return lastPage.page < totalPages ? lastPage.page + 1 : undefined;
    },
  });

  const reviews = useMemo(() => data?.pages.flatMap((p) => p.items) ?? [], [data]);
  const totalCount = data?.pages[0]?.totalCount ?? 0;

  const loadMore = useCallback(() => {
    if (hasNextPage && !isFetchingNextPage) void fetchNextPage();
  }, [hasNextPage, isFetchingNextPage, fetchNextPage]);

  const loadMoreRef = useInfiniteScroll(loadMore, !!hasNextPage && !isFetchingNextPage);

  const { data: pendingData } = useQuery({
    queryKey: ['pending-reviews', lookupWhatsapp, pendingPage],
    queryFn: () => api.getPendingReviews(lookupWhatsapp, pendingPage),
    enabled: !!lookupWhatsapp,
  });
  const pending = pendingData?.items ?? [];

  const handleLookup = (e: React.FormEvent) => {
    e.preventDefault();
    if (whatsapp.trim()) {
      setPendingPage(1);
      setLookupWhatsapp(whatsapp.trim());
    }
  };

  const refreshPending = () => {
    queryClient.invalidateQueries({ queryKey: ['pending-reviews', lookupWhatsapp] });
    queryClient.invalidateQueries({ queryKey: ['reviews'] });
  };

  return (
    <div className="max-w-6xl mx-auto px-4 sm:px-6 py-8">
      <SeoHead
        title={PAGE_SEO.reviews.title}
        description={PAGE_SEO.reviews.description}
        path={PAGE_SEO.reviews.path}
      />
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-slate-800">Customer Reviews</h1>
        <p className="text-slate-500 mt-1">See what parents are saying about our toys</p>
      </div>

      {/* Write review - top */}
      <section className="mb-12 pb-10 border-b border-slate-200">
        <h2 className="text-2xl font-bold text-slate-800 mb-2">Write a Review</h2>
        <p className="text-sm text-slate-500 mb-6">
          Only customers with a <span className="font-medium text-slate-700">delivered order</span> can review — one review per order item, one time only.
        </p>

        <form onSubmit={handleLookup} className="flex flex-col sm:flex-row gap-3 mb-6 max-w-lg">
          <Input
            label="Your WhatsApp number"
            value={whatsapp}
            onChange={(e) => setWhatsapp(e.target.value)}
            placeholder="number used at checkout"
            className="flex-1"
          />
          <Button type="submit" className="sm:self-end">Check Orders</Button>
        </form>

        {lookupWhatsapp && pendingData && pending.length === 0 && (
          <p className="text-sm text-amber-600 bg-amber-50 border border-amber-100 rounded-xl px-4 py-3">
            No items ready to review. Reviews unlock after your order is delivered.
          </p>
        )}

        {pending.length > 0 && (
          <div className="space-y-4">
            <p className="text-sm font-medium text-green-700 bg-green-50 border border-green-100 rounded-xl px-4 py-2.5">
              {pendingData?.totalCount ?? pending.length} item{(pendingData?.totalCount ?? pending.length) > 1 ? 's' : ''} ready for review
            </p>
            {pending.map((item) => (
              <ReviewForm
                key={`${item.orderId}-${item.toyId}`}
                item={item}
                whatsapp={lookupWhatsapp}
                onSuccess={refreshPending}
              />
            ))}
            {pendingData && (
              <PaginationBar
                page={pendingData.page}
                totalCount={pendingData.totalCount}
                pageSize={pendingData.pageSize}
                onPageChange={setPendingPage}
              />
            )}
          </div>
        )}
      </section>

      {/* All reviews — server-side infinite scroll */}
      <section>
        <h2 className="text-xl font-semibold text-slate-800 mb-4">
          All reviews
          {totalCount > 0 && (
            <span className="text-sm font-normal text-slate-400 ml-2">({totalCount})</span>
          )}
        </h2>

        {isLoading ? (
          <div className="text-center py-12 text-slate-400">Loading reviews...</div>
        ) : reviews.length === 0 ? (
          <div className="bg-white rounded-2xl border p-10 text-center text-slate-500">
            <MessageSquare className="w-10 h-10 mx-auto mb-3 text-slate-300" />
            No reviews yet. Be the first after your order is delivered!
          </div>
        ) : (
          <>
            <div className="grid gap-4 md:grid-cols-2">
              {reviews.map((r) => (
                <div key={r.id} className="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-semibold text-slate-800">{r.reviewerName}</p>
                      <p className="text-sm text-brand-600 mt-0.5">{r.toyName}</p>
                      <p className="text-xs text-slate-400 mt-0.5">{new Date(r.createdAt).toLocaleDateString()}</p>
                    </div>
                    <StarRatingDisplay value={r.rating} size="sm" />
                  </div>
                  <p className="text-slate-600 mt-3 text-sm leading-relaxed">{r.comment}</p>
                  {r.imageUrl && (
                    <button
                      type="button"
                      onClick={() => setLightbox({ images: [r.imageUrl!], alt: r.toyName || 'Review image' })}
                      className="mt-3 block max-w-xs rounded-xl focus:outline-none focus:ring-2 focus:ring-brand-400"
                      aria-label="View review image"
                    >
                      <img
                        src={r.imageUrl}
                        alt=""
                        className="w-full rounded-xl border object-cover max-h-48 cursor-zoom-in hover:opacity-90 transition-opacity"
                      />
                    </button>
                  )}
                </div>
              ))}
            </div>

            <div ref={loadMoreRef} className="h-8" aria-hidden />

            {isFetchingNextPage && (
              <div className="flex items-center justify-center gap-2 py-6 text-slate-400 text-sm">
                <Loader2 className="w-4 h-4 animate-spin" />
                Loading more reviews…
              </div>
            )}

            {!hasNextPage && reviews.length > 0 && (
              <p className="text-center text-xs text-slate-400 py-4">You&apos;ve seen all reviews</p>
            )}
          </>
        )}
      </section>

      {lightbox && (
        <ImageLightbox
          images={lightbox.images}
          alt={lightbox.alt}
          onClose={() => setLightbox(null)}
        />
      )}
    </div>
  );
}
