import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import {
  DEFAULT_SOCIAL_LINKS,
  setSocialLinksCache,
  type SiteSocialLinks,
} from '@/lib/socialLinks';

export function useSocialLinks() {
  const query = useQuery({
    queryKey: ['site-social-links'],
    queryFn: api.getSiteSocialLinks,
    staleTime: 5 * 60 * 1000,
  });

  const links: SiteSocialLinks = query.data
    ? {
        whatsAppNumber: query.data.whatsAppNumber || DEFAULT_SOCIAL_LINKS.whatsAppNumber,
        whatsAppDisplay: query.data.whatsAppDisplay || DEFAULT_SOCIAL_LINKS.whatsAppDisplay,
        youTubeUrl: query.data.youTubeUrl?.trim() ?? '',
        facebookUrl: query.data.facebookUrl?.trim() ?? '',
        instagramUrl: query.data.instagramUrl?.trim() ?? '',
        tikTokUrl: query.data.tikTokUrl?.trim() ?? '',
        pinterestUrl: query.data.pinterestUrl?.trim() ?? '',
      }
    : DEFAULT_SOCIAL_LINKS;

  useEffect(() => {
    setSocialLinksCache(links);
  }, [
    links.whatsAppNumber,
    links.whatsAppDisplay,
    links.youTubeUrl,
    links.facebookUrl,
    links.instagramUrl,
    links.tikTokUrl,
    links.pinterestUrl,
  ]);

  return { links, isLoading: query.isLoading };
}
