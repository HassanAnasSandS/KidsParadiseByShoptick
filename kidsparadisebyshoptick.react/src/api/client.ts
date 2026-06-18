const API_BASE = '/api';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const token = localStorage.getItem('adminToken');
  const headers: Record<string, string> = {
    ...(options?.headers as Record<string, string>),
  };

  if (options?.body && !(options.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json';
  }

  if (token && url.includes('/admin/')) {
    headers['Authorization'] = `Bearer ${token}`;
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
  averageRating: number | null;
  reviewCount: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
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
  balanceAmount: number;
  city: string;
  address: string;
  phone: string;
  whatsapp: string;
  trackingNumber: string | null;
  customerName: string;
  customerEmail: string;
  createdAt: string;
  items: OrderItem[];
}

export const api = {
  getCategories: () => request<Category[]>('/categories'),
  getCategory: (id: number) => request<Category & { toys: ToyListItem[] }>(`/categories/${id}`),
  getToys: (params?: { categoryId?: number; search?: string; onSale?: boolean; sort?: string; page?: number }) => {
    const q = new URLSearchParams();
    if (params?.categoryId) q.set('categoryId', String(params.categoryId));
    if (params?.search) q.set('search', params.search);
    if (params?.onSale === true) q.set('onSale', 'true');
    else if (params?.onSale === false) q.set('onSale', 'false');
    if (params?.sort) q.set('sort', params.sort);
    if (params?.page) q.set('page', String(params.page));
    return request<PagedResult<ToyListItem>>(`/toys?${q}`);
  },
  getLatestToys: () => request<ToyListItem[]>('/toys/latest'),
  getToy: (id: number) => request<ToyDetail>(`/toys/${id}`),
  placeOrder: (data: {
    email: string;
    name: string;
    phone: string;
    whatsapp: string;
    city: string;
    address: string;
    toyIds: number[];
  }) => request<{ orderNumber: string; total: number; deliveryCharge: number }>('/orders', {
    method: 'POST',
    body: JSON.stringify(data),
  }),
  trackOrdersByEmail: (email: string) =>
    request<Order[]>(`/orders/track?email=${encodeURIComponent(email)}`),
  getDeliveryCharge: (city: string) =>
    request<{ deliveryCharge: number }>(`/orders/delivery-charge?city=${encodeURIComponent(city)}`),
  getAllReviews: () => request<Review[]>('/reviews'),
  getPendingReviews: (email: string) =>
    request<PendingReview[]>(`/reviews/pending?email=${encodeURIComponent(email)}`),
  uploadReviewImage: async (file: File) => {
    const form = new FormData();
    form.append('file', file);
    return request<{ path: string; url: string }>('/reviews/upload', { method: 'POST', body: form });
  },
  createReview: (data: {
    email: string;
    orderId: number;
    toyId: number;
    reviewerName: string;
    rating: number;
    comment: string;
    imagePath?: string;
  }) => request<Review>('/reviews', { method: 'POST', body: JSON.stringify(data) }),

  adminLogin: (username: string, password: string) =>
    request<{ token: string; username: string }>('/admin/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),
  adminGetCategories: () => request<Category[]>('/admin/categories'),
  adminCreateCategory: (data: { name: string; imagePath?: string }) =>
    request<Category>('/admin/categories', { method: 'POST', body: JSON.stringify(data) }),
  adminUpdateCategory: (id: number, data: { name: string; imagePath?: string }) =>
    request<Category>(`/admin/categories/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  adminDeleteCategory: (id: number) => request<void>(`/admin/categories/${id}`, { method: 'DELETE' }),
  adminGetToys: () => request<ToyListItem[]>('/admin/toys'),
  adminGetToy: (id: number) => request<ToyDetail>(`/admin/toys/${id}`),
  adminCreateToy: (data: Record<string, unknown>) =>
    request<ToyListItem>('/admin/toys', { method: 'POST', body: JSON.stringify(data) }),
  adminUpdateToy: (id: number, data: Record<string, unknown>) =>
    request<ToyListItem>(`/admin/toys/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  adminDeleteToy: (id: number) => request<void>(`/admin/toys/${id}`, { method: 'DELETE' }),
  adminGetOrders: () => request<Order[]>('/admin/orders'),
  adminUpdateOrderStatus: (
    id: number,
    status: string,
    options?: { trackingNumber?: string; advanceAmount?: number }
  ) =>
    request<Order>(`/admin/orders/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({
        status,
        trackingNumber: options?.trackingNumber,
        advanceAmount: options?.advanceAmount,
      }),
    }),
  adminGetReviews: () => request<Review[]>('/admin/reviews'),
  adminUpdateReview: (id: number, data: { reviewerName: string; rating: number; comment: string; imagePath?: string }) =>
    request<Review>(`/admin/reviews/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  adminUpload: async (file: File, folder: string) => {
    const form = new FormData();
    form.append('file', file);
    return request<{ path: string; url: string }>(`/admin/upload?folder=${folder}`, { method: 'POST', body: form });
  },
};

export function toyPrimaryImage(toy: { imageUrls: string[]; name: string }) {
  return toy.imageUrls[0] ?? null;
}

export function effectivePrice(toy: { price: number; salePrice: number | null }) {
  return toy.salePrice ?? toy.price;
}
