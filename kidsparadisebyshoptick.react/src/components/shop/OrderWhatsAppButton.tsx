import { MessageCircle } from 'lucide-react';
import type { Order } from '@/api/client';
import { buildOrderInquiryWhatsAppUrl } from '@/lib/whatsapp';
import { cn } from '@/lib/utils';

interface OrderWhatsAppButtonProps {
  order: Order;
  size?: 'sm' | 'md';
  showLabel?: boolean;
  className?: string;
  onClick?: (e: React.MouseEvent) => void;
}

export function OrderWhatsAppButton({
  order,
  size = 'sm',
  showLabel = false,
  className,
  onClick,
}: OrderWhatsAppButtonProps) {
  const dim = size === 'sm' ? 'w-9 h-9' : 'w-11 h-11';
  const icon = size === 'sm' ? 'w-4 h-4' : 'w-5 h-5';

  if (showLabel) {
    return (
      <a
        href={buildOrderInquiryWhatsAppUrl(order)}
        target="_blank"
        rel="noopener noreferrer"
        aria-label={`Contact Kids Paradise about order ${order.orderNumber} on WhatsApp`}
        onClick={onClick}
        className={cn(
          'inline-flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-[#25D366] hover:bg-[#20bd5a] text-white text-sm font-semibold shadow-md transition-all hover:scale-[1.02]',
          className,
        )}
      >
        <MessageCircle className="w-5 h-5 fill-white shrink-0" />
        WhatsApp Support
      </a>
    );
  }

  return (
    <a
      href={buildOrderInquiryWhatsAppUrl(order)}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={`Contact Kids Paradise about order ${order.orderNumber} on WhatsApp`}
      title="Contact on WhatsApp"
      onClick={onClick}
      className={cn(
        `${dim} bg-[#25D366] hover:bg-[#20bd5a] text-white rounded-full shadow-md flex items-center justify-center transition-all hover:scale-105 shrink-0`,
        className,
      )}
    >
      <MessageCircle className={`${icon} fill-white`} />
    </a>
  );
}
