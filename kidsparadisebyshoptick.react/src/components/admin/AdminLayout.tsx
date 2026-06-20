import { Link, Outlet, useNavigate, useLocation } from 'react-router-dom';
import { Package, Tags, ShoppingBag, LogOut, Menu, X, Store, Star, ImageIcon } from 'lucide-react';
import { useState, useEffect } from 'react';
import { BrandName } from '@/components/ui/BrandName';

const nav = [
  { to: '/admin/categories', label: 'Categories', icon: Tags },
  { to: '/admin/toys', label: 'Toys', icon: Package },
  { to: '/admin/orders', label: 'Orders', icon: ShoppingBag },
  { to: '/admin/reviews', label: 'Reviews', icon: Star },
  { to: '/admin/customization', label: 'Images', icon: ImageIcon },
];

export function AdminLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const currentPage =
    nav.find((n) => n.to === location.pathname)?.label
    ?? (location.pathname.startsWith('/admin/orders') ? 'Orders' : 'Admin');

  useEffect(() => {
    setSidebarOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    document.body.classList.add('admin-mode');
    return () => document.body.classList.remove('admin-mode');
  }, []);

  useEffect(() => {
    if (sidebarOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => { document.body.style.overflow = ''; };
  }, [sidebarOpen]);

  const logout = () => {
    localStorage.removeItem('adminToken');
    navigate('/admin/login');
  };

  return (
    <div className="admin-shell min-h-[100dvh] w-full max-w-[100vw] overflow-x-hidden bg-slate-100 flex flex-col md:flex-row">
      {sidebarOpen && (
        <button
          type="button"
          className="fixed inset-0 bg-black/50 z-40 md:hidden"
          onClick={() => setSidebarOpen(false)}
          aria-label="Close menu"
        />
      )}

      <aside
        className={`fixed md:static inset-y-0 left-0 z-50 w-[min(280px,88vw)] bg-slate-900 text-white flex flex-col shrink-0 transform transition-transform duration-300 ease-out md:translate-x-0 ${
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        <div className="p-4 border-b border-slate-800 flex items-center justify-between safe-area-pt">
          <div className="flex items-center gap-2.5 min-w-0">
            <div className="w-9 h-9 rounded-xl bg-brand-600 flex items-center justify-center shrink-0 text-lg">🧸</div>
            <div className="min-w-0">
              <BrandName variant="admin" />
              <p className="text-[10px] text-slate-500 mt-1">Admin Panel</p>
            </div>
          </div>
          <button type="button" className="md:hidden p-2 rounded-lg hover:bg-slate-800 shrink-0" onClick={() => setSidebarOpen(false)}>
            <X className="w-5 h-5" />
          </button>
        </div>

        <nav className="flex-1 p-3 space-y-1 overflow-y-auto">
          {nav.map(({ to, label, icon: Icon }) => {
              const active = location.pathname === to || (to === '/admin/orders' && location.pathname.startsWith('/admin/orders'));
              return (
                <Link
                  key={to}
                  to={to}
                  className={`flex items-center gap-3 px-4 py-3.5 rounded-xl text-sm font-medium transition-colors ${
                    active ? 'bg-brand-600 text-white' : 'text-slate-300 active:bg-slate-800'
                  }`}
                >
              <Icon className="w-5 h-5 shrink-0" />
              <span>{label}</span>
            </Link>
              );
            })}
        </nav>

        <div className="p-3 border-t border-slate-800 safe-area-pb">
          <Link to="/" className="flex items-center gap-2 px-4 py-3 text-sm text-slate-400 hover:text-white rounded-xl hover:bg-slate-800">
            <Store className="w-4 h-4 shrink-0" /> View Store
          </Link>
          <button
            type="button"
            onClick={logout}
            className="flex items-center gap-3 px-4 py-3.5 rounded-xl text-sm text-red-400 hover:bg-slate-800 w-full mt-1"
          >
            <LogOut className="w-5 h-5 shrink-0" /> Logout
          </button>
        </div>
      </aside>

      <div className="flex-1 flex flex-col min-w-0 w-full">
        <header className="md:hidden sticky top-0 z-30 bg-white border-b border-slate-200 px-3 py-2.5 flex items-center gap-2 shadow-sm safe-area-pt">
          <button
            type="button"
            onClick={() => setSidebarOpen(true)}
            className="p-2.5 rounded-xl hover:bg-slate-100 active:bg-slate-200 shrink-0"
            aria-label="Open menu"
          >
            <Menu className="w-5 h-5 text-slate-700" />
          </button>
          <h1 className="font-bold text-slate-800 truncate flex-1 text-base">{currentPage}</h1>
          <button
            type="button"
            onClick={logout}
            className="p-2.5 rounded-xl text-red-500 hover:bg-red-50 shrink-0"
            aria-label="Logout"
          >
            <LogOut className="w-5 h-5" />
          </button>
        </header>

        <main className="flex-1 w-full max-w-full overflow-x-hidden p-3 sm:p-4 md:p-6 pb-[5.5rem] md:pb-6">
          <Outlet />
        </main>

        <nav className="md:hidden fixed bottom-0 left-0 right-0 z-30 bg-white/95 backdrop-blur-md border-t border-slate-200 shadow-[0_-4px_24px_rgba(0,0,0,0.08)]">
          <div className="flex safe-area-pb">
            {nav.map(({ to, label, icon: Icon }) => {
              const active = location.pathname === to || (to === '/admin/orders' && location.pathname.startsWith('/admin/orders'));
              return (
                <Link
                  key={to}
                  to={to}
                  className={`flex-1 flex flex-col items-center justify-center gap-0.5 py-2 min-h-[56px] text-[11px] font-semibold transition-colors active:scale-95 ${
                    active ? 'text-brand-600' : 'text-slate-500'
                  }`}
                >
                  <Icon className={`w-5 h-5 ${active ? 'text-brand-600' : 'text-slate-400'}`} />
                  <span>{label}</span>
                </Link>
              );
            })}
          </div>
        </nav>
      </div>
    </div>
  );
}
