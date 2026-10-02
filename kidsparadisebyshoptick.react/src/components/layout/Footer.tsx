import { type ComponentType, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { MessageCircle } from 'lucide-react';
import { BrandName } from '@/components/ui/BrandName';
import { BrandLogo } from '@/components/ui/BrandLogo';
import { getWhatsAppUrl } from '@/lib/whatsapp';
import { useSocialLinks } from '@/hooks/useSocialLinks';
import { withAffiliatePath } from '@/store/affiliate';

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
        <path d="M7 2h10a5 5 0 0 1 5 5v10a5 5 0 0 1-5 5H7a5 5 0 0 1-5-5V7a5 5 0 0 1 5-5Zm10 2H7a3 3 0 0 0-3 3v10a3 3 0 0 0 3 3h10a3 3 0 0 0 3-3V7a3 3 0 0 0-3-3Zm-5 3.5A5.5 5.5 0 1 1 6.5 13 5.5 5.5 0 0 1 12 7.5Zm0 2A3.5 3.5 0 1 0 15.5 13 3.5 3.5 0 0 0 12 9.5ZM17.8 6.2a1.1 1.1 0 1 1-1.1 1.1 1.1 0 0 1 1.1-1.1Z" />
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

function PinterestIcon({ className }: { className?: string }) {
  return (
    <SocialIcon className={className}>
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4">
        <path d="M12.04 2C6.87 2 3 5.7 3 10.4c0 3 1.8 5.4 4.5 6.3-.06-.53-.12-1.35.03-1.93.13-.53.86-3.45.86-3.45s-.22-.44-.22-1.1c0-1.03.6-1.8 1.34-1.8.63 0 .94.48.94 1.05 0 .64-.41 1.6-.62 2.49-.18.74.37 1.35 1.1 1.35 1.32 0 2.2-1.7 2.2-3.7 0-1.53-1.03-2.68-2.9-2.68-2.12 0-3.44 1.58-3.44 3.34 0 .61.18 1.04.47 1.37.13.15.15.27.1.42-.03.13-.11.43-.14.55-.05.17-.18.23-.34.14-1-.52-1.47-1.9-1.47-3.45 0-2.56 2.17-5.64 6.48-5.64 3.47 0 5.75 2.5 5.75 5.2 0 3.56-1.98 6.22-4.9 6.22-.98 0-1.9-.53-2.22-1.14l-.6 2.32c-.22.85-.65 1.7-.97 2.34A9.8 9.8 0 0 0 12.04 22C17.6 22 22 17.52 22 12S17.6 2 12.04 2Z" />
      </svg>
    </SocialIcon>
  );
}

type SocialItem = {
  label: string;
  href: string;
  icon: ComponentType<{ className?: string }>;
};

export function Footer() {
  const homePath = withAffiliatePath('/');
  const { links } = useSocialLinks();

  const socialItems: SocialItem[] = [
    { label: 'YouTube', href: links.youTubeUrl, icon: YouTubeIcon },
    { label: 'TikTok', href: links.tikTokUrl, icon: TikTokIcon },
    { label: 'Instagram', href: links.instagramUrl, icon: InstagramIcon },
    { label: 'Facebook', href: links.facebookUrl, icon: FacebookIcon },
    { label: 'Pinterest', href: links.pinterestUrl, icon: PinterestIcon },
  ].filter((item) => item.href.length > 0);

  return (
    <footer className="fixed bottom-0 inset-x-0 z-50 bg-slate-900/95 text-slate-300 backdrop-blur-md border-t border-slate-800 shadow-[0_-4px_16px_rgba(15,23,42,0.25)]">
      <div className="max-w-7xl mx-auto px-4 sm:px-6">
        <div className="flex items-center justify-between gap-3 h-14 sm:h-16">
          <Link to={homePath} className="flex items-center gap-2.5 shrink-0 min-w-0">
            <BrandLogo size={32} className="w-8 h-8 sm:w-9 sm:h-9" />
            <BrandName
              variant="light"
              className="hidden sm:inline-flex"
              nameClassName="text-base sm:text-lg"
            />
          </Link>

          <div className="flex items-center gap-2 sm:gap-3 min-w-0">
            <a
              href={getWhatsAppUrl()}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-1.5 text-xs sm:text-sm text-[#25D366] hover:text-[#20bd5a] font-medium shrink-0"
              aria-label={`WhatsApp ${links.whatsAppDisplay}`}
            >
              <MessageCircle className="w-4 h-4" />
              <span className="hidden sm:inline">{links.whatsAppDisplay}</span>
            </a>

            <div className="flex items-center gap-1 sm:gap-1.5" aria-label="Follow us">
              {socialItems.map(({ label, href, icon: Icon }) => (
                <a
                  key={label}
                  href={href}
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label={label}
                  title={label}
                  className="inline-flex items-center justify-center w-8 h-8 sm:w-9 sm:h-9 rounded-full border border-slate-700 bg-slate-800/80 text-slate-300 transition-colors hover:border-slate-500 hover:bg-slate-800 hover:text-white"
                >
                  <Icon />
                </a>
              ))}
            </div>

            <p className="hidden lg:block text-xs text-slate-500 whitespace-nowrap pl-2 border-l border-slate-700">
              © {new Date().getFullYear()} Kids Paradise by Shoptick
            </p>
          </div>
        </div>
      </div>
    </footer>
  );
}
