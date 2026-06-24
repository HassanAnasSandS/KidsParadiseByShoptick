import { useState } from 'react';
import { Link, useParams, useLocation } from 'react-router-dom';
import { CheckCircle, Copy, Check } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { BrandName } from '@/components/ui/BrandName';
import { formatPrice, PAYMENT_POLICY } from '@/lib/utils';
import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO } from '@/lib/seo';
import { useShopPath } from '@/store/shopFilters';

export function OrderSuccessPage() {
  const shopPath = useShopPath();
  const { orderNumber } = useParams<{ orderNumber: string }>();
  const location = useLocation();
  const state = location.state as { total?: number; deliveryCharge?: number } | null;
  const [copied, setCopied] = useState(false);

  const copyOrderNumber = async () => {
    if (!orderNumber) return;
    try {
      await navigator.clipboard.writeText(orderNumber);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      /* clipboard unavailable */
    }
  };

  return (
    <div className="max-w-lg mx-auto px-4 py-20 text-center animate-fade-in">
      <SeoHead title={PAGE_SEO.orderSuccess.title} description={PAGE_SEO.orderSuccess.description} path={PAGE_SEO.orderSuccess.path} noIndex />
      <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
        <CheckCircle className="w-10 h-10 text-green-600" />
      </div>
      <h1 className="text-3xl font-bold text-slate-800">Order Placed!</h1>
      <p className="text-slate-500 mt-2 flex flex-col items-center gap-1">
        <span>Thank you for shopping at</span>
        <BrandName variant="inline" className="items-center" />
      </p>

      <div className="bg-white rounded-2xl p-6 border border-slate-100 mt-8 text-left">
        <p className="text-sm text-slate-500">Order Number</p>
        <div className="flex items-center gap-2 mt-1">
          <p className="text-2xl font-bold text-brand-600 break-all flex-1">{orderNumber}</p>
          <button
            type="button"
            onClick={copyOrderNumber}
            className="shrink-0 p-2.5 rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-50 hover:text-brand-600 hover:border-brand-300 transition-colors"
            aria-label="Copy order number"
            title={copied ? 'Copied!' : 'Copy order number'}
          >
            {copied ? <Check className="w-5 h-5 text-green-600" /> : <Copy className="w-5 h-5" />}
          </button>
        </div>
        {copied && <p className="text-xs text-green-600 mt-1">Copied to clipboard!</p>}        {state?.total != null && (
          <p className="mt-3 text-slate-700">Total: <span className="font-bold">{formatPrice(state.total)}</span></p>
        )}
        {state?.deliveryCharge != null && (
          <p className="text-sm text-slate-500">Includes {formatPrice(state.deliveryCharge)} delivery</p>
        )}
      </div>

      <p className="text-sm text-slate-500 mt-4">
        We&apos;ll contact you shortly to confirm your order. {PAYMENT_POLICY}.
      </p>

      <div className="flex flex-col sm:flex-row gap-3 mt-8 justify-center">
        <Link to="/track-order"><Button variant="outline">My Orders</Button></Link>
        <Link to={shopPath}><Button>Continue Shopping</Button></Link>
      </div>
    </div>
  );
}
