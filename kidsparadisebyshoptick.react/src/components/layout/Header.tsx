import { Link, useNavigate } from 'react-router-dom';
import { ShoppingCart, Search, Menu, X } from 'lucide-react';
import { useState } from 'react';
import { useCartStore } from '@/store/cart';
import { useShopFiltersStore, useShopPath } from '@/store/shopFilters';
import { buildShopPath, mergeShopFilters } from '@/lib/shopFilters';
import { useAffiliateStore, withAffiliatePath } from '@/store/affiliate';
import { BrandName } from '@/components/ui/BrandName';
import { BrandLogo } from '@/components/ui/BrandLogo';

export function Header() {
  const totalItems = useCartStore((s) => s.totalItems());
  // Re-render nav links when affiliate code is captured from ?aff=.
  useAffiliateStore((s) => s.code);
  const shopPath = useShopPath();
  const patchFilters = useShopFiltersStore((s) => s.patchFilters);
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const [search, setSearch] = useState('');

  const homePath = withAffiliatePath('/');
  const reviewsPath = withAffiliatePath('/reviews');
  const trackPath = withAffiliatePath('/track-order');
  const cartPath = withAffiliatePath('/cart');
  const shopWithAff = withAffiliatePath(shopPath);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const term = search.trim();
    if (!term) return;

    const filters = mergeShopFilters(useShopFiltersStore.getState().filters, { search: term });
    patchFilters({ search: term });
    navigate(withAffiliatePath(buildShopPath(filters)));
    setMenuOpen(false);
  };

  return (
    <header className="sticky top-0 z-50 bg-white/90 backdrop-blur-md border-b border-brand-100 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6">
        <div className="flex items-center justify-between h-16 gap-4">
          <Link to={homePath} className="flex items-center gap-2.5 shrink-0 group">
            <BrandLogo
              size={40}
              className="w-10 h-10 shadow-md shadow-brand-500/30 group-hover:scale-105 transition-transform"
            />
            <div>
              <BrandName />
            </div>
          </Link>

          <form onSubmit={handleSearch} className="hidden md:flex flex-1 max-w-md mx-4">
            <div className="relative w-full">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
              <input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search toys..."
                className="w-full pl-10 pr-4 py-2.5 rounded-full border border-slate-200 bg-slate-50/80 focus:outline-none focus:ring-2 focus:ring-brand-400 focus:bg-white text-sm transition-all"
              />
            </div>
          </form>

          <nav className="hidden md:flex items-center gap-1 text-sm font-semibold">
            {[
              { to: homePath, label: 'Home' },
              { to: shopWithAff, label: 'Shop' },
              { to: reviewsPath, label: 'Reviews' },
              { to: trackPath, label: 'My Orders' },
            ].map(({ to, label }) => (
              <Link
                key={label}
                to={to}
                className="px-4 py-2 rounded-full text-slate-600 hover:text-brand-600 hover:bg-brand-50 transition-all"
              >
                {label}
              </Link>
            ))}
          </nav>

          <div className="flex items-center gap-2">
            <Link
              to={cartPath}
              className="relative p-2.5 rounded-full bg-brand-50 hover:bg-brand-100 text-brand-600 transition-colors"
            >
              <ShoppingCart className="w-5 h-5" />
              {totalItems > 0 && (
                <span className="absolute -top-0.5 -right-0.5 bg-accent-500 text-white text-[10px] font-bold w-5 h-5 rounded-full flex items-center justify-center shadow">
                  {totalItems}
                </span>
              )}
            </Link>
            <button
              className="md:hidden p-2.5 rounded-lg hover:bg-slate-100"
              onClick={() => setMenuOpen(!menuOpen)}
              aria-label={menuOpen ? 'Close menu' : 'Open menu'}
            >
              {menuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
            </button>
          </div>
        </div>

        {menuOpen && (
          <nav className="md:hidden pb-4 flex flex-col gap-1 border-t border-slate-100 pt-3">
            <form onSubmit={handleSearch} className="px-1 pb-2">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                <input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="Search toys..."
                  className="w-full pl-10 pr-4 py-2.5 rounded-full border border-slate-200 bg-slate-50/80 focus:outline-none focus:ring-2 focus:ring-brand-400 focus:bg-white text-base transition-all"
                />
              </div>
            </form>
            <Link to={homePath} className="px-3 py-2.5 rounded-xl hover:bg-brand-50 font-medium" onClick={() => setMenuOpen(false)}>Home</Link>
            <Link to={shopWithAff} className="px-3 py-2.5 rounded-xl hover:bg-brand-50 font-medium" onClick={() => setMenuOpen(false)}>Shop</Link>
            <Link to={reviewsPath} className="px-3 py-2.5 rounded-xl hover:bg-brand-50 font-medium" onClick={() => setMenuOpen(false)}>Reviews</Link>
            <Link to={trackPath} className="px-3 py-2.5 rounded-xl hover:bg-brand-50 font-medium" onClick={() => setMenuOpen(false)}>My Orders</Link>
          </nav>
        )}
      </div>
    </header>
  );
}
