import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { Footer } from './Footer';
import { WhatsAppButton } from './WhatsAppButton';
import { AffiliateCapture } from '@/components/AffiliateCapture';

export function ShopLayout() {
  return (
    <div className="flex flex-col min-h-screen">
      <AffiliateCapture />
      <Header />
      <main className="flex-1 pb-14 sm:pb-16">
        <Outlet />
      </main>
      <Footer />
      <WhatsAppButton />
    </div>
  );
}
