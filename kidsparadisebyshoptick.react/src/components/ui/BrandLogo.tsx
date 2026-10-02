import { cn } from '@/lib/utils';
import { useSiteImages } from '@/hooks/useSiteImages';

interface BrandLogoProps {
  className?: string;
  size?: number;
  alt?: string;
}

export function BrandLogo({ className, size = 40, alt = 'Kids Paradise' }: BrandLogoProps) {
  const { get } = useSiteImages();

  return (
    <img
      src={get('favicon')}
      alt={alt}
      width={size}
      height={size}
      className={cn('rounded-xl object-cover shrink-0', className)}
    />
  );
}
