import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO } from '@/lib/seo';

export function TermsOfServicePage() {
  return (
    <div className="max-w-3xl mx-auto px-4 sm:px-6 py-10">
      <SeoHead
        title={PAGE_SEO.terms.title}
        description={PAGE_SEO.terms.description}
        path={PAGE_SEO.terms.path}
      />
      <h1 className="text-3xl font-bold text-slate-800 mb-4">Terms of Service</h1>
      <div className="space-y-4 text-slate-600 leading-relaxed text-sm">
        <p><strong>Last updated:</strong> {new Date().getFullYear()}</p>
        <p>
          Welcome to Kids Paradise by Shoptick (&quot;we&quot;, &quot;our&quot;). By using kidsparadise.shoptick.shop
          you agree to these Terms of Service.
        </p>
        <h2 className="text-lg font-semibold text-slate-800 pt-2">Our service</h2>
        <p>
          We sell kids toys online for delivery in Karachi and across Pakistan. Product photos, prices,
          and stock may change. Orders are confirmed via WhatsApp after you place them on the website.
        </p>
        <h2 className="text-lg font-semibold text-slate-800 pt-2">Orders and payment</h2>
        <ul className="list-disc pl-5 space-y-1">
          <li>Prices are shown in Pakistani Rupees (PKR).</li>
          <li>Payment terms (including advance and cash on delivery) are as stated at checkout.</li>
          <li>We may cancel an order if a toy is unavailable or payment cannot be completed.</li>
        </ul>
        <h2 className="text-lg font-semibold text-slate-800 pt-2">Delivery</h2>
        <p>
          Delivery charges and timelines depend on your city. Delays caused by couriers or incorrect
          address details are outside our full control.
        </p>
        <h2 className="text-lg font-semibold text-slate-800 pt-2">Acceptable use</h2>
        <p>
          You agree not to misuse the website, attempt unauthorized access, or place fraudulent orders.
        </p>
        <h2 className="text-lg font-semibold text-slate-800 pt-2">Social media posting</h2>
        <p>
          We may publish product photos and videos of toys we sell on platforms such as Facebook,
          Instagram, YouTube, and TikTok for marketing. Customer personal data is not posted publicly.
        </p>
        <h2 className="text-lg font-semibold text-slate-800 pt-2">Contact</h2>
        <p>
          Questions about these terms: use the Contact page or WhatsApp listed on our website.
        </p>
      </div>
    </div>
  );
}
