import { Link } from 'react-router-dom';
import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO } from '@/lib/seo';

export function AboutPage() {
  return (
    <div className="max-w-3xl mx-auto px-4 sm:px-6 py-10">
      <SeoHead
        title={PAGE_SEO.about.title}
        description={PAGE_SEO.about.description}
        path={PAGE_SEO.about.path}
      />
      <h1 className="text-3xl font-bold text-slate-800 mb-4">About Kids Paradise by Shoptick</h1>
      <div className="prose prose-slate max-w-none space-y-4 text-slate-600 leading-relaxed">
        <p>
          Kids Paradise by Shoptick is an online toys shop in Karachi and across Pakistan, offering unique kids toys
          for babies, toddlers, and children. We help parents find quality toys — from soft toys and dolls to educational
          toys, musical toys, and cars — with reliable delivery in Karachi and all major cities in Pakistan.
        </p>
        <p>
          Our mission is to be one of the best toys stores in Pakistan by listing one-of-a-kind items you won&apos;t find
          everywhere else. Every product is carefully selected so your child gets something special.
        </p>
        <p>
          Whether you search for &quot;kids toys in Pakistan&quot;, &quot;online toys shop Karachi&quot;, or the best toys store near you,
          Kids Paradise makes ordering simple with WhatsApp support and doorstep delivery.
        </p>
      </div>
      <Link to="/shop" className="inline-block mt-8 text-brand-600 font-semibold hover:underline">Browse our toy collection →</Link>
    </div>
  );
}
