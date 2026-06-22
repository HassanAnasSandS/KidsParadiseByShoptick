import { Link } from 'react-router-dom';
import { Star } from 'lucide-react';
import type { ToyListItem } from '@/api/client';
import { effectivePrice, toyPrimaryImage } from '@/api/client';
import { formatPrice, placeholderImage } from '@/lib/utils';
import { ToyInquiryButton } from '@/components/shop/ToyInquiryButton';

interface ToyCardProps {
  toy: ToyListItem;
}

export function ToyCard({ toy }: ToyCardProps) {
  const primary = toyPrimaryImage(toy) || placeholderImage(toy.name);
  const price = effectivePrice(toy);
  const onSale = toy.salePrice != null && toy.salePrice < toy.price;

  return (
    <div className="group relative bg-white rounded-2xl border border-slate-100 overflow-hidden shadow-sm hover:shadow-xl hover:-translate-y-1.5 transition-all duration-300 animate-fade-in ring-1 ring-slate-100 hover:ring-brand-200">
      {!toy.isSold && (
        <ToyInquiryButton
          toy={toy}
          className="absolute top-3 right-3 z-20"
          onClick={(e) => e.stopPropagation()}
        />
      )}
      <Link to={`/product/${toy.id}`} className="block">
        <div className="aspect-square overflow-hidden bg-slate-50 relative">
          <img
            src={primary}
            alt={toy.name}
            className="w-full h-full object-contain p-2 group-hover:scale-105 transition-transform duration-500"
            loading="lazy"
          />
          {onSale && (
            <span className="absolute top-3 left-3 bg-red-500 text-white text-xs font-bold px-2.5 py-1 rounded-full">
              Sale
            </span>
          )}
          {toy.isSold && (
            <div className="absolute inset-0 bg-black/40 flex items-center justify-center">
              <span className="bg-white text-slate-800 text-sm font-semibold px-4 py-1.5 rounded-full">Sold</span>
            </div>
          )}
        </div>
        <div className="p-4">
          <p className="text-xs text-brand-500 font-medium mb-1">{toy.categoryName}</p>
          <h3 className="font-semibold text-slate-800 line-clamp-2 group-hover:text-brand-600 transition-colors">
            {toy.name}
          </h3>
          {toy.averageRating != null && (
            <div className="flex items-center gap-1 mt-1.5">
              <Star className="w-3.5 h-3.5 fill-accent-500 text-accent-500" />
              <span className="text-xs text-slate-500">{toy.averageRating.toFixed(1)}</span>
            </div>
          )}
          <div className="mt-2 flex items-center gap-2">
            <p className="text-lg font-bold text-brand-600">{formatPrice(price)}</p>
            {onSale && (
              <p className="text-sm text-slate-400 line-through">{formatPrice(toy.price)}</p>
            )}
          </div>
        </div>
      </Link>
    </div>
  );
}

export function ToyCardSkeleton() {
  return (
    <div className="bg-white rounded-2xl border border-slate-100 overflow-hidden">
      <div className="aspect-square skeleton" />
      <div className="p-4 space-y-2">
        <div className="h-3 w-16 skeleton rounded" />
        <div className="h-4 w-full skeleton rounded" />
        <div className="h-5 w-20 skeleton rounded mt-2" />
      </div>
    </div>
  );
}
