import { effectivePrice } from '@/api/client';
import type { AffiliateLedger, Order } from '@/api/client';
import { formatPrice } from '@/lib/utils';
import { SITE } from '@/lib/seo';
import {
  DEFAULT_SOCIAL_LINKS,
  getCachedWhatsAppDisplay,
  getCachedWhatsAppNumber,
} from '@/lib/socialLinks';

/** @deprecated Prefer useSocialLinks().links — kept for fallback before API loads */
export const WHATSAPP_NUMBER = DEFAULT_SOCIAL_LINKS.whatsAppNumber;
/** @deprecated Prefer useSocialLinks().links — kept for fallback before API loads */
export const WHATSAPP_DISPLAY = DEFAULT_SOCIAL_LINKS.whatsAppDisplay;

export function getWhatsAppUrl(message?: string) {
  const base = `https://wa.me/${getCachedWhatsAppNumber()}`;
  return message ? `${base}?text=${encodeURIComponent(message)}` : base;
}

export function getWhatsAppDisplay() {
  return getCachedWhatsAppDisplay();
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

export function buildOrderInquiryMessage(order: Order) {
  const discount = order.discountAmount ?? 0;
  const advance = order.advanceAmount ?? 0;
  const showPayment =
    order.status === 'Confirmed' || order.status === 'Shipped' || order.status === 'Delivered';

  const items = order.items
    .map((item, i) => `${i + 1}. ${item.toyName}\n   ${formatPrice(item.price)}`)
    .join('\n');

  const lines = [
    'Hello Kids Paradise!',
    '',
    'I need help with my order:',
    '',
    `📦 Order: ${order.orderNumber}`,
    `📋 Status: ${order.status}`,
    `👤 Name: ${order.customerName}`,
    `📱 WhatsApp: ${order.whatsapp}`,
    `🏙️ City: ${order.city}`,
    `📍 Address: ${order.address}`,
    '',
    '🛍️ Items:',
    items,
    '',
    `Products Subtotal: ${formatPrice(order.subTotal)}`,
    `Delivery Charges: ${formatPrice(order.deliveryCharge)}`,
    `Order Total: ${formatPrice(order.total)}`,
  ];

  if (discount > 0) lines.push(`Discount: -${formatPrice(discount)}`);
  if (showPayment && advance > 0) lines.push(`Advance Paid: ${formatPrice(advance)}`);
  if (showPayment) lines.push(`Balance Due: ${formatPrice(order.balanceAmount)}`);
  if (order.trackingNumber) lines.push(`Tracking: ${order.trackingNumber}`);

  lines.push('', 'Please assist me with this order.', '', 'Thank you!');

  return lines.join('\n');
}

export function buildOrderInquiryWhatsAppUrl(order: Order) {
  return getWhatsAppUrl(buildOrderInquiryMessage(order));
}

export function buildOrderSuccessMessage(details: {
  orderNumber: string;
  total?: number;
  deliveryCharge?: number;
}) {
  const lines = [
    'Hello Kids Paradise!',
    '',
    'I just placed an order and would like to confirm:',
    '',
    `📦 Order: ${details.orderNumber}`,
  ];

  if (details.total != null) lines.push(`Order Total: ${formatPrice(details.total)}`);
  if (details.deliveryCharge != null) {
    lines.push(`Delivery Charges: ${formatPrice(details.deliveryCharge)}`);
  }

  lines.push('', 'Please confirm my order. Thank you!');
  return lines.join('\n');
}

export function buildOrderSuccessWhatsAppUrl(details: {
  orderNumber: string;
  total?: number;
  deliveryCharge?: number;
}) {
  return getWhatsAppUrl(buildOrderSuccessMessage(details));
}

function buildAffiliateLink(code: string) {
  return `${SITE.url}/shop?aff=${encodeURIComponent(code)}`;
}

export function buildAffiliateLedgerMessage(ledger: AffiliateLedger) {
  const { partner, totalCommission, totalPaid, balance, entries } = ledger;
  const recent = entries.slice(0, 8);
  const lines = [
    'Hello Kids Paradise!',
    '',
    'Affiliate partner message — ledger details:',
    '',
    `Partner: ${partner.name}`,
    `Code: ${partner.code}`,
    `My link: ${buildAffiliateLink(partner.code)}`,
    '',
    `Earned: ${formatPrice(totalCommission)}`,
    `Paid: ${formatPrice(totalPaid)}`,
    `Outstanding: ${formatPrice(balance)}`,
    `Orders: ${partner.attributedOrders}`,
  ];

  if (recent.length > 0) {
    lines.push('', 'Recent entries:');
    for (const e of recent) {
      const orderBit = e.orderNumber ? ` · Order ${e.orderNumber}` : '';
      lines.push(`• ${e.type}: ${formatPrice(e.amount)}${orderBit}`);
    }
    if (entries.length > recent.length) {
      lines.push(`…and ${entries.length - recent.length} more`);
    }
  }

  lines.push('', `Ledger: ${SITE.url}/partner?code=${encodeURIComponent(partner.code)}`);
  lines.push('', 'Please assist me with my affiliate account.', '', 'Thank you!');
  return lines.join('\n');
}

export function buildAffiliateLedgerWhatsAppUrl(ledger: AffiliateLedger) {
  return getWhatsAppUrl(buildAffiliateLedgerMessage(ledger));
}

export function buildAffiliateSupportMessage(code?: string) {
  const lines = [
    'Hello Kids Paradise!',
    '',
    'I am an affiliate partner and need help.',
  ];
  if (code?.trim()) {
    lines.push('', `My partner code: ${code.trim().toUpperCase()}`);
  }
  lines.push('', 'Please assist me. Thank you!');
  return lines.join('\n');
}

export function buildAffiliateSupportWhatsAppUrl(code?: string) {
  return getWhatsAppUrl(buildAffiliateSupportMessage(code));
}
