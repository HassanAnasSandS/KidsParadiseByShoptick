import { cn } from '@/lib/utils';

type BrandVariant = 'default' | 'light' | 'hero' | 'muted' | 'inline';

interface BrandNameProps {
  variant?: BrandVariant;
  showByline?: boolean;
  className?: string;
  nameClassName?: string;
  bylineClassName?: string;
}

const variantStyles: Record<BrandVariant, { name: string; byline: string }> = {
  default: {
    name: 'font-extrabold text-brand-600 text-lg leading-tight tracking-tight',
    byline: 'text-[10px] text-slate-400 font-medium tracking-[0.12em] uppercase',
  },
  light: {
    name: 'font-bold text-white text-xl leading-tight',
    byline: 'text-[11px] text-slate-400 font-medium tracking-[0.1em]',
  },
  hero: {
    name: 'text-sm font-semibold text-white leading-tight',
    byline: 'text-[10px] text-white/75 font-medium tracking-[0.08em]',
  },
  muted: {
    name: 'font-semibold text-slate-700 text-base leading-tight',
    byline: 'text-[10px] text-slate-400 font-medium tracking-[0.1em]',
  },
  inline: {
    name: 'font-semibold text-slate-700 leading-tight',
    byline: 'text-[10px] text-slate-400 font-medium',
  },
};

export function BrandName({
  variant = 'default',
  showByline = true,
  className,
  nameClassName,
  bylineClassName,
}: BrandNameProps) {
  const styles = variantStyles[variant];

  return (
    <div className={cn('inline-flex flex-col leading-none', className)}>
      <span className={cn(styles.name, nameClassName)}>Kids Paradise</span>
      {showByline && (
        <span className={cn(styles.byline, 'mt-0.5', bylineClassName)}>by Shoptick</span>
      )}
    </div>
  );
}
