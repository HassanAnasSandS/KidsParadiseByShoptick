import { useQuery } from '@tanstack/react-query';
import { useParams, Navigate } from 'react-router-dom';
import { api } from '@/api/client';
import { buildShopPath, mergeShopFilters } from '@/lib/shopFilters';
import { useShopFiltersStore } from '@/store/shopFilters';

/** Legacy route — redirects to Shop with category filter while preserving other filters. */
export function CategoryPage() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading } = useQuery({
    queryKey: ['category', id],
    queryFn: () => api.getCategory(Number(id)),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 py-20 text-center text-slate-500">
        Loading category…
      </div>
    );
  }

  if (!data) {
    return <Navigate to="/shop" replace />;
  }

  const target = buildShopPath(
    mergeShopFilters(useShopFiltersStore.getState().filters, { categoryId: data.id }),
  );

  return <Navigate to={target} replace />;
}
