import { MessageCircle } from 'lucide-react';
import { getWhatsAppUrl } from '@/lib/whatsapp';
import { useSocialLinks } from '@/hooks/useSocialLinks';

export function WhatsAppButton() {
  const { links } = useSocialLinks();

  return (
    <a
      href={getWhatsAppUrl()}
      target="_blank"
      rel="noopener noreferrer"
      className="fixed bottom-20 sm:bottom-24 right-6 z-50 w-14 h-14 bg-[#25D366] hover:bg-[#20bd5a] text-white rounded-full shadow-lg hover:shadow-xl flex items-center justify-center transition-all hover:scale-110"
      aria-label={`Contact on WhatsApp ${links.whatsAppDisplay}`}
    >
      <MessageCircle className="w-7 h-7 fill-white" />
    </a>
  );
}
