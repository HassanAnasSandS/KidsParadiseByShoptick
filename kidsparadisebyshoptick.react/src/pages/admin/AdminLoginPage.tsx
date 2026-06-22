import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Lock } from 'lucide-react';
import { api } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input, PasswordInput } from '@/components/ui/Input';
import { BrandName } from '@/components/ui/BrandName';
import {
  getAdminRememberMe,
  getRememberedUsername,
  isAdminLoggedIn,
  setAdminToken,
  setRememberedUsername,
} from '@/lib/adminAuth';

export function AdminLoginPage() {
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(true);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isAdminLoggedIn()) {
      navigate('/admin/categories', { replace: true });
      return;
    }
    setRememberMe(getAdminRememberMe());
    setUsername(getRememberedUsername());
  }, [navigate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      const res = await api.adminLogin(username, password, rememberMe);
      setAdminToken(res.token, rememberMe);
      setRememberedUsername(username.trim(), rememberMe);
      navigate('/admin/categories');
    } catch {
      setError('Invalid username or password');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-[100dvh] bg-gradient-to-br from-slate-900 to-brand-900 flex items-center justify-center p-4 py-8 safe-area-pt safe-area-pb">
      <div className="bg-white rounded-2xl shadow-2xl p-6 sm:p-8 w-full max-w-md animate-fade-in">
        <div className="text-center mb-8">
          <div className="flex justify-center mb-4">
            <BrandName variant="muted" className="items-center" />
          </div>
          <div className="w-14 h-14 bg-brand-100 rounded-2xl flex items-center justify-center mx-auto mb-4">
            <Lock className="w-7 h-7 text-brand-600" />
          </div>
          <h1 className="text-2xl font-bold text-slate-800">Admin Login</h1>
          <p className="text-slate-500 text-sm mt-1">Management Portal</p>
        </div>
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input label="Username" value={username} onChange={(e) => setUsername(e.target.value)} required autoComplete="username" />
          <PasswordInput label="Password" value={password} onChange={(e) => setPassword(e.target.value)} required autoComplete="current-password" />
          <label className="flex items-center gap-2.5 cursor-pointer select-none">
            <input
              type="checkbox"
              checked={rememberMe}
              onChange={(e) => setRememberMe(e.target.checked)}
              className="w-4 h-4 rounded border-slate-300 text-brand-600 focus:ring-brand-400"
            />
            <span className="text-sm text-slate-600">Remember me for 30 days</span>
          </label>
          {error && <p className="text-red-500 text-sm">{error}</p>}
          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? 'Signing in...' : 'Sign In'}
          </Button>
        </form>
        <p className="text-xs text-slate-400 text-center mt-4">Contact your administrator for access.</p>
      </div>
    </div>
  );
}
