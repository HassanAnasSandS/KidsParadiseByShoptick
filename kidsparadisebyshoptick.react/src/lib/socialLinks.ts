export type SiteSocialLinks = {
  whatsAppNumber: string;
  whatsAppDisplay: string;
  youTubeUrl: string;
  facebookUrl: string;
  instagramUrl: string;
  tikTokUrl: string;
  pinterestUrl: string;
};

export const DEFAULT_SOCIAL_LINKS: SiteSocialLinks = {
  whatsAppNumber: '923217175896',
  whatsAppDisplay: '0321 7175896',
  youTubeUrl: 'https://www.youtube.com/@KidsParadiseByShoptick',
  facebookUrl: 'https://www.facebook.com/share/1DesofLX6U/',
  instagramUrl: 'https://www.instagram.com/miniclosetpk?igsh=cnVuazFjZjE3d3Vo',
  tikTokUrl: 'https://www.tiktok.com/@kiranhassanhk?_r=1&_t=ZS-92ITfkXq7AG',
  pinterestUrl: '',
};

let cached: SiteSocialLinks = { ...DEFAULT_SOCIAL_LINKS };

export function setSocialLinksCache(links: SiteSocialLinks) {
  cached = { ...links };
}

export function getCachedSocialLinks(): SiteSocialLinks {
  return cached;
}

export function getCachedWhatsAppNumber() {
  return cached.whatsAppNumber || DEFAULT_SOCIAL_LINKS.whatsAppNumber;
}

export function getCachedWhatsAppDisplay() {
  return cached.whatsAppDisplay || DEFAULT_SOCIAL_LINKS.whatsAppDisplay;
}
