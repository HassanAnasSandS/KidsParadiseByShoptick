export const SITE_IMAGE_KEYS = [
  'favicon',
  'hero_slide_1',
  'hero_slide_2',
  'hero_slide_3',
  'hero_slide_4',
  'banner_new_arrivals',
  'banner_perfect_gifts',
  'shop_header',
] as const;

export type SiteImageKey = (typeof SITE_IMAGE_KEYS)[number];

export type SiteImageContent = {
  imageUrl: string;
  title?: string | null;
  subtitle?: string | null;
  ctaText?: string | null;
  linkUrl?: string | null;
  titleColor?: string | null;
  subtitleColor?: string | null;
  ctaColor?: string | null;
};

const WHITE = '#FFFFFF';

export const SITE_IMAGE_DEFAULTS: Record<SiteImageKey, SiteImageContent> = {
  favicon: { imageUrl: '/favicon.png' },
  hero_slide_1: { imageUrl: '/hero/slide-1.jpg' },
  hero_slide_2: { imageUrl: '/hero/slide-2.jpg' },
  hero_slide_3: { imageUrl: '/hero/slide-3.jpg' },
  hero_slide_4: { imageUrl: '/hero/slide-4.jpg' },
  banner_new_arrivals: { imageUrl: '/hero/slide-1.jpg' },
  banner_perfect_gifts: { imageUrl: '/hero/slide-3.jpg' },
  shop_header: { imageUrl: '/hero/slide-1.jpg' },
};

export const SITE_IMAGE_LABELS: Record<SiteImageKey, string> = {
  favicon: 'Favicon / Logo',
  hero_slide_1: 'Hero Slide 1',
  hero_slide_2: 'Hero Slide 2',
  hero_slide_3: 'Hero Slide 3',
  hero_slide_4: 'Hero Slide 4',
  banner_new_arrivals: 'New Arrivals Banner',
  banner_perfect_gifts: 'Perfect Gifts Banner',
  shop_header: 'Shop Page Header',
};

export function resolveSiteText(
  template: string | null | undefined,
  vars: { delivery?: string; count?: string | number } = {},
): string {
  if (!template) return '';
  return template
    .replaceAll('{delivery}', vars.delivery ?? '')
    .replaceAll('{count}', String(vars.count ?? ''));
}

export function resolveSiteColor(value: string | null | undefined, fallback = WHITE): string {
  if (!value?.trim()) return fallback;
  const raw = value.trim();
  const hex = raw.startsWith('#') ? raw.slice(1) : raw;
  if (![3, 6, 8].includes(hex.length)) return fallback;
  if (!/^[0-9a-fA-F]+$/.test(hex)) return fallback;
  return `#${hex}`;
}
