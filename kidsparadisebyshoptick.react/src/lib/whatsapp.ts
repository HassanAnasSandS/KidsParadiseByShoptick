import { effectivePrice } from '@/api/client';
import { formatPrice } from '@/lib/utils';

export const WHATSAPP_NUMBER = '923217175896';
export const WHATSAPP_DISPLAY = '0321 7175896';

export function getWhatsAppUrl(message?: string) {
  const base = `https://wa.me/${WHATSAPP_NUMBER}`;
  return message ? `${base}?text=${encodeURIComponent(message)}` : base;
}

export function getToyProductUrl(toyId: number) {
  if (typeof window !== 'undefined') {
    return `${window.location.origin}/product/${toyId}`;
  }
  return `/product/${toyId}`;
}

export function buildToyInquiryMessage(toy: {
  id: number;
  name: string;
  price: number;
  salePrice: number | null;
}) {
  const productUrl = getToyProductUrl(toy.id);
  const price = formatPrice(effectivePrice(toy));

  return [
    'Hello Kids Paradise!',
    '',
    'I would like to inquire about this toy:',
    '',
    `🧸 ${toy.name}`,
    `💰 ${price}`,
    `🔗 ${productUrl}`,
    '',
    'Could you please share more details about availability and delivery?',
    '',
    'Thank you!',
  ].join('\n');
}

export function buildToyInquiryWhatsAppUrl(toy: {
  id: number;
  name: string;
  price: number;
  salePrice: number | null;
}) {
  return getWhatsAppUrl(buildToyInquiryMessage(toy));
}
