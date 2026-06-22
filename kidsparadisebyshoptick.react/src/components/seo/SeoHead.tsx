import { useEffect } from 'react';
import { SITE, absoluteUrl, pageTitle } from '@/lib/seo';

type SeoHeadProps = {
  title?: string;
  description?: string;
  path?: string;
  image?: string | null;
  noIndex?: boolean;
  jsonLd?: object | object[];
};

function upsertMeta(attr: 'name' | 'property', key: string, content: string) {
  let el = document.head.querySelector(`meta[${attr}="${key}"]`);
  if (!el) {
    el = document.createElement('meta');
    el.setAttribute(attr, key);
    document.head.appendChild(el);
  }
  el.setAttribute('content', content);
}

function upsertLink(rel: string, href: string) {
  let el = document.head.querySelector(`link[rel="${rel}"]`) as HTMLLinkElement | null;
  if (!el) {
    el = document.createElement('link');
    el.rel = rel;
    document.head.appendChild(el);
  }
  el.href = href;
}

function upsertJsonLd(data: object | object[] | undefined) {
  const id = 'seo-jsonld';
  let el = document.getElementById(id) as HTMLScriptElement | null;

  if (!data) {
    el?.remove();
    return;
  }

  if (!el) {
    el = document.createElement('script');
    el.id = id;
    el.type = 'application/ld+json';
    document.head.appendChild(el);
  }
  el.textContent = JSON.stringify(data);
}

export function SeoHead({
  title,
  description = SITE.description,
  path = '/',
  image,
  noIndex = false,
  jsonLd,
}: SeoHeadProps) {
  const jsonLdKey = jsonLd ? JSON.stringify(jsonLd) : '';

  useEffect(() => {
    const fullTitle = pageTitle(title);
    const canonical = absoluteUrl(path);
    const ogImage = image ? absoluteUrl(image) : SITE.ogImage;
    const robots = noIndex ? 'noindex, nofollow' : 'index, follow';

    document.title = fullTitle;

    upsertMeta('name', 'description', description);
    upsertMeta('name', 'keywords', SITE.keywords);
    upsertMeta('name', 'robots', robots);
    upsertMeta('name', 'author', SITE.name);
    upsertMeta('name', 'geo.region', SITE.region);
    upsertMeta('name', 'geo.placename', 'Pakistan');

    upsertLink('canonical', canonical);

    upsertMeta('property', 'og:site_name', SITE.name);
    upsertMeta('property', 'og:title', fullTitle);
    upsertMeta('property', 'og:description', description);
    upsertMeta('property', 'og:type', path === '/' ? 'website' : 'article');
    upsertMeta('property', 'og:url', canonical);
    upsertMeta('property', 'og:image', ogImage);
    upsertMeta('property', 'og:locale', SITE.locale);

    upsertMeta('name', 'twitter:card', SITE.twitterCard);
    upsertMeta('name', 'twitter:title', fullTitle);
    upsertMeta('name', 'twitter:description', description);
    upsertMeta('name', 'twitter:image', ogImage);

    upsertJsonLd(jsonLd);
  }, [title, description, path, image, noIndex, jsonLdKey, jsonLd]);

  return null;
}
