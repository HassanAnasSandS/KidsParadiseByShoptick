export interface ShopFilters {
  search: string;
  categoryId?: number;
  onSale?: boolean;
  sort: string;
}

export const DEFAULT_SHOP_FILTERS: ShopFilters = {
  search: '',
  sort: 'newest',
};

export function parseShopSearchParams(params: URLSearchParams): ShopFilters {
  const search = params.get('search') || '';
  const categoryRaw = params.get('categoryId');
  const categoryId = categoryRaw ? Number(categoryRaw) : undefined;
  const saleFilter = params.get('onSale');
  const onSale =
    saleFilter === 'true' ? true : saleFilter === 'false' ? false : undefined;
  const sort = params.get('sort') || 'newest';

  return {
    search,
    categoryId: categoryId && !Number.isNaN(categoryId) ? categoryId : undefined,
    onSale,
    sort,
  };
}

export function filtersToSearchParams(filters: ShopFilters): URLSearchParams {
  const params = new URLSearchParams();
  const search = filters.search.trim();
  if (search) params.set('search', search);
  if (filters.categoryId) params.set('categoryId', String(filters.categoryId));
  if (filters.onSale === true) params.set('onSale', 'true');
  if (filters.onSale === false) params.set('onSale', 'false');
  if (filters.sort && filters.sort !== 'newest') params.set('sort', filters.sort);
  return params;
}

export function hasActiveShopFilters(filters: ShopFilters): boolean {
  return (
    filters.search.trim() !== '' ||
    !!filters.categoryId ||
    filters.onSale !== undefined ||
    filters.sort !== 'newest'
  );
}

export function buildShopPath(filters: ShopFilters = DEFAULT_SHOP_FILTERS): string {
  const params = filtersToSearchParams(filters);
  const qs = params.toString();
  return qs ? `/shop?${qs}` : '/shop';
}

export function mergeShopFilters(
  base: ShopFilters,
  updates: Partial<ShopFilters> & { categoryId?: number | null; onSale?: boolean | null },
): ShopFilters {
  const next: ShopFilters = { ...base, ...updates };
  if ('categoryId' in updates && (updates.categoryId === null || updates.categoryId === undefined)) {
    delete next.categoryId;
  }
  if ('onSale' in updates && updates.onSale === null) {
    delete next.onSale;
  }
  return next;
}
