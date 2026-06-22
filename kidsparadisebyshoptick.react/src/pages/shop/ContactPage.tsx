import { MessageCircle, MapPin, Truck } from 'lucide-react';
import { getWhatsAppUrl, WHATSAPP_DISPLAY } from '@/lib/whatsapp';
import { PAYMENT_POLICY_DETAIL } from '@/lib/utils';
import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO } from '@/lib/seo';

export function ContactPage() {
  return (
    <div className="max-w-3xl mx-auto px-4 sm:px-6 py-10">
      <SeoHead
        title={PAGE_SEO.contact.title}
        description={PAGE_SEO.contact.description}
        path={PAGE_SEO.contact.path}
      />
      <h1 className="text-3xl font-bold text-slate-800 mb-4">Contact Us</h1>
      <p className="text-slate-600 mb-8 leading-relaxed">
        Questions about kids toys, delivery in Karachi or anywhere in Pakistan? Reach Kids Paradise by Shoptick — your
        trusted online toys shop — on WhatsApp. We reply quickly and help you choose the perfect toy.
      </p>

      <div className="space-y-4">
        <a
          href={getWhatsAppUrl('Hi! I have a question about Kids Paradise toys.')}
          target="_blank"
          rel="noopener noreferrer"
          className="flex items-center gap-4 bg-white border border-slate-200 rounded-2xl p-5 hover:border-[#25D366] hover:shadow-md transition-all"
        >
          <div className="w-12 h-12 rounded-xl bg-[#25D366]/10 flex items-center justify-center shrink-0">
            <MessageCircle className="w-6 h-6 text-[#25D366]" />
          </div>
          <div>
            <p className="font-semibold text-slate-800">WhatsApp</p>
            <p className="text-sm text-slate-500">{WHATSAPP_DISPLAY}</p>
          </div>
        </a>

        <div className="flex items-start gap-4 bg-white border border-slate-200 rounded-2xl p-5">
          <div className="w-12 h-12 rounded-xl bg-brand-50 flex items-center justify-center shrink-0">
            <MapPin className="w-6 h-6 text-brand-600" />
          </div>
          <div>
            <p className="font-semibold text-slate-800">Service Area</p>
            <p className="text-sm text-slate-500">Karachi &amp; delivery across Pakistan</p>
          </div>
        </div>

        <div className="flex items-start gap-4 bg-white border border-slate-200 rounded-2xl p-5">
          <div className="w-12 h-12 rounded-xl bg-amber-50 flex items-center justify-center shrink-0">
            <Truck className="w-6 h-6 text-amber-600" />
          </div>
          <div>
            <p className="font-semibold text-slate-800">Delivery &amp; Payment</p>
            <p className="text-sm text-slate-500">Karachi Rs.300 · Other cities Rs.400 · {PAYMENT_POLICY_DETAIL}</p>
          </div>
        </div>
      </div>
    </div>
  );
}
