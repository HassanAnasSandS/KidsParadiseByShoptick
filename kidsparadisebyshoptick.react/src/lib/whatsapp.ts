import { effectivePrice } from '@/api/client';
import type { Order } from '@/api/client';
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
