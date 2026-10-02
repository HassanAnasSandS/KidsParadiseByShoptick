import { useState } from 'react';
import { useNavigate, Link, useLocation } from 'react-router-dom';
import { useCartStore, type CartItem } from '@/store/cart';
import { getAffiliateCode } from '@/store/affiliate';
import { api } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input, Textarea } from '@/components/ui/Input';
import { formatPrice, placeholderImage, PAYMENT_POLICY } from '@/lib/utils';
import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO } from '@/lib/seo';
import { useShopPath } from '@/store/shopFilters';
import { useDeliveryRates } from '@/hooks/useDeliveryRates';
import { ImageLightbox } from '@/components/shop/ImageLightbox';
import { getWhatsAppUrl } from '@/lib/whatsapp';
import { MessageCircle } from 'lucide-react';

function buildCheckoutInquiryMessage(
  form: { name: string; whatsapp: string; city: string; address: string },
  items: CartItem[],
  subTotal: number,
  deliveryCharge: number,
  total: number,
) {
  const itemLines = items
    .map((item, i) => `${i + 1}. ${item.name}\n   ${formatPrice(item.salePrice ?? item.price)}`)
    .join('\n');

  const lines = [
    'Hello Kids Paradise!',
    '',
    'I need help with checkout:',
    '',
  ];

  if (form.name.trim()) lines.push(`👤 Name: ${form.name.trim()}`);
  if (form.whatsapp.trim()) lines.push(`📱 WhatsApp: ${form.whatsapp.trim()}`);
  if (form.city.trim()) lines.push(`🏙️ City: ${form.city.trim()}`);
  if (form.address.trim()) lines.push(`📍 Address: ${form.address.trim()}`);

  lines.push('', '🛍️ Cart items:', itemLines, '');
  lines.push(`Products Subtotal: ${formatPrice(subTotal)}`);
  if (form.city.trim()) {
    lines.push(`Delivery Charges: ${formatPrice(deliveryCharge)}`);
    lines.push(`Order Total: ${formatPrice(total)}`);
  } else {
    lines.push(`Order Total (before delivery): ${formatPrice(subTotal)}`);
  }

  lines.push('', 'Please assist me. Thank you!');
  return lines.join('\n');
}

export function CheckoutPage() {
  const shopPath = useShopPath();
  const { items, clearCart } = useCartStore();
  const navigate = useNavigate();
  const location = useLocation();
  const buyNow = (location.state as { buyNow?: CartItem } | null)?.buyNow;
  const checkoutItems = buyNow ? [buyNow] : items;
  const checkoutSubTotal = () =>
    checkoutItems.reduce((sum, i) => sum + (i.salePrice ?? i.price), 0);
  const { getCharge } = useDeliveryRates();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [form, setForm] = useState({ name: '', whatsapp: '', city: '', address: '' });
  const [lightbox, setLightbox] = useState<{ images: string[]; alt: string } | null>(null);

  const deliveryCharge = getCharge(form.city);
  const total = checkoutSubTotal() + (form.city.trim() ? deliveryCharge : 0);

  if (checkoutItems.length === 0) {
    return (
      <div className="text-center py-20">
        <h2 className="text-xl font-semibold">Cart is empty</h2>
        <Link to={shopPath} className="text-brand-600 mt-2 inline-block">Go shopping</Link>
      </div>
    );
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      // Read at submit time (not render) so persist/rehydrate races cannot drop the code.
      const affiliateCode = getAffiliateCode();
      const result = await api.placeOrder({
        ...form,
        toyIds: checkoutItems.map((i) => i.toyId),
        affiliateCode: affiliateCode || undefined,
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
    <div className="max-w-5xl mx-auto px-4 sm:px-6 py-8 pb-24 sm:pb-8">
      <SeoHead title={PAGE_SEO.checkout.title} description={PAGE_SEO.checkout.description} path={PAGE_SEO.checkout.path} noIndex />
      <h1 className="text-3xl font-bold text-slate-800 mb-2">Checkout</h1>
      {buyNow && (
        <p className="text-sm text-brand-600 bg-brand-50 border border-brand-100 rounded-xl px-4 py-2.5 mb-6">
          Ordering <span className="font-semibold break-words">{buyNow.name}</span> only. Your cart is unchanged.
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

          <a
            href={getWhatsAppUrl(
              buildCheckoutInquiryMessage(form, checkoutItems, checkoutSubTotal(), deliveryCharge, total),
            )}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex w-full items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-[#25D366] hover:bg-[#20bd5a] text-white text-sm font-semibold shadow-md transition-all"
          >
            <MessageCircle className="w-5 h-5 fill-white shrink-0" />
            WhatsApp Help (with cart details)
          </a>
        </form>

        <div className="lg:col-span-2">
          <div className="bg-white rounded-2xl p-6 border border-slate-100 sticky top-24">
            <h2 className="font-semibold text-slate-800 mb-4">Order Summary</h2>
            <div className="space-y-3 text-sm">
              {checkoutItems.map((item) => {
                const src = item.imageUrl || placeholderImage(item.name);
                return (
                  <div key={item.toyId} className="flex items-center gap-3">
                    <button
                      type="button"
                      onClick={() => setLightbox({ images: [src], alt: item.name })}
                      className="shrink-0 rounded-lg focus:outline-none focus:ring-2 focus:ring-brand-400"
                      aria-label={`View ${item.name} image`}
                    >
                      <img
                        src={src}
                        alt={item.name}
                        className="w-12 h-12 rounded-lg object-cover bg-slate-100 cursor-zoom-in hover:opacity-90 transition-opacity"
                      />
                    </button>
                    <span className="text-slate-600 min-w-0 flex-1 break-words">{item.name}</span>
                    <span className="font-medium shrink-0">{formatPrice(item.salePrice ?? item.price)}</span>
                  </div>
                );
              })}
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
