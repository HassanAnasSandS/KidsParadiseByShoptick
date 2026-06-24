import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ScrollToTop } from '@/components/ScrollToTop';
import { ShopLayout } from '@/components/layout/ShopLayout';
import { HomePage } from '@/pages/shop/HomePage';
import { ShopPage } from '@/pages/shop/ShopPage';
import { CategoryPage } from '@/pages/shop/CategoryPage';
import { ProductPage } from '@/pages/shop/ProductPage';
import { CartPage } from '@/pages/shop/CartPage';
import { CheckoutPage } from '@/pages/shop/CheckoutPage';
import { OrderSuccessPage } from '@/pages/shop/OrderSuccessPage';
import { TrackOrderPage } from '@/pages/shop/TrackOrderPage';
import { ReviewsPage } from '@/pages/shop/ReviewsPage';
import { AboutPage } from '@/pages/shop/AboutPage';
import { ContactPage } from '@/pages/shop/ContactPage';
import { PrivacyPolicyPage } from '@/pages/shop/PrivacyPolicyPage';

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000, retry: 1 } },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ScrollToTop />
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
            <Route path="about" element={<AboutPage />} />
            <Route path="contact" element={<ContactPage />} />
            <Route path="privacy-policy" element={<PrivacyPolicyPage />} />
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
