import { placeholderImage } from '@/lib/utils';

type AdminListImageProps = {
  src?: string | null;
  name: string;
  size?: 'sm' | 'md';
};

const sizeClass = {
  sm: 'w-10 h-10 rounded-lg',
  md: 'w-12 h-12 rounded-xl',
} as const;

export function AdminListImage({ src, name, size = 'md' }: AdminListImageProps) {
  return (
    <img
      src={src || placeholderImage(name)}
      alt=""
      className={`${sizeClass[size]} object-cover border border-slate-100 shrink-0 bg-slate-50`}
    />
  );
}
