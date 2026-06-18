import { Link } from 'react-router-dom';
import { MessageCircle } from 'lucide-react';
import { BrandName } from '@/components/ui/BrandName';
import { getWhatsAppUrl, WHATSAPP_DISPLAY } from '@/lib/whatsapp';
import { PAYMENT_POLICY_DETAIL } from '@/lib/utils';

export function Footer() {
  return (
    <footer className="relative bg-slate-900 text-slate-300 mt-auto overflow-hidden">
      <div
        className="absolute inset-0 opacity-[0.04] pointer-events-none"
        style={{ backgroundImage: 'url(/watermark.svg)', backgroundRepeat: 'repeat', backgroundSize: '160px' }}
      />
      <div className="max-w-7xl mx-auto px-4 sm:px-6 py-12 relative z-10">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div>
            <div className="flex items-center gap-2.5 mb-4">
              <div className="w-10 h-10 rounded-xl bg-brand-600 flex items-center justify-center text-xl">🧸</div>
              <BrandName variant="light" />
            </div>
            <p className="text-sm text-slate-400 leading-relaxed">
              Pakistan&apos;s favorite online toy store. Unique toys for happy kids, delivered to your doorstep.
            </p>
            <a
              href={getWhatsAppUrl()}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 mt-4 text-sm text-[#25D366] hover:text-[#20bd5a] font-medium"
            >
              <MessageCircle className="w-4 h-4" /> WhatsApp: {WHATSAPP_DISPLAY}
            </a>
          </div>
          <div>
            <h4 className="font-semibold text-white mb-3">Quick Links</h4>
            <div className="flex flex-col gap-2 text-sm">
              <Link to="/shop" className="hover:text-white transition-colors">Shop All Toys</Link>
              <Link to="/track-order" className="hover:text-white transition-colors">My Orders</Link>
              <Link to="/cart" className="hover:text-white transition-colors">My Cart</Link>
            </div>
          </div>
          <div>
            <h4 className="font-semibold text-white mb-3">Delivery Info</h4>
            <p className="text-sm text-slate-400 leading-relaxed">
              Karachi: Rs. 300 delivery<br />
              Other cities: Rs. 400 delivery<br />
              Cash on Delivery — {PAYMENT_POLICY_DETAIL}
            </p>
          </div>
        </div>
        <div className="border-t border-slate-800 mt-8 pt-6 text-center text-sm text-slate-500">
          © {new Date().getFullYear()} Kids Paradise by Shoptick. All rights reserved.
        </div>
      </div>
    </footer>
  );
}
