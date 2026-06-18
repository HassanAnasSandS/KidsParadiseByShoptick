import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { ChevronDown, ChevronUp, Package, Search, Star } from 'lucide-react';
import { api } from '@/api/client';
import type { Order, PendingReview } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { ReviewForm } from '@/components/shop/ReviewForm';
import { formatPrice, placeholderImage } from '@/lib/utils';

const statusColors: Record<string, string> = {
  Pending: 'bg-yellow-100 text-yellow-700',
  Confirmed: 'bg-blue-100 text-blue-700',
  Shipped: 'bg-purple-100 text-purple-700',
  Delivered: 'bg-green-100 text-green-700',
  Cancelled: 'bg-red-100 text-red-700',
};

function OrderCard({
  order,
  email,
  pendingItems,
  onReviewSuccess,
}: {
  order: Order;
  email: string;
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
                  email={email}
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

          {(order.status === 'Confirmed' || order.status === 'Shipped' || order.status === 'Delivered') &&
            order.advanceAmount != null && (
            <div className="bg-blue-50 rounded-xl p-4 text-sm space-y-2">
              <p className="font-semibold text-slate-700">Payment Details</p>
              <div className="flex justify-between">
                <span className="text-slate-500">Order Total</span>
                <span className="font-semibold">{formatPrice(order.total)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Advance Paid</span>
                <span className="font-semibold text-green-700">{formatPrice(order.advanceAmount)}</span>
              </div>
              <div className="flex justify-between border-t border-blue-100 pt-2">
                <span className="text-slate-700 font-medium">Balance Due</span>
                <span className="font-bold text-brand-600">{formatPrice(order.balanceAmount)}</span>
              </div>
            </div>
          )}

          <div className="grid grid-cols-2 gap-3 text-sm">
            <div><span className="text-slate-500">City:</span> {order.city}</div>
            <div><span className="text-slate-500">Phone:</span> {order.phone}</div>
            <div className="col-span-2 break-words"><span className="text-slate-500">Address:</span> {order.address}</div>
          </div>

          <div className="border-t pt-4 space-y-3">
            {order.items.map((item) => (
              <div key={item.toyId} className="flex items-center gap-3">
                <img src={item.imageUrl || placeholderImage(item.toyName)} alt="" className="w-12 h-12 rounded-lg object-cover" />
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-sm truncate">{item.toyName}</p>
                </div>
                <p className="font-semibold text-sm shrink-0">{formatPrice(item.price)}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export function TrackOrderPage() {
  const queryClient = useQueryClient();
  const [email, setEmail] = useState('');
  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null);

  const { data: orders, isLoading } = useQuery({
    queryKey: ['track', submittedEmail],
    queryFn: () => api.trackOrdersByEmail(submittedEmail!),
    enabled: !!submittedEmail,
    retry: false,
  });

  const { data: pendingReviews } = useQuery({
    queryKey: ['pending-reviews', submittedEmail],
    queryFn: () => api.getPendingReviews(submittedEmail!),
    enabled: !!submittedEmail && !!orders?.some((o) => o.status === 'Delivered'),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSubmittedEmail(email.trim().toLowerCase());
  };

  const refreshReviews = () => {
    queryClient.invalidateQueries({ queryKey: ['pending-reviews', submittedEmail] });
    queryClient.invalidateQueries({ queryKey: ['reviews'] });
  };

  return (
    <div className="max-w-2xl mx-auto px-4 sm:px-6 py-8">
      <div className="text-center mb-8">
        <Package className="w-12 h-12 text-brand-600 mx-auto mb-3" />
        <h1 className="text-3xl font-bold text-slate-800">My Orders</h1>
        <p className="text-slate-500 mt-1">Enter your email to view all your orders</p>
      </div>

      <form onSubmit={handleSubmit} className="bg-white rounded-2xl p-6 border border-slate-100 space-y-4">
        <Input
          label="Email"
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="your@email.com"
        />
        <Button type="submit" className="w-full"><Search className="w-4 h-4" /> View My Orders</Button>
      </form>

      {isLoading && submittedEmail && (
        <div className="mt-6 bg-white rounded-2xl p-6 border skeleton h-48" />
      )}

      {submittedEmail && !isLoading && orders?.length === 0 && (
        <div className="mt-6 text-center text-slate-500 bg-slate-50 rounded-xl p-6">
          No orders found for <span className="font-medium text-slate-700">{submittedEmail}</span>
        </div>
      )}

      {orders && orders.length > 0 && (
        <div className="mt-6 space-y-3 animate-fade-in">
          <p className="text-sm text-slate-500">
            {orders.length} order{orders.length !== 1 ? 's' : ''} found for <span className="font-medium text-slate-700">{submittedEmail}</span>
          </p>
          {orders.map((order) => (
            <OrderCard
              key={order.id}
              order={order}
              email={submittedEmail!}
              pendingItems={pendingReviews?.filter((p) => p.orderId === order.id) ?? []}
              onReviewSuccess={refreshReviews}
            />
          ))}
        </div>
      )}
    </div>
  );
}
