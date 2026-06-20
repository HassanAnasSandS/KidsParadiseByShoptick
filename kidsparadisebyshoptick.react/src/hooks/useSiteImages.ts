import { useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import { SITE_IMAGE_DEFAULTS, type SiteImageKey } from '@/lib/siteImages';

export function useSiteImages() {
  const { data, isLoading } = useQuery({
    queryKey: ['site-images'],
    queryFn: api.getSiteImages,
    staleTime: 5 * 60 * 1000,
  });

  const get = useCallback(
    (key: SiteImageKey) => data?.[key] ?? SITE_IMAGE_DEFAULTS[key],
    [data]
  );

  return { images: data, get, isLoading };
}
