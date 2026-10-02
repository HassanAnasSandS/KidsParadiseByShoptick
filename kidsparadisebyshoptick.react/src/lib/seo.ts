/** Canonical SEO config — keep in sync with appsettings.json → Seo section */
export const SITE = {
  name: 'Kids Paradise by Shoptick',
  url: 'https://kidsparadise.shoptick.shop',
  title: 'Kids Paradise by Shoptick — Online Toy Shop Karachi & Pakistan',
  description:
    'Buy kids toys online in Karachi & Pakistan with cash on delivery. Soft toys, dolls, RC cars, educational toys & gifts. 10% advance, balance on delivery.',
  keywords:
    'buy toys online Pakistan, kids toys Karachi, online toy shop Pakistan, cash on delivery toys, soft toys, educational toys, RC cars, Shoptick Kids Paradise',
  ogImage: 'https://kidsparadise.shoptick.shop/uploads/site/d1b0e2a546a0425b94ffad7ab55e5d62.jpg',
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

/** Keep meta title/description within typical SERP limits. */
export function clipSeo(text: string, max: number): string {
  const clean = text.replace(/\s+/g, ' ').trim();
  if (clean.length <= max) return clean;
  const sliced = clean.slice(0, max - 1);
  const lastSpace = sliced.lastIndexOf(' ');
  return `${(lastSpace > 40 ? sliced.slice(0, lastSpace) : sliced).trimEnd()}…`;
}

/**
 * Per-toy SEO for organic search (Pakistan buy intent + COD).
 * Title becomes: `{title} | Kids Paradise by Shoptick`
 */
export function buildProductSeo(toy: {
  name: string;
  categoryName: string;
  price: number;
  salePrice: number | null;
  isSold?: boolean;
}) {
  const price = toy.salePrice ?? toy.price;
  const priceLabel = `Rs. ${price.toLocaleString('en-PK')}`;
  const soldNote = toy.isSold ? ' Currently sold out.' : '';

  const title = clipSeo(`${toy.name} – Buy Online Pakistan`, 58);

  const description = clipSeo(
    `Buy original ${toy.name} in Pakistan with cash on delivery.${soldNote} ${toy.categoryName} from Kids Paradise by Shoptick (Karachi). Price ${priceLabel}. Fast delivery nationwide.`,
    158,
  );

  const keywords = [
    toy.name,
    `buy ${toy.name} Pakistan`,
    toy.categoryName,
    'kids toys Pakistan',
    'toys Karachi cash on delivery',
    'online toy shop Pakistan',
    SITE.name,
  ].join(', ');

  return { title, description, keywords };
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
    logo: absoluteUrl('/favicon.png'),
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

/** Local pack / map signals — pair with a real Google Business Profile. */
export function buildLocalBusinessJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'ToyStore',
    name: SITE.name,
    url: SITE.url,
    image: SITE.ogImage,
    description: SITE.description,
    priceRange: 'PKR',
    address: {
      '@type': 'PostalAddress',
      addressLocality: 'Karachi',
      addressRegion: 'Sindh',
      addressCountry: 'PK',
    },
    areaServed: [
      { '@type': 'City', name: 'Karachi' },
      { '@type': 'Country', name: 'Pakistan' },
    ],
    openingHoursSpecification: {
      '@type': 'OpeningHoursSpecification',
      dayOfWeek: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'],
      opens: '10:00',
      closes: '22:00',
    },
  };
}

export function buildFaqJsonLd(faqs: { question: string; answer: string }[]) {
  return {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: faqs.map((f) => ({
      '@type': 'Question',
      name: f.question,
      acceptedAnswer: { '@type': 'Answer', text: f.answer },
    })),
  };
}

export const HOME_FAQS = [
  {
    question: 'Do you deliver kids toys across Pakistan?',
    answer:
      'Yes. Kids Paradise by Shoptick delivers nationwide. Karachi delivery is Rs.300 and other cities are Rs.400. Pay 10% advance and the balance on delivery.',
  },
  {
    question: 'Are the toys new or used?',
    answer:
      'We sell unique pre-loved and carefully listed toys. Each item is usually available only once — check the product page for condition and photos.',
  },
  {
    question: 'How do I order toys online with cash on delivery?',
    answer:
      'Browse the shop, add a toy to cart or tap Order Now, then complete checkout. We confirm on WhatsApp and arrange delivery across Pakistan.',
  },
] as const;

export const PRODUCT_FAQS = [
  {
    question: 'Is cash on delivery available for this toy?',
    answer:
      'Yes for most of Pakistan. Pay a small advance (about 10%), then pay the remaining amount on delivery.',
  },
  {
    question: 'How fast is delivery from Kids Paradise by Shoptick?',
    answer:
      'Orders are usually prepared quickly after WhatsApp confirmation. Delivery timing depends on your city (Karachi is typically fastest).',
  },
] as const;

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
  const { description } = buildProductSeo(toy);

  return {
    '@context': 'https://schema.org',
    '@type': 'Product',
    name: toy.name,
    image: images,
    description,
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
      itemCondition: 'https://schema.org/UsedCondition',
      seller: { '@type': 'Organization', name: SITE.name },
      shippingDetails: {
        '@type': 'OfferShippingDetails',
        shippingDestination: {
          '@type': 'DefinedRegion',
          addressCountry: 'PK',
        },
      },
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
    title: 'Shop Kids Toys Online Pakistan',
    description:
      'Browse kids toys online in Pakistan with cash on delivery. Soft toys, dolls, RC cars & educational toys from Kids Paradise by Shoptick — Karachi & nationwide delivery.',
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
      'Kids Paradise by Shoptick — online toys shop in Karachi & Pakistan. Unique kids toys, soft toys, educational toys with cash on delivery nationwide.',
    path: '/about',
  },
  contact: {
    title: 'Contact Us',
    description:
      'Contact Kids Paradise by Shoptick via WhatsApp. Order help, delivery queries & toy inquiries for Karachi & all Pakistan.',
    path: '/contact',
  },
  privacy: {
    title: 'KidsParadiseByShoptick Privacy Policy',
    description: 'KidsParadiseByShoptick Privacy Policy for Kids Paradise by Shoptick online toy shop at kidsparadise.shoptick.shop.',
    path: '/privacy-policy',
  },
  terms: {
    title: 'KidsParadiseByShoptick Terms of Service',
    description: 'KidsParadiseByShoptick Terms of Service for Kids Paradise by Shoptick online toy shop at kidsparadise.shoptick.shop.',
    path: '/terms-of-service',
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
  partnerPortal: {
    title: 'Affiliate Partner Ledger',
    description: 'Read-only affiliate partner ledger for Kids Paradise by Shoptick partners.',
    path: '/partner',
    noIndex: true,
  },
} as const;
