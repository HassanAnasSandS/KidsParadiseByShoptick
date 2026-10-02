import { useCallback, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import {
  SITE_IMAGE_DEFAULTS,
  type SiteImageContent,
  type SiteImageKey,
} from '@/lib/siteImages';

function setDocumentIcon(url: string) {
  const selectors = ["link[rel='icon']", "link[rel='apple-touch-icon']"];
  for (const selector of selectors) {
    const link = document.querySelector<HTMLLinkElement>(selector);
    if (link) link.href = url;
  }
}

function normalizeContent(
  key: SiteImageKey,
  value: SiteImageContent | string | undefined,
): SiteImageContent {
  const fallback = SITE_IMAGE_DEFAULTS[key];
  if (!value) return { imageUrl: fallback.imageUrl };
  if (typeof value === 'string') {
    return { imageUrl: value || fallback.imageUrl };
  }
  return {
    imageUrl: value.imageUrl || fallback.imageUrl,
    title: value.title?.trim() || null,
    subtitle: value.subtitle?.trim() || null,
    ctaText: value.ctaText?.trim() || null,
    linkUrl: value.linkUrl?.trim() || null,
    titleColor: value.titleColor?.trim() || null,
    subtitleColor: value.subtitleColor?.trim() || null,
    ctaColor: value.ctaColor?.trim() || null,
  };
}

export function useSiteImages() {
  const { data, isLoading } = useQuery({
    queryKey: ['site-images'],
    queryFn: api.getSiteImages,
    staleTime: 5 * 60 * 1000,
  });

  const getContent = useCallback(
    (key: SiteImageKey) => normalizeContent(key, data?.[key]),
    [data],
  );

  const get = useCallback(
    (key: SiteImageKey) => getContent(key).imageUrl,
    [getContent],
  );

  useEffect(() => {
    setDocumentIcon(get('favicon'));
  }, [get, data]);

  return { images: data, get, getContent, isLoading };
}
