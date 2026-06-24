import { useMemo } from 'react';
import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import {
  DEFAULT_SHOP_FILTERS,
  buildShopPath,
  type ShopFilters,
  parseShopSearchParams,
  mergeShopFilters,
} from '@/lib/shopFilters';

interface ShopFiltersState {
  filters: ShopFilters;
  setFilters: (filters: ShopFilters) => void;
  patchFilters: (updates: Partial<ShopFilters>) => void;
  syncFromSearchParams: (params: URLSearchParams) => void;
  resetFilters: () => void;
}

export const useShopFiltersStore = create<ShopFiltersState>()(
  persist(
    (set, get) => ({
      filters: DEFAULT_SHOP_FILTERS,

      setFilters: (filters) => set({ filters }),

      patchFilters: (updates) =>
        set({ filters: mergeShopFilters(get().filters, updates) }),

      syncFromSearchParams: (params) => {
        set({ filters: parseShopSearchParams(params) });
      },

      resetFilters: () => set({ filters: DEFAULT_SHOP_FILTERS }),
    }),
    {
      name: 'kidsparadise-shop-filters',
      storage: createJSONStorage(() => sessionStorage),
      partialize: (state) => ({ filters: state.filters }),
    },
  ),
);

export function useShopPath(overrides?: Partial<ShopFilters>): string {
  const filters = useShopFiltersStore((s) => s.filters);
  const overrideCategoryId = overrides?.categoryId;
  const overrideSearch = overrides?.search;
  const overrideOnSale = overrides?.onSale;
  const overrideSort = overrides?.sort;

  return useMemo(() => {
    if (!overrides) return buildShopPath(filters);
    return buildShopPath(
      mergeShopFilters(filters, {
        ...(overrideSearch !== undefined ? { search: overrideSearch } : {}),
        ...(overrideCategoryId !== undefined ? { categoryId: overrideCategoryId } : {}),
        ...(overrideOnSale !== undefined ? { onSale: overrideOnSale } : {}),
        ...(overrideSort !== undefined ? { sort: overrideSort } : {}),
      }),
    );
  }, [filters, overrideCategoryId, overrideSearch, overrideOnSale, overrideSort]);
}

export function getShopPath(overrides?: Partial<ShopFilters>): string {
  const filters = useShopFiltersStore.getState().filters;
  return buildShopPath(overrides ? mergeShopFilters(filters, overrides) : filters);
}
