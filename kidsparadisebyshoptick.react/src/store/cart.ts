import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export interface CartItem {
  toyId: number;
  name: string;
  price: number;
  salePrice: number | null;
  imageUrl: string | null;
}

interface CartState {
  items: CartItem[];
  addItem: (item: CartItem) => void;
  removeItem: (toyId: number) => void;
  clearCart: () => void;
  totalItems: () => number;
  subTotal: () => number;
}

export const useCartStore = create<CartState>()(
  persist(
    (set, get) => ({
      items: [],
      addItem: (item) => {
        set((state) => {
          if (state.items.some((i) => i.toyId === item.toyId)) return state;
          return { items: [...state.items, item] };
        });
      },
      removeItem: (toyId) => set((state) => ({ items: state.items.filter((i) => i.toyId !== toyId) })),
      clearCart: () => set({ items: [] }),
      totalItems: () => get().items.length,
      subTotal: () => get().items.reduce((sum, i) => sum + (i.salePrice ?? i.price), 0),
    }),
    { name: 'kids-paradise-cart' }
  )
);
