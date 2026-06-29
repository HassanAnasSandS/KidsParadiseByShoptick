import { sortCategoriesByName } from '@/lib/utils';

const API_BASE = '/api';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const headers: Record<string, string> = {
    ...(options?.headers as Record<string, string>),
  };

  if (options?.body && !(options.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json';
  }

  const res = await fetch(`${API_BASE}${url}`, { ...options, headers });

  if (!res.ok) {
    const err = await res.json().catch(() => ({ message: 'Request failed' }));
    throw new Error(err.message || 'Request failed');
  }

  if (res.status === 204) return undefined as T;
  return res.json();
}

export interface Category {
  id: number;
  name: string;
  imageUrl: string | null;
  imagePath: string | null;
  toyCount: number;
}

export interface ToyListItem {
  id: number;
  name: string;
  price: number;
  salePrice: number | null;
  isSold: boolean;
  imageUrls: string[];
  categoryName: string;
  averageRating: number | null;
}

export interface Review {
  id: number;
  reviewerName: string;
  rating: number;
  comment: string;
  imageUrl: string | null;
  imagePath: string | null;
  toyName: string;
  toyId: number;
  orderNumber: string;
  createdAt: string;
}

export interface PendingReview {
  orderId: number;
  orderNumber: string;
  toyId: number;
  toyName: string;
  toyImageUrl: string | null;
}

export interface ToyDetail extends ToyListItem {
  categoryId: number;
  imagePaths: string[];
  averageRating: number | null;
  reviewCount: number;
  videoLink: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CategoryDetail {
  id: number;
  name: string;
  imageUrl: string | null;
  availableToyCount: number;
}

type PagedParams = {
  page?: number;
  pageSize?: number;
  search?: string;
};

function buildQuery(params: Record<string, string | number | boolean | undefined | null>) {
  const q = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '')
      q.set(key, String(value));
  });
  const query = q.toString();
  return query ? `?${query}` : '';
}

export interface OrderItem {
  toyId: number;
  toyName: string;
  price: number;
  imageUrl: string | null;
}

export interface Order {
  id: number;
  orderNumber: string;
  status: string;
  subTotal: number;
  deliveryCharge: number;
  total: number;
  advanceAmount: number | null;
  discountAmount: number | null;
  balanceAmount: number;
  city: string;
  address: string;
  whatsapp: string;
  trackingNumber: string | null;
  customerName: string;
  createdAt: string;
  items: OrderItem[];
}

export const api = {
  getCategories: async (params?: PagedParams) => {
    const result = await request<PagedResult<Category>>(
      `/categories${buildQuery({ page: params?.page ?? 1, pageSize: params?.pageSize ?? 50 })}`
    );
    return { ...result, items: sortCategoriesByName(result.items) };
  },
  getCategory: (id: number) => request<CategoryDetail>(`/categories/${id}`),
  getToys: (params?: { categoryId?: number; search?: string; onSale?: boolean; sort?: string; page?: number; pageSize?: number }) => {
    const q = new URLSearchParams();
    if (params?.categoryId) q.set('categoryId', String(params.categoryId));
    if (params?.search) q.set('search', params.search);
    if (params?.onSale === true) q.set('onSale', 'true');
    else if (params?.onSale === false) q.set('onSale', 'false');
    if (params?.sort) q.set('sort', params.sort);
    if (params?.page) q.set('page', String(params.page));
    if (params?.pageSize) q.set('pageSize', String(params.pageSize));
    return request<PagedResult<ToyListItem>>(`/toys?${q}`);
  },
  getLatestToys: (page = 1, pageSize = 8) =>
    request<PagedResult<ToyListItem>>(`/toys/latest${buildQuery({ page, pageSize })}`),
  getToy: (id: number) => request<ToyDetail>(`/toys/${id}`),
  placeOrder: (data: {
    name: string;
    whatsapp: string;
    city: string;
    address: string;
    toyIds: number[];
  }) => request<{ orderNumber: string; total: number; deliveryCharge: number }>('/orders', {
    method: 'POST',
    body: JSON.stringify(data),
  }),
  trackOrdersByWhatsapp: (whatsapp: string, page = 1, pageSize = 20) =>
    request<PagedResult<Order>>(
      `/orders/track${buildQuery({ whatsapp, page, pageSize })}`
    ),
  getDeliveryCharge: (city: string) =>
    request<{ deliveryCharge: number }>(`/orders/delivery-charge?city=${encodeURIComponent(city)}`),
  getAllReviews: (params?: PagedParams) =>
    request<PagedResult<Review>>(`/reviews${buildQuery({ page: params?.page ?? 1, pageSize: params?.pageSize ?? 20, search: params?.search })}`),
  getReviewsByToy: (toyId: number, params?: PagedParams) =>
    request<PagedResult<Review>>(
      `/reviews/toy/${toyId}${buildQuery({ page: params?.page ?? 1, pageSize: params?.pageSize ?? 20 })}`
    ),
  getPendingReviews: (whatsapp: string, page = 1, pageSize = 20) =>
    request<PagedResult<PendingReview>>(
      `/reviews/pending${buildQuery({ whatsapp, page, pageSize })}`
    ),
  uploadReviewImage: async (file: File) => {
    const form = new FormData();
    form.append('file', file);
    return request<{ path: string; url: string }>('/reviews/upload', { method: 'POST', body: form });
  },
  createReview: (data: {
    whatsapp: string;
    orderId: number;
    toyId: number;
    reviewerName: string;
    rating: number;
    comment: string;
    imagePath?: string;
  }) => request<Review>('/reviews', { method: 'POST', body: JSON.stringify(data) }),

  getSiteImages: () => request<Record<string, string>>('/site-images'),
};

export function toyPrimaryImage(toy: { imageUrls: string[]; name: string }) {
  return toy.imageUrls[0] ?? null;
}

export function effectivePrice(toy: { price: number; salePrice: number | null }) {
  return toy.salePrice ?? toy.price;
}
