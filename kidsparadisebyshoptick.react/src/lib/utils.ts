import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatPrice(amount: number) {
  return `Rs. ${amount.toLocaleString('en-PK')}`;
}

export function getDeliveryCharge(city: string, rates?: { karachi: number; otherCities: number }) {
  const karachi = rates?.karachi ?? 300;
  const other = rates?.otherCities ?? 400;
  return city.trim().toLowerCase() === 'karachi' ? karachi : other;
}

export const PAYMENT_POLICY = '10% advance payment required';
export const PAYMENT_POLICY_DETAIL = '10% advance payment required. Balance payable on delivery.';

export function sortCategoriesByName<T extends { name: string }>(categories: readonly T[]): T[] {
  return [...categories].sort((a, b) =>
    a.name.localeCompare(b.name, undefined, { sensitivity: 'base' })
  );
}

export function placeholderImage(name: string) {
  const colors = ['3b82f6', 'f59e0b', '10b981', 'ec4899', '8b5cf6'];
  const color = colors[name.length % colors.length];
  const text = encodeURIComponent(name.slice(0, 2).toUpperCase());
  return `https://placehold.co/400x400/${color}/ffffff?text=${text}`;
}
