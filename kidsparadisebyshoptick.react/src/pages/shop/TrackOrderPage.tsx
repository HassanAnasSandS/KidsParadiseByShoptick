import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { ChevronDown, ChevronUp, Package, Search, Star } from 'lucide-react';
import { api } from '@/api/client';
import type { Order, PendingReview } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { PaginationBar } from '@/components/ui/PaginationBar';
import { ReviewForm } from '@/components/shop/ReviewForm';
import { OrderWhatsAppButton } from '@/components/shop/OrderWhatsAppButton';
import { formatPrice, placeholderImage } from '@/lib/utils';
import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO } from '@/lib/seo';

const statusColors: Record<string, string> = {
  Pending: 'bg-yellow-100 text-yellow-700',
  Confirmed: 'bg-blue-100 text-blue-700',
  Shipped: 'bg-purple-100 text-purple-700',
  Delivered: 'bg-green-100 text-green-700',
  Cancelled: 'bg-red-100 text-red-700',
};

function OrderItemLine({ item }: { item: Order['items'][number] }) {
  return (
    <div className="flex items-center gap-3">
      <img
        src={item.imageUrl || placeholderImage(item.toyName)}
        alt=""
        className="w-12 h-12 rounded-lg object-cover shrink-0 bg-slate-100"
      />
      <div className="min-w-0 flex-1">
        <p className="text-slate-700 font-medium truncate">{item.toyName}</p>
        <p className="text-xs text-slate-500">{formatPrice(item.price)}</p>
      </div>
      <span className="font-semibold text-slate-800 shrink-0">{formatPrice(item.price)}</span>
    </div>
  );
}

function OrderBillSummary({ order }: { order: Order }) {
  const discount = order.discountAmount ?? 0;
  const advance = order.advanceAmount ?? 0;
  const payableAfterDiscount = order.total - discount;
  const showPayment = order.status === 'Confirmed' || order.status === 'Shipped' || order.status === 'Delivered';

  return (
    <div className="bg-slate-50 rounded-xl p-4 text-sm space-y-3 border border-slate-100">
      <p className="font-semibold text-slate-800">Bill Summary</p>

      <div className="space-y-2">
        {order.items.map((item) => (
          <OrderItemLine key={item.toyId} item={item} />
        ))}
      </div>

      <div className="border-t border-slate-200 pt-3 space-y-2">
        <div className="flex justify-between">
          <span className="text-slate-500">Products Subtotal</span>
          <span className="font-semibold">{formatPrice(order.subTotal)}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-slate-500">Delivery Charges</span>
          <span className="font-semibold">{formatPrice(order.deliveryCharge)}</span>
        </div>
        <div className="flex justify-between border-t border-slate-200 pt-2">
          <span className="text-slate-700 font-medium">Order Total</span>
          <span className="font-bold text-slate-900">{formatPrice(order.total)}</span>
        </div>
        {discount > 0 && (
          <div className="flex justify-between">
            <span className="text-slate-500">Discount</span>
            <span className="font-semibold text-orange-700">-{formatPrice(discount)}</span>
          </div>
        )}
        {discount > 0 && (
          <div className="flex justify-between">
            <span className="text-slate-700 font-medium">Payable Amount</span>
            <span className="font-bold text-slate-900">{formatPrice(payableAfterDiscount)}</span>
          </div>
        )}
        {showPayment && advance > 0 && (
          <div className="flex justify-between">
            <span className="text-slate-500">Advance Paid</span>
            <span className="font-semibold text-green-700">{formatPrice(advance)}</span>
          </div>
        )}
        {showPayment && (
          <div className="flex justify-between border-t border-slate-200 pt-2">
            <span className="text-slate-800 font-semibold">Balance Due</span>
            <span className="font-bold text-brand-600">{formatPrice(order.balanceAmount)}</span>
          </div>
        )}
      </div>
    </div>
  );
}

function OrderCard({
  order,
  whatsapp,
  pendingItems,
  onReviewSuccess,
}: {
  order: Order;
  whatsapp: string;
  pendingItems: PendingReview[];
  onReviewSuccess: () => void;
}) {
  const [expanded, setExpanded] = useState(false);
  const [showReview, setShowReview] = useState(false);
  const canReview = order.status === 'Delivered' && pendingItems.length > 0;

  return (
    <div className="bg-white rounded-2xl border border-slate-100 overflow-hidden">
      <button
        type="button"
        onClick={() => setExpanded((v) => !v)}
        className="w-full p-4 text-left hover:bg-slate-50/80 transition-colors"
      >
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <p className="font-bold text-brand-600 break-all">{order.orderNumber}</p>
              <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${statusColors[order.status] || 'bg-slate-100'}`}>
                {order.status}
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-1">
              {new Date(order.createdAt).toLocaleString()} · {order.city} · {order.items.length} item{order.items.length !== 1 ? 's' : ''}
            </p>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <OrderWhatsAppButton
              order={order}
              onClick={(e) => e.stopPropagation()}
            />
            <p className="font-bold text-slate-800">{formatPrice(order.total)}</p>
            {expanded ? <ChevronUp className="w-5 h-5 text-slate-400" /> : <ChevronDown className="w-5 h-5 text-slate-400" />}
          </div>
        </div>
      </button>

      {expanded && (
        <div className="px-4 pb-4 border-t border-slate-100 pt-4 space-y-4">
          {canReview && (
            <div className="flex justify-end">
              <Button
                size="sm"
                variant={showReview ? 'outline' : 'primary'}
                onClick={(e) => {
                  e.stopPropagation();
                  setShowReview((v) => !v);
                }}
              >
                <Star className="w-4 h-4" />
                {showReview ? 'Hide Review' : 'Add Review'}
              </Button>
            </div>
          )}

          {showReview && canReview && (
            <div className="space-y-3">
              <p className="text-sm text-green-700 bg-green-50 border border-green-100 rounded-xl px-4 py-2.5">
                {pendingItems.length} item{pendingItems.length !== 1 ? 's' : ''} ready for review
              </p>
              {pendingItems.map((item) => (
                <ReviewForm
                  key={`${item.orderId}-${item.toyId}`}
                  item={item}
                  whatsapp={whatsapp}
                  onSuccess={onReviewSuccess}
                />
              ))}
            </div>
          )}
          {order.trackingNumber && (
            <div className="bg-purple-50 rounded-xl p-3 text-sm">
              <span className="text-slate-500">Tracking Number:</span>{' '}
              <span className="font-semibold text-purple-700">{order.trackingNumber}</span>
            </div>
          )}

          <OrderBillSummary order={order} />

          <div className="flex justify-end">
            <OrderWhatsAppButton order={order} showLabel />
          </div>

          <div className="grid grid-cols-2 gap-3 text-sm">
            <div><span className="text-slate-500">City:</span> {order.city}</div>
            <div><span className="text-slate-500">WhatsApp:</span> {order.whatsapp}</div>
            <div className="col-span-2 break-words"><span className="text-slate-500">Address:</span> {order.address}</div>
          </div>

          <div className="border-t pt-4 space-y-3">
            <p className="text-sm font-semibold text-slate-700">Ordered Items</p>
            {order.items.map((item) => (
              <OrderItemLine key={item.toyId} item={item} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export function TrackOrderPage() {
  const queryClient = useQueryClient();
  const [whatsapp, setWhatsapp] = useState('');
  const [submittedWhatsapp, setSubmittedWhatsapp] = useState<string | null>(null);
  const [page, setPage] = useState(1);

  const { data, isLoading, isFetching, refetch } = useQuery({
    queryKey: ['track', submittedWhatsapp, page],
    queryFn: () => api.trackOrdersByWhatsapp(submittedWhatsapp!, page),
    enabled: !!submittedWhatsapp,
    retry: false,
  });
  const orders = data?.items ?? [];

  const { data: pendingReviewsData } = useQuery({
    queryKey: ['pending-reviews', submittedWhatsapp],
    queryFn: () => api.getPendingReviews(submittedWhatsapp!, 1, 100),
    enabled: !!submittedWhatsapp && orders.some((o) => o.status === 'Delivered'),
  });
  const pendingReviews = pendingReviewsData?.items ?? [];

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = whatsapp.trim();
    if (!trimmed) return;

    if (trimmed === submittedWhatsapp) {
      void refetch();
      void queryClient.invalidateQueries({ queryKey: ['pending-reviews', trimmed] });
    } else {
      setPage(1);
      setSubmittedWhatsapp(trimmed);
    }
  };

  const refreshReviews = () => {
    queryClient.invalidateQueries({ queryKey: ['pending-reviews', submittedWhatsapp] });
    queryClient.invalidateQueries({ queryKey: ['reviews'] });
  };

  return (
    <div className="max-w-2xl mx-auto px-4 sm:px-6 py-8">
      <SeoHead
        title={PAGE_SEO.trackOrder.title}
        description={PAGE_SEO.trackOrder.description}
        path={PAGE_SEO.trackOrder.path}
      />
      <div className="text-center mb-8">
        <Package className="w-12 h-12 text-brand-600 mx-auto mb-3" />
        <h1 className="text-3xl font-bold text-slate-800">My Orders</h1>
        <p className="text-slate-500 mt-1">Enter your WhatsApp number to view all your orders</p>
      </div>

      <form onSubmit={handleSubmit} className="bg-white rounded-2xl p-6 border border-slate-100 space-y-4">
        <Input
          label="WhatsApp Number"
          required
          value={whatsapp}
          onChange={(e) => setWhatsapp(e.target.value)}
          placeholder="e.g. 03221234567"
        />
        <Button type="submit" className="w-full" disabled={isFetching && !!submittedWhatsapp}>
          <Search className="w-4 h-4" />
          {isFetching && submittedWhatsapp ? 'Refreshing...' : 'View My Orders'}
        </Button>
      </form>

      {(isLoading || isFetching) && submittedWhatsapp && (
        <div className="mt-6 bg-white rounded-2xl p-6 border skeleton h-48" />
      )}

      {submittedWhatsapp && !isLoading && !isFetching && orders.length === 0 && (
        <div className="mt-6 text-center text-slate-500 bg-slate-50 rounded-xl p-6">
          No orders found for <span className="font-medium text-slate-700">{submittedWhatsapp}</span>
        </div>
      )}

      {orders.length > 0 && (
        <div className="mt-6 space-y-3 animate-fade-in">
          <p className="text-sm text-slate-500">
            {data?.totalCount ?? orders.length} order{(data?.totalCount ?? orders.length) !== 1 ? 's' : ''} found for <span className="font-medium text-slate-700">{submittedWhatsapp}</span>
          </p>
          {orders.map((order) => (
            <OrderCard
              key={order.id}
              order={order}
              whatsapp={submittedWhatsapp!}
              pendingItems={pendingReviews?.filter((p) => p.orderId === order.id) ?? []}
              onReviewSuccess={refreshReviews}
            />
          ))}
          {data && (
            <PaginationBar
              page={data.page}
              totalCount={data.totalCount}
              pageSize={data.pageSize}
              onPageChange={setPage}
            />
          )}
        </div>
      )}
    </div>
  );
}
