import { Link } from 'react-router-dom';
import type { Category } from '@/api/client';
import { placeholderImage } from '@/lib/utils';
import { useShopPath } from '@/store/shopFilters';

const categoryIcons = ['🧸', '🎮', '🎨', '⚽', '🚗', '🧩', '🎪', '🦄'];

interface CategoryCardProps {
  category: Category;
  index?: number;
}

export function CategoryCard({ category, index = 0 }: CategoryCardProps) {
  const img = category.imageUrl || placeholderImage(category.name);
  const icon = categoryIcons[index % categoryIcons.length];
  const categoryShopPath = useShopPath({ categoryId: category.id });

  return (
    <Link
      to={categoryShopPath}
      className="group relative bg-white rounded-2xl overflow-hidden border border-slate-100 hover:border-brand-200 hover:shadow-xl transition-all duration-300"
    >
      <div className="aspect-[4/3] overflow-hidden relative">
        <img
          src={img}
          alt={category.name}
          className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500"
          loading="lazy"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-slate-900/70 via-slate-900/20 to-transparent" />
        <span className="absolute top-3 right-3 text-2xl drop-shadow">{icon}</span>
        <div className="absolute bottom-0 left-0 right-0 p-4">
          <h3 className="font-bold text-white text-lg group-hover:text-accent-400 transition-colors">{category.name}</h3>
          <p className="text-white/80 text-xs mt-0.5">{category.toyCount} toys available</p>
        </div>
      </div>
    </Link>
  );
}
