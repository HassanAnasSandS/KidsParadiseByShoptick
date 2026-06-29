import { type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useShopPath } from '@/store/shopFilters';
import { MessageCircle } from 'lucide-react';
import { BrandName } from '@/components/ui/BrandName';
import { getWhatsAppUrl, WHATSAPP_DISPLAY } from '@/lib/whatsapp';
import { PAYMENT_POLICY_DETAIL } from '@/lib/utils';

function SocialIcon({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <span className={`inline-flex shrink-0 ${className ?? ''}`} aria-hidden="true">
      {children}
    </span>
  );
}

function YouTubeIcon({ className }: { className?: string }) {
  return (
    <SocialIcon className={className}>
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4">
        <path d="M23.5 6.2a3 3 0 0 0-2.1-2.1C19.5 3.5 12 3.5 12 3.5s-7.5 0-9.4.6A3 3 0 0 0 .5 6.2 31 31 0 0 0 0 12a31 31 0 0 0 .5 5.8 3 3 0 0 0 2.1 2.1c1.9.6 9.4.6 9.4.6s7.5 0 9.4-.6a3 3 0 0 0 2.1-2.1A31 31 0 0 0 24 12a31 31 0 0 0-.5-5.8ZM9.7 15.5V8.5L15.8 12l-6.1 3.5Z" />
      </svg>
    </SocialIcon>
  );
}

function TikTokIcon({ className }: { className?: string }) {
  return (
    <SocialIcon className={className}>
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4">
        <path d="M19.59 6.69a4.83 4.83 0 0 1-3.77-4.25V2h-3.45v13.67a2.89 2.89 0 0 1-2.88 2.5 2.89 2.89 0 0 1-2.89-2.89 2.89 2.89 0 0 1 2.89-2.89c.28 0 .54.04.79.1V9.01a6.27 6.27 0 0 0-.79-.05 6.34 6.34 0 0 0-6.34 6.34 6.34 6.34 0 0 0 6.34 6.34 6.34 6.34 0 0 0 6.33-6.34V8.69a8.18 8.18 0 0 0 4.78 1.52V6.76a4.85 4.85 0 0 1-1.01-.07z" />
      </svg>
    </SocialIcon>
  );
}

function InstagramIcon({ className }: { className?: string }) {
  return (
    <SocialIcon className={className}>
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4">
        <path d="M7 2h10a5 5 0 0 1 5 5v10a5 5 0 0 1-5 5H7a5 5 0 0 1-5-5V7a5 5 0 0 1 5-5Zm10 2H7a3 3 0 0 0-3 3v10a3 3 0 0 0 3 3h10a3 3 0 0 0 3-3V7a3 3 0 0 0-3-3Zm-5 3.5A5.5 5.5 0 1 1 6.5 13 5.5 5.5 0 0 1 12 7.5Zm0 2A3.5 3.5 0 1 0 15.5 13 3.5 3.5 0 0 0 12 9.5ZM17.8 6.2a1.1 1.1 0 1 1-1.1 1.1 1.1 1.1 0 0 1 1.1-1.1Z" />
      </svg>
    </SocialIcon>
  );
}

function FacebookIcon({ className }: { className?: string }) {
  return (
    <SocialIcon className={className}>
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4">
        <path d="M13.5 3h3.6l-.2 3.9h-3.4c-1.7 0-2 .8-2 2v2.6h4l-.5 3.9h-3.5V21H9.5v-5.6H6.5V12h3V9.8c0-3 1.8-4.7 4.5-4.7Z" />
      </svg>
    </SocialIcon>
  );
}

const SOCIAL_LINKS = [
  {
    label: 'YouTube',
    href: 'https://www.youtube.com/@KidsParadiseByShoptick',
    icon: YouTubeIcon,
  },
  {
    label: 'TikTok',
    href: 'https://www.tiktok.com/@kiranhassanhk?_r=1&_t=ZS-92ITfkXq7AG',
    icon: TikTokIcon,
  },
  {
    label: 'Instagram',
    href: 'https://www.instagram.com/miniclosetpk?igsh=cnVuazFjZjE3d3Vo',
    icon: InstagramIcon,
  },
  {
    label: 'Facebook',
    href: 'https://www.facebook.com/share/1DesofLX6U/',
    icon: FacebookIcon,
  },
] as const;

export function Footer() {
  const shopPath = useShopPath();
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
            <div className="mt-5">
              <h4 className="font-semibold text-white mb-3 text-sm">Follow Us</h4>
              <div className="flex flex-wrap gap-2">
                {SOCIAL_LINKS.map(({ label, href, icon: Icon }) => (
                  <a
                    key={label}
                    href={href}
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label={label}
                    title={label}
                    className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-800/80 px-3 py-2 text-sm text-slate-300 transition-colors hover:border-slate-500 hover:bg-slate-800 hover:text-white"
                  >
                    <Icon />
                    <span>{label}</span>
                  </a>
                ))}
              </div>
            </div>
          </div>
          <div>
            <h4 className="font-semibold text-white mb-3">Quick Links</h4>
            <div className="flex flex-col gap-2 text-sm">
              <Link to={shopPath} className="hover:text-white transition-colors">Shop All Toys</Link>
              <Link to="/about" className="hover:text-white transition-colors">About Us</Link>
              <Link to="/contact" className="hover:text-white transition-colors">Contact</Link>
              <Link to="/privacy-policy" className="hover:text-white transition-colors">Privacy Policy</Link>
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
