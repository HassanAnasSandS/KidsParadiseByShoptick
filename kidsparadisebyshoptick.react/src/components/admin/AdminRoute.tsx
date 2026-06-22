import { Navigate, Outlet } from 'react-router-dom';
import { isAdminLoggedIn } from '@/lib/adminAuth';

export function AdminRoute() {
  if (!isAdminLoggedIn()) return <Navigate to="/admin/login" replace />;
  return <Outlet />;
}
