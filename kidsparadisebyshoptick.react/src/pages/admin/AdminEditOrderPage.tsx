import { useEffect, useMemo, useState } from 'react';
import { useNavigate, Link, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Plus, Search, Trash2, ShoppingBag } from 'lucide-react';
import { api, effectivePrice, toyPrimaryImage, type ToyListItem } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input, Textarea } from '@/components/ui/Input';
import { formatPrice, getDeliveryCharge, placeholderImage } from '@/lib/utils';

function orderItemToToy(item: { toyId: number; toyName: string; price: number; imageUrl: string | null }): ToyListItem {
  return {
    id: item.toyId,
    name: item.toyName,
    price: item.price,
    salePrice: null,
    isSold: true,
    imageUrls: item.imageUrl ? [item.imageUrl] : [],
    categoryName: '',
    averageRating: null,
  };
}

export function AdminEditOrderPage() {
  const { id } = useParams<{ id: string }>();
  const orderId = Number(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [form, setForm] = useState({ name: '', whatsapp: '', city: '', address: '' });
  const [deliveryCharge, setDeliveryCharge] = useState(0);
  const [advance, setAdvance] = useState('');
  const [discount, setDiscount] = useState('');
  const [tracking, setTracking] = useState('');
  const [selectedToys, setSelectedToys] = useState<ToyListItem[]>([]);
  const [toySearch, setToySearch] = useState('');
  const [error, setError] = useState('');
  const [initialized, setInitialized] = useState(false);

  const { data: order, isLoading: orderLoading, error: orderError } = useQuery({
    queryKey: ['admin-order', orderId],
    queryFn: () => api.adminGetOrder(orderId),
    enabled: Number.isFinite(orderId) && orderId > 0,
  });

  const { data: toys, isLoading: toysLoading } = useQuery({
    queryKey: ['admin-toys'],
    queryFn: api.adminGetToys,
  });

  useEffect(() => {
    if (!order || initialized) return;

    setForm({
      name: order.customerName,
      whatsapp: order.whatsapp,
      city: order.city,
      address: order.address,
    });
    setDeliveryCharge(order.deliveryCharge);
    setAdvance(order.advanceAmount != null ? String(order.advanceAmount) : '');
    setDiscount(order.discountAmount != null ? String(order.discountAmount) : '');
    setTracking(order.trackingNumber ?? '');

    const mapped = order.items.map((item) => {
      const toy = toys?.find((t) => t.id === item.toyId);
      return toy ?? orderItemToToy(item);
    });
    setSelectedToys(mapped);
    setInitialized(true);
  }, [order, toys, initialized]);

  const isCancelled = order?.status === 'Cancelled';
  const isDelivered = order?.status === 'Delivered';
  const canEditToys = order && !isCancelled && !isDelivered;

  const availableToys = useMemo(() => {
    const selectedIds = new Set(selectedToys.map((t) => t.id));
    return (toys ?? [])
      .filter((t) => !selectedIds.has(t.id))
      .filter((t) => !t.isSold)
      .filter((t) => {
        const q = toySearch.trim().toLowerCase();
        if (!q) return true;
        return t.name.toLowerCase().includes(q) || t.categoryName.toLowerCase().includes(q);
      });
  }, [toys, selectedToys, toySearch]);

  const subTotal = selectedToys.reduce((sum, t) => sum + effectivePrice(t), 0);
  const total = subTotal + deliveryCharge;
  const advanceNum = advance.trim() === '' ? 0 : Number(advance);
  const discountNum = discount.trim() === '' ? 0 : Number(discount);
  const balance = total - (Number.isFinite(discountNum) ? discountNum : 0) - (Number.isFinite(advanceNum) ? advanceNum : 0);

  const updateMutation = useMutation({
    mutationFn: () =>
      api.adminUpdateOrder(orderId, {
        customerName: form.name.trim(),
        whatsapp: form.whatsapp.trim(),
        city: form.city.trim(),
        address: form.address.trim(),
        deliveryCharge,
        advanceAmount: advance.trim() === '' ? 0 : advanceNum,
        discountAmount: discount.trim() === '' ? 0 : discountNum,
        trackingNumber: tracking,
        toyIds: selectedToys.map((t) => t.id),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
      queryClient.invalidateQueries({ queryKey: ['admin-order', orderId] });
      queryClient.invalidateQueries({ queryKey: ['admin-toys'] });
      navigate('/admin/orders', { state: { editedOrderNumber: order?.orderNumber } });
    },
    onError: (err: Error) => setError(err.message),
  });

  const addToy = (toy: ToyListItem) => {
    setSelectedToys((prev) => [...prev, toy]);
    setError('');
  };

  const removeToy = (toyId: number) => {
    setSelectedToys((prev) => prev.filter((t) => t.id !== toyId));
  };

  const canSubmit =
    !isCancelled &&
    form.name.trim() &&
    form.whatsapp.trim() &&
    form.city.trim() &&
    form.address.trim() &&
    selectedToys.length > 0 &&
    deliveryCharge >= 0 &&
    (Number.isFinite(advanceNum) ? advanceNum : 0) >= 0 &&
    (Number.isFinite(discountNum) ? discountNum : 0) >= 0 &&
    (Number.isFinite(advanceNum) ? advanceNum : 0) + (Number.isFinite(discountNum) ? discountNum : 0) <= total;

  if (!Number.isFinite(orderId) || orderId <= 0) {
    return (
      <div className="p-8 text-center text-slate-500">
        Invalid order ID. <Link to="/admin/orders" className="text-brand-600 hover:underline">Back to orders</Link>
      </div>
    );
  }

  if (orderLoading) {
    return <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">Loading order...</div>;
  }

  if (orderError || !order) {
    return (
      <div className="p-8 text-center text-slate-500">
        Order not found. <Link to="/admin/orders" className="text-brand-600 hover:underline">Back to orders</Link>
      </div>
    );
  }

  if (isCancelled) {
    return (
      <div className="p-8 text-center">
        <p className="text-slate-600 mb-4">Cancelled orders cannot be edited.</p>
        <Link to="/admin/orders" className="text-brand-600 hover:underline">Back to orders</Link>
      </div>
    );
  }

  return (
    <div className="w-full max-w-full overflow-x-hidden">
      <div className="flex items-center gap-3 mb-4 md:mb-6">
        <Link
          to="/admin/orders"
          className="p-2 rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-50 shrink-0"
        >
          <ArrowLeft className="w-5 h-5" />
        </Link>
        <div className="min-w-0">
          <h1 className="text-xl md:text-2xl font-bold text-slate-800">Edit Order</h1>
          <p className="text-sm text-slate-500">
            {order.orderNumber} · <span className="font-medium">{order.status}</span>
          </p>
        </div>
      </div>

      <div className="grid lg:grid-cols-5 gap-6">
        <div className="lg:col-span-3 space-y-4">
          <div className="bg-white rounded-2xl p-4 md:p-6 border border-slate-200 shadow-sm">
            <h2 className="font-semibold text-slate-800 mb-4">Customer Details</h2>
            <div className="grid sm:grid-cols-2 gap-4">
              <Input label="Full Name *" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
              <Input label="WhatsApp *" required value={form.whatsapp} onChange={(e) => setForm({ ...form, whatsapp: e.target.value })} placeholder="e.g. 03221234567" />
              <Input label="City *" required value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} placeholder="e.g. Karachi" className="sm:col-span-2" />
            </div>
            <div className="mt-4">
              <Textarea label="Delivery Address *" required rows={3} value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
            </div>
          </div>

          <div className="bg-white rounded-2xl p-4 md:p-6 border border-slate-200 shadow-sm">
            <h2 className="font-semibold text-slate-800 mb-4">Charges &amp; Payment</h2>
            <div className="grid sm:grid-cols-2 gap-4">
              <div>
                <Input
                  label="Delivery Charge (Rs.) *"
                  type="number"
                  min={0}
                  value={String(deliveryCharge)}
                  onChange={(e) => setDeliveryCharge(Math.max(0, Number(e.target.value) || 0))}
                />
                {form.city.trim() && (
                  <button
                    type="button"
                    className="text-xs text-brand-600 mt-1 hover:underline"
                    onClick={() => setDeliveryCharge(getDeliveryCharge(form.city))}
                  >
                    Reset from city ({formatPrice(getDeliveryCharge(form.city))})
                  </button>
                )}
              </div>
              <Input
                label="Tracking Number"
                value={tracking}
                onChange={(e) => setTracking(e.target.value)}
                placeholder="Courier tracking"
              />
              <Input
                label="Advance Amount (Rs.)"
                type="number"
                min={0}
                max={total}
                value={advance}
                onChange={(e) => setAdvance(e.target.value)}
                placeholder="e.g. 500"
              />
              <Input
                label="Discount (Rs.)"
                type="number"
                min={0}
                max={total}
                value={discount}
                onChange={(e) => setDiscount(e.target.value)}
                placeholder="e.g. 200"
              />
            </div>
          </div>

          <div className="bg-white rounded-2xl p-4 md:p-6 border border-slate-200 shadow-sm">
            <div className="flex items-center justify-between gap-3 mb-4">
              <h2 className="font-semibold text-slate-800">Order Toys</h2>
              <span className="text-sm text-slate-500">{selectedToys.length} selected</span>
            </div>

            {isDelivered && (
              <p className="text-sm text-amber-700 bg-amber-50 border border-amber-100 rounded-xl p-3 mb-4">
                Delivered orders: customer, payment, and tracking can be updated — toys cannot be changed.
              </p>
            )}

            {selectedToys.length > 0 && (
              <div className="space-y-2 mb-4">
                {selectedToys.map((toy) => (
                  <div key={toy.id} className="flex items-center gap-3 p-3 rounded-xl bg-brand-50 border border-brand-100">
                    <img
                      src={toyPrimaryImage(toy) || placeholderImage(toy.name)}
                      alt=""
                      className="w-12 h-12 rounded-lg object-cover shrink-0"
                    />
                    <div className="min-w-0 flex-1">
                      <p className="font-medium text-slate-800 truncate">{toy.name}</p>
                      <p className="text-xs text-slate-500">{toy.categoryName || '—'}</p>
                    </div>
                    <p className="font-semibold text-brand-600 shrink-0">{formatPrice(effectivePrice(toy))}</p>
                    {canEditToys && (
                      <button
                        type="button"
                        onClick={() => removeToy(toy.id)}
                        className="p-2 text-red-500 hover:bg-red-50 rounded-lg shrink-0"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    )}
                  </div>
                ))}
              </div>
            )}

            {canEditToys && (
              <>
                <div className="relative mb-3">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                  <input
                    type="search"
                    value={toySearch}
                    onChange={(e) => setToySearch(e.target.value)}
                    placeholder="Search available toys..."
                    className="w-full pl-10 pr-4 py-3 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                  />
                </div>

                <div className="max-h-72 overflow-y-auto space-y-2 border border-slate-100 rounded-xl p-2">
                  {toysLoading ? (
                    <p className="text-center text-slate-400 py-8 text-sm">Loading toys...</p>
                  ) : availableToys.length === 0 ? (
                    <p className="text-center text-slate-400 py-8 text-sm">
                      {toySearch.trim() ? 'No matching toys' : 'No available toys to add'}
                    </p>
                  ) : (
                    availableToys.map((toy) => (
                      <button
                        key={toy.id}
                        type="button"
                        onClick={() => addToy(toy)}
                        className="w-full flex items-center gap-3 p-2.5 rounded-xl hover:bg-slate-50 text-left transition-colors"
                      >
                        <img
                          src={toyPrimaryImage(toy) || placeholderImage(toy.name)}
                          alt=""
                          className="w-11 h-11 rounded-lg object-cover shrink-0"
                        />
                        <div className="min-w-0 flex-1">
                          <p className="font-medium text-sm text-slate-800 truncate">{toy.name}</p>
                          <p className="text-xs text-slate-500">{toy.categoryName}</p>
                        </div>
                        <p className="text-sm font-semibold text-brand-600 shrink-0">{formatPrice(effectivePrice(toy))}</p>
                        <span className="p-1.5 rounded-lg bg-brand-100 text-brand-600 shrink-0">
                          <Plus className="w-4 h-4" />
                        </span>
                      </button>
                    ))
                  )}
                </div>
              </>
            )}
          </div>

          {error && <p className="text-red-500 text-sm bg-red-50 p-3 rounded-xl border border-red-100">{error}</p>}

          <Button
            size="lg"
            className="w-full lg:hidden"
            disabled={!canSubmit || updateMutation.isPending}
            onClick={() => updateMutation.mutate()}
          >
            {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
          </Button>
        </div>

        <div className="lg:col-span-2">
          <div className="bg-white rounded-2xl p-6 border border-slate-200 shadow-sm lg:sticky lg:top-24">
            <div className="flex items-center gap-2 mb-4">
              <ShoppingBag className="w-5 h-5 text-brand-600" />
              <h2 className="font-semibold text-slate-800">Order Summary</h2>
            </div>

            {selectedToys.length === 0 ? (
              <p className="text-sm text-slate-500 py-4">Order must have at least one toy.</p>
            ) : (
              <div className="space-y-2 text-sm mb-4">
                {selectedToys.map((toy) => (
                  <div key={toy.id} className="flex justify-between gap-2">
                    <span className="text-slate-600 truncate">{toy.name}</span>
                    <span className="font-medium shrink-0">{formatPrice(effectivePrice(toy))}</span>
                  </div>
                ))}
              </div>
            )}

            <div className="border-t border-slate-100 pt-4 space-y-2 text-sm">
              <div className="flex justify-between"><span>Subtotal</span><span>{formatPrice(subTotal)}</span></div>
              <div className="flex justify-between"><span>Delivery</span><span>{formatPrice(deliveryCharge)}</span></div>
              <div className="flex justify-between text-lg font-bold text-slate-800 pt-2 border-t">
                <span>Total</span>
                <span>{formatPrice(total)}</span>
              </div>
              {(discountNum > 0 || advanceNum > 0) && (
                <div className="pt-2 space-y-1 border-t">
                  {discountNum > 0 && (
                    <div className="flex justify-between text-orange-700">
                      <span>Discount</span>
                      <span>-{formatPrice(discountNum)}</span>
                    </div>
                  )}
                  {advanceNum > 0 && (
                    <div className="flex justify-between text-green-700">
                      <span>Advance</span>
                      <span>{formatPrice(advanceNum)}</span>
                    </div>
                  )}
                  <div className="flex justify-between font-bold text-brand-600">
                    <span>Balance</span>
                    <span>{formatPrice(balance)}</span>
                  </div>
                </div>
              )}
            </div>

            <Button
              size="lg"
              className="w-full mt-4 hidden lg:flex"
              disabled={!canSubmit || updateMutation.isPending}
              onClick={() => updateMutation.mutate()}
            >
              {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
