import { useMemo, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Plus, Search, Trash2, ShoppingBag } from 'lucide-react';
import { api, effectivePrice, toyPrimaryImage, type ToyListItem } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input, Textarea } from '@/components/ui/Input';
import { formatPrice, getDeliveryCharge, placeholderImage } from '@/lib/utils';

export function AdminCreateOrderPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState({ name: '', whatsapp: '', city: '', address: '' });
  const [selectedToys, setSelectedToys] = useState<ToyListItem[]>([]);
  const [toySearch, setToySearch] = useState('');
  const [error, setError] = useState('');

  const { data: toys, isLoading } = useQuery({
    queryKey: ['admin-toys'],
    queryFn: api.adminGetToys,
  });

  const availableToys = useMemo(() => {
    const selectedIds = new Set(selectedToys.map((t) => t.id));
    return (toys ?? [])
      .filter((t) => !t.isSold && !selectedIds.has(t.id))
      .filter((t) => {
        const q = toySearch.trim().toLowerCase();
        if (!q) return true;
        return t.name.toLowerCase().includes(q) || t.categoryName.toLowerCase().includes(q);
      });
  }, [toys, selectedToys, toySearch]);

  const subTotal = selectedToys.reduce((sum, t) => sum + effectivePrice(t), 0);
  const deliveryCharge = form.city.trim() ? getDeliveryCharge(form.city) : 0;
  const total = subTotal + (form.city.trim() ? deliveryCharge : 0);

  const createMutation = useMutation({
    mutationFn: () =>
      api.adminCreateOrder({
        ...form,
        toyIds: selectedToys.map((t) => t.id),
      }),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
      queryClient.invalidateQueries({ queryKey: ['admin-toys'] });
      navigate('/admin/orders', {
        state: { createdOrderNumber: result.orderNumber, createdTotal: result.total },
      });
    },
    onError: (err: Error) => setError(err.message),
  });

  const addToy = (toy: ToyListItem) => {
    setSelectedToys((prev) => [...prev, toy]);
    setError('');
  };

  const removeToy = (toyId: number) => {
    setSelectedToys((prev) => prev.filter((t) => t.id !== toyId));
  };

  const canSubmit =
    form.name.trim() &&
    form.whatsapp.trim() &&
    form.city.trim() &&
    form.address.trim() &&
    selectedToys.length > 0;

  return (
    <div className="w-full max-w-full overflow-x-hidden">
      <div className="flex items-center gap-3 mb-4 md:mb-6">
        <Link
          to="/admin/orders"
          className="p-2 rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-50 shrink-0"
        >
          <ArrowLeft className="w-5 h-5" />
        </Link>
        <div className="min-w-0">
          <h1 className="text-xl md:text-2xl font-bold text-slate-800">Create Order</h1>
          <p className="text-sm text-slate-500">Place order on behalf of customer · starts as Pending</p>
        </div>
      </div>

      <div className="grid lg:grid-cols-5 gap-6">
        <div className="lg:col-span-3 space-y-4">
          <div className="bg-white rounded-2xl p-4 md:p-6 border border-slate-200 shadow-sm">
            <h2 className="font-semibold text-slate-800 mb-4">Customer Details</h2>
            <div className="grid sm:grid-cols-2 gap-4">
              <Input label="Full Name *" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
              <Input label="WhatsApp *" required value={form.whatsapp} onChange={(e) => setForm({ ...form, whatsapp: e.target.value })} placeholder="e.g. 03221234567" />
              <Input label="City *" required value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} placeholder="e.g. Karachi" className="sm:col-span-2" />
            </div>
            <div className="mt-4">
              <Textarea label="Delivery Address *" required rows={3} value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
            </div>
            {form.city.trim() && (
              <p className="text-sm text-brand-600 mt-3 font-medium">
                Delivery: {formatPrice(deliveryCharge)}
                {form.city.trim().toLowerCase() === 'karachi' ? ' (Karachi)' : ' (Other city)'}
              </p>
            )}
          </div>

          <div className="bg-white rounded-2xl p-4 md:p-6 border border-slate-200 shadow-sm">
            <div className="flex items-center justify-between gap-3 mb-4">
              <h2 className="font-semibold text-slate-800">Select Toys</h2>
              <span className="text-sm text-slate-500">{selectedToys.length} selected</span>
            </div>

            {selectedToys.length > 0 && (
              <div className="space-y-2 mb-4">
                {selectedToys.map((toy) => (
                  <div key={toy.id} className="flex items-center gap-3 p-3 rounded-xl bg-brand-50 border border-brand-100">
                    <img
                      src={toyPrimaryImage(toy) || placeholderImage(toy.name)}
                      alt=""
                      className="w-12 h-12 rounded-lg object-cover shrink-0"
                    />
                    <div className="min-w-0 flex-1">
                      <p className="font-medium text-slate-800 truncate">{toy.name}</p>
                      <p className="text-xs text-slate-500">{toy.categoryName}</p>
                    </div>
                    <p className="font-semibold text-brand-600 shrink-0">{formatPrice(effectivePrice(toy))}</p>
                    <button
                      type="button"
                      onClick={() => removeToy(toy.id)}
                      className="p-2 text-red-500 hover:bg-red-50 rounded-lg shrink-0"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                ))}
              </div>
            )}

            <div className="relative mb-3">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
              <input
                type="search"
                value={toySearch}
                onChange={(e) => setToySearch(e.target.value)}
                placeholder="Search available toys..."
                className="w-full pl-10 pr-4 py-3 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
              />
            </div>

            <div className="max-h-72 overflow-y-auto space-y-2 border border-slate-100 rounded-xl p-2">
              {isLoading ? (
                <p className="text-center text-slate-400 py-8 text-sm">Loading toys...</p>
              ) : availableToys.length === 0 ? (
                <p className="text-center text-slate-400 py-8 text-sm">
                  {toySearch.trim() ? 'No matching toys' : 'No available toys to add'}
                </p>
              ) : (
                availableToys.map((toy) => (
                  <button
                    key={toy.id}
                    type="button"
                    onClick={() => addToy(toy)}
                    className="w-full flex items-center gap-3 p-2.5 rounded-xl hover:bg-slate-50 text-left transition-colors"
                  >
                    <img
                      src={toyPrimaryImage(toy) || placeholderImage(toy.name)}
                      alt=""
                      className="w-11 h-11 rounded-lg object-cover shrink-0"
                    />
                    <div className="min-w-0 flex-1">
                      <p className="font-medium text-sm text-slate-800 truncate">{toy.name}</p>
                      <p className="text-xs text-slate-500">{toy.categoryName}</p>
                    </div>
                    <p className="text-sm font-semibold text-brand-600 shrink-0">{formatPrice(effectivePrice(toy))}</p>
                    <span className="p-1.5 rounded-lg bg-brand-100 text-brand-600 shrink-0">
                      <Plus className="w-4 h-4" />
                    </span>
                  </button>
                ))
              )}
            </div>
          </div>

          {error && <p className="text-red-500 text-sm bg-red-50 p-3 rounded-xl border border-red-100">{error}</p>}

          <Button
            size="lg"
            className="w-full lg:hidden"
            disabled={!canSubmit || createMutation.isPending}
            onClick={() => createMutation.mutate()}
          >
            {createMutation.isPending ? 'Creating Order...' : 'Create Pending Order'}
          </Button>
        </div>

        <div className="lg:col-span-2">
          <div className="bg-white rounded-2xl p-6 border border-slate-200 shadow-sm lg:sticky lg:top-24">
            <div className="flex items-center gap-2 mb-4">
              <ShoppingBag className="w-5 h-5 text-brand-600" />
              <h2 className="font-semibold text-slate-800">Order Summary</h2>
            </div>

            {selectedToys.length === 0 ? (
              <p className="text-sm text-slate-500 py-4">Add toys from the list to build the order.</p>
            ) : (
              <div className="space-y-2 text-sm mb-4">
                {selectedToys.map((toy) => (
                  <div key={toy.id} className="flex justify-between gap-2">
                    <span className="text-slate-600 truncate">{toy.name}</span>
                    <span className="font-medium shrink-0">{formatPrice(effectivePrice(toy))}</span>
                  </div>
                ))}
              </div>
            )}

            <div className="border-t border-slate-100 pt-4 space-y-2 text-sm">
              <div className="flex justify-between"><span>Subtotal</span><span>{formatPrice(subTotal)}</span></div>
              <div className="flex justify-between">
                <span>Delivery</span>
                <span>{form.city.trim() ? formatPrice(deliveryCharge) : '—'}</span>
              </div>
              <div className="flex justify-between text-lg font-bold text-slate-800 pt-2 border-t">
                <span>Total</span>
                <span>{form.city.trim() ? formatPrice(total) : formatPrice(subTotal)}</span>
              </div>
            </div>

            <p className="text-xs text-slate-500 mt-4 bg-slate-50 rounded-xl p-3">
              Order will be created with <span className="font-semibold text-yellow-700">Pending</span> status.
              Confirm later with advance &amp; discount like regular customer orders.
            </p>

            <Button
              size="lg"
              className="w-full mt-4 hidden lg:flex"
              disabled={!canSubmit || createMutation.isPending}
              onClick={() => createMutation.mutate()}
            >
              {createMutation.isPending ? 'Creating Order...' : 'Create Pending Order'}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
