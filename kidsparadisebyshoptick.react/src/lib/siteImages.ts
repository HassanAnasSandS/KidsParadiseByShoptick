export const SITE_IMAGE_KEYS = [
  'hero_slide_1',
  'hero_slide_2',
  'hero_slide_3',
  'hero_slide_4',
  'banner_new_arrivals',
  'banner_perfect_gifts',
  'shop_header',
] as const;

export type SiteImageKey = (typeof SITE_IMAGE_KEYS)[number];

export const SITE_IMAGE_DEFAULTS: Record<SiteImageKey, string> = {
  hero_slide_1: '/hero/slide-1.jpg',
  hero_slide_2: '/hero/slide-2.jpg',
  hero_slide_3: '/hero/slide-3.jpg',
  hero_slide_4: '/hero/slide-4.jpg',
  banner_new_arrivals: '/hero/slide-1.jpg',
  banner_perfect_gifts: '/hero/slide-3.jpg',
  shop_header: '/hero/slide-1.jpg',
};

export const SITE_IMAGE_LABELS: Record<SiteImageKey, string> = {
  hero_slide_1: 'Hero Slide 1',
  hero_slide_2: 'Hero Slide 2',
  hero_slide_3: 'Hero Slide 3',
  hero_slide_4: 'Hero Slide 4',
  banner_new_arrivals: 'New Arrivals Banner',
  banner_perfect_gifts: 'Perfect Gifts Banner',
  shop_header: 'Shop Page Header',
};
