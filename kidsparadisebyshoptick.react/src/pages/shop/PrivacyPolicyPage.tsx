import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO } from '@/lib/seo';

export function PrivacyPolicyPage() {
  return (
    <div className="max-w-3xl mx-auto px-4 sm:px-6 py-10">
      <SeoHead
        title={PAGE_SEO.privacy.title}
        description={PAGE_SEO.privacy.description}
        path={PAGE_SEO.privacy.path}
      />
      <h1 className="text-3xl font-bold text-slate-800 mb-4">Privacy Policy</h1>
      <div className="space-y-4 text-slate-600 leading-relaxed text-sm">
        <p><strong>Last updated:</strong> {new Date().getFullYear()}</p>
        <p>
          Kids Paradise by Shoptick (&quot;we&quot;, &quot;our&quot;) operates the online toys shop at kidsparadise.shoptick.shop.
          This policy explains how we collect and use information when you order kids toys in Pakistan through our website.
        </p>
        <h2 className="text-lg font-semibold text-slate-800 pt-2">Information we collect</h2>
        <p>
          When you place an order we collect your name, WhatsApp number, city, and delivery address so we can process
          and deliver your toys across Karachi and Pakistan.
        </p>
        <h2 className="text-lg font-semibold text-slate-800 pt-2">How we use your information</h2>
        <ul className="list-disc pl-5 space-y-1">
          <li>To confirm and deliver your orders</li>
          <li>To contact you via WhatsApp about order status</li>
          <li>To improve our toy store and customer service</li>
        </ul>
        <h2 className="text-lg font-semibold text-slate-800 pt-2">Data sharing</h2>
        <p>
          We do not sell your personal information. We only share details when required for delivery couriers or by law.
        </p>
        <h2 className="text-lg font-semibold text-slate-800 pt-2">Contact</h2>
        <p>
          For privacy questions, contact us via WhatsApp on our Contact page.
        </p>
      </div>
    </div>
  );
}
