import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ShopLayout } from '@/components/layout/ShopLayout';
import { AdminLayout } from '@/components/admin/AdminLayout';
import { AdminRoute } from '@/components/admin/AdminRoute';
import { HomePage } from '@/pages/shop/HomePage';
import { ShopPage } from '@/pages/shop/ShopPage';
import { CategoryPage } from '@/pages/shop/CategoryPage';
import { ProductPage } from '@/pages/shop/ProductPage';
import { CartPage } from '@/pages/shop/CartPage';
import { CheckoutPage } from '@/pages/shop/CheckoutPage';
import { OrderSuccessPage } from '@/pages/shop/OrderSuccessPage';
import { TrackOrderPage } from '@/pages/shop/TrackOrderPage';
import { ReviewsPage } from '@/pages/shop/ReviewsPage';
import { AdminLoginPage } from '@/pages/admin/AdminLoginPage';
import { AdminCategoriesPage } from '@/pages/admin/AdminCategoriesPage';
import { AdminToysPage } from '@/pages/admin/AdminToysPage';
import { AdminOrdersPage } from '@/pages/admin/AdminOrdersPage';
import { AdminReviewsPage } from '@/pages/admin/AdminReviewsPage';

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000, retry: 1 } },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route element={<ShopLayout />}>
            <Route index element={<HomePage />} />
            <Route path="shop" element={<ShopPage />} />
            <Route path="category/:id" element={<CategoryPage />} />
            <Route path="product/:id" element={<ProductPage />} />
            <Route path="cart" element={<CartPage />} />
            <Route path="checkout" element={<CheckoutPage />} />
            <Route path="order-success/:orderNumber" element={<OrderSuccessPage />} />
            <Route path="track-order" element={<TrackOrderPage />} />
            <Route path="reviews" element={<ReviewsPage />} />
          </Route>

          <Route path="admin/login" element={<AdminLoginPage />} />
          <Route path="admin" element={<AdminRoute />}>
            <Route element={<AdminLayout />}>
              <Route index element={<Navigate to="categories" replace />} />
              <Route path="categories" element={<AdminCategoriesPage />} />
              <Route path="toys" element={<AdminToysPage />} />
              <Route path="orders" element={<AdminOrdersPage />} />
              <Route path="reviews" element={<AdminReviewsPage />} />
            </Route>
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
