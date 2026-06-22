/** Canonical SEO config — keep in sync with appsettings.json → Seo section */
export const SITE = {
  name: 'Kids Paradise by Shoptick',
  url: 'https://kidsparadise.shoptick.shop',
  title: 'Kids Paradise by Shoptick — Online Toy Shop Karachi & Pakistan',
  description:
    'Shop unique kids toys online in Karachi & Pakistan. Soft toys, dolls, educational toys, cars & gifts with fast delivery. 10% advance, balance on delivery.',
  keywords:
    'kids toys Pakistan, online toy shop Karachi, buy toys online Pakistan, toy store Pakistan, soft toys, educational toys, baby toys, Shoptick',
  ogImage: 'https://kidsparadise.shoptick.shop/hero/slide-2.jpg',
  locale: 'en_PK',
  region: 'PK',
  twitterCard: 'summary_large_image' as const,
} as const;

export function absoluteUrl(path: string): string {
  if (path.startsWith('http://') || path.startsWith('https://')) return path;
  return `${SITE.url}${path.startsWith('/') ? path : `/${path}`}`;
}

export function pageTitle(title?: string): string {
  if (!title) return SITE.title;
  return `${title} | ${SITE.name}`;
}

export function buildBreadcrumbJsonLd(items: { name: string; path: string }[]) {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: items.map((item, i) => ({
      '@type': 'ListItem',
      position: i + 1,
      name: item.name,
      item: absoluteUrl(item.path),
    })),
  };
}

export function buildOrganizationJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'OnlineStore',
    name: SITE.name,
    url: SITE.url,
    logo: absoluteUrl('/favicon.svg'),
    image: SITE.ogImage,
    description: SITE.description,
    areaServed: { '@type': 'Country', name: 'Pakistan' },
    address: {
      '@type': 'PostalAddress',
      addressLocality: 'Karachi',
      addressCountry: 'PK',
    },
  };
}

export function buildWebSiteJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebSite',
    name: SITE.name,
    url: SITE.url,
    potentialAction: {
      '@type': 'SearchAction',
      target: {
        '@type': 'EntryPoint',
        urlTemplate: `${SITE.url}/shop?search={search_term_string}`,
      },
      'query-input': 'required name=search_term_string',
    },
  };
}

export function buildProductJsonLd(toy: {
  id: number;
  name: string;
  imageUrls: string[];
  isSold: boolean;
  categoryName: string;
  price: number;
  salePrice: number | null;
}) {
  const price = toy.salePrice ?? toy.price;
  const images = toy.imageUrls.length > 0 ? toy.imageUrls.map(absoluteUrl) : [SITE.ogImage];

  return {
    '@context': 'https://schema.org',
    '@type': 'Product',
    name: toy.name,
    image: images,
    description: `${toy.name} — ${toy.categoryName}. Buy online at ${SITE.name} with delivery across Pakistan.`,
    sku: `KP-${toy.id}`,
    brand: { '@type': 'Brand', name: SITE.name },
    category: toy.categoryName,
    offers: {
      '@type': 'Offer',
      url: absoluteUrl(`/product/${toy.id}`),
      priceCurrency: 'PKR',
      price: String(price),
      availability: toy.isSold
        ? 'https://schema.org/OutOfStock'
        : 'https://schema.org/InStock',
      seller: { '@type': 'Organization', name: SITE.name },
    },
  };
}

export const PAGE_SEO = {
  home: {
    title: SITE.title,
    description: SITE.description,
    path: '/',
  },
  shop: {
    title: 'Shop All Toys',
    description:
      'Browse all available kids toys at Kids Paradise by Shoptick. Filter by category, price & sale. Unique toys with delivery in Karachi & Pakistan.',
    path: '/shop',
  },
  reviews: {
    title: 'Customer Reviews',
    description:
      'Read verified customer reviews for toys purchased from Kids Paradise by Shoptick. Real feedback from parents across Pakistan.',
    path: '/reviews',
  },
  about: {
    title: 'About Us',
    description:
      'Kids Paradise by Shoptick — online toys shop in Karachi & Pakistan. Unique kids toys, soft toys, educational toys with delivery nationwide.',
    path: '/about',
  },
  contact: {
    title: 'Contact Us',
    description:
      'Contact Kids Paradise by Shoptick via WhatsApp. Order help, delivery queries & toy inquiries for Karachi & all Pakistan.',
    path: '/contact',
  },
  privacy: {
    title: 'Privacy Policy',
    description: 'Privacy policy for Kids Paradise by Shoptick online toy shop at kidsparadise.shoptick.shop.',
    path: '/privacy-policy',
  },
  trackOrder: {
    title: 'Track Your Order',
    description:
      'Track your Kids Paradise by Shoptick order status using your WhatsApp number. See pending, confirmed, shipped & delivered updates.',
    path: '/track-order',
  },
  cart: {
    title: 'Shopping Cart',
    description: 'Your shopping cart at Kids Paradise by Shoptick.',
    path: '/cart',
    noIndex: true,
  },
  checkout: {
    title: 'Checkout',
    description: 'Complete your toy order at Kids Paradise by Shoptick.',
    path: '/checkout',
    noIndex: true,
  },
  orderSuccess: {
    title: 'Order Placed',
    description: 'Your order has been placed successfully.',
    path: '/order-success',
    noIndex: true,
  },
} as const;
