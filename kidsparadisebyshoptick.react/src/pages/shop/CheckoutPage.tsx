import { useState } from 'react';
import { useNavigate, Link, useLocation } from 'react-router-dom';
import { useCartStore, type CartItem } from '@/store/cart';
import { api } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input, Textarea } from '@/components/ui/Input';
import { formatPrice, getDeliveryCharge, PAYMENT_POLICY } from '@/lib/utils';

export function CheckoutPage() {
  const { items, clearCart } = useCartStore();
  const navigate = useNavigate();
  const location = useLocation();
  const buyNow = (location.state as { buyNow?: CartItem } | null)?.buyNow;
  const checkoutItems = buyNow ? [buyNow] : items;
  const checkoutSubTotal = () =>
    checkoutItems.reduce((sum, i) => sum + (i.salePrice ?? i.price), 0);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [form, setForm] = useState({ name: '', whatsapp: '', city: '', address: '' });

  const deliveryCharge = getDeliveryCharge(form.city);
  const total = checkoutSubTotal() + (form.city.trim() ? deliveryCharge : 0);

  if (checkoutItems.length === 0) {
    return (
      <div className="text-center py-20">
        <h2 className="text-xl font-semibold">Cart is empty</h2>
        <Link to="/shop" className="text-brand-600 mt-2 inline-block">Go shopping</Link>
      </div>
    );
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      const result = await api.placeOrder({
        ...form,
        toyIds: checkoutItems.map((i) => i.toyId),
      });
      if (!buyNow) clearCart();
      navigate(`/order-success/${result.orderNumber}`, { state: { total: result.total, deliveryCharge: result.deliveryCharge } });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Order failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 py-8">
      <h1 className="text-3xl font-bold text-slate-800 mb-2">Checkout</h1>
      {buyNow && (
        <p className="text-sm text-brand-600 bg-brand-50 border border-brand-100 rounded-xl px-4 py-2.5 mb-6">
          Ordering <span className="font-semibold">{buyNow.name}</span> only. Your cart is unchanged.
        </p>
      )}

      <div className="grid lg:grid-cols-5 gap-8">
        <form onSubmit={handleSubmit} className="lg:col-span-3 space-y-4">
          <div className="bg-white rounded-2xl p-6 border border-slate-100">
            <h2 className="font-semibold text-slate-800 mb-4">Contact & Delivery</h2>
            <div className="grid sm:grid-cols-2 gap-4">
              <Input label="Full Name *" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
              <Input label="WhatsApp *" required value={form.whatsapp} onChange={(e) => setForm({ ...form, whatsapp: e.target.value })} placeholder="e.g. 03221234567" />
              <Input label="City *" required value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} placeholder="e.g. Karachi" className="sm:col-span-2" />
            </div>
            <div className="mt-4">
              <Textarea label="Delivery Address *" required rows={3} value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
            </div>
            {form.city.trim() && (
              <p className="text-sm text-brand-600 mt-3 font-medium">
                Delivery charge: {formatPrice(deliveryCharge)}
                {form.city.trim().toLowerCase() === 'karachi' ? ' (Karachi rate)' : ' (Outside Karachi)'}
              </p>
            )}
          </div>

          {error && <p className="text-red-500 text-sm bg-red-50 p-3 rounded-xl">{error}</p>}

          <p className="text-sm text-slate-600 bg-amber-50 border border-amber-100 rounded-xl px-4 py-3">
            {PAYMENT_POLICY}. Balance amount is paid on delivery.
          </p>

          <Button type="submit" size="lg" className="w-full" disabled={loading}>
            {loading ? 'Placing Order...' : buyNow ? 'Place Order Now' : 'Place Order'}
          </Button>
        </form>

        <div className="lg:col-span-2">
          <div className="bg-white rounded-2xl p-6 border border-slate-100 sticky top-24">
            <h2 className="font-semibold text-slate-800 mb-4">Order Summary</h2>
            <div className="space-y-3 text-sm">
              {checkoutItems.map((item) => (
                <div key={item.toyId} className="flex justify-between">
                  <span className="text-slate-600">{item.name}</span>
                  <span className="font-medium">{formatPrice(item.salePrice ?? item.price)}</span>
                </div>
              ))}
            </div>
            <div className="border-t border-slate-100 mt-4 pt-4 space-y-2 text-sm">
              <div className="flex justify-between"><span>Subtotal</span><span>{formatPrice(checkoutSubTotal())}</span></div>
              <div className="flex justify-between"><span>Delivery</span><span>{form.city.trim() ? formatPrice(deliveryCharge) : '—'}</span></div>
              <div className="flex justify-between text-lg font-bold text-slate-800 pt-2 border-t">
                <span>Total</span><span>{form.city.trim() ? formatPrice(total) : formatPrice(checkoutSubTotal())}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
