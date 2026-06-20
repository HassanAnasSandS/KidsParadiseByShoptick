import { useMemo, useState, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Search, X, ChevronDown, ChevronUp, Package, Filter, Plus, Pencil } from 'lucide-react';
import { api } from '@/api/client';
import type { Order } from '@/api/client';
import { Input, Select } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { formatPrice } from '@/lib/utils';

const statuses = ['Pending', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'] as const;

const statusColors: Record<string, string> = {
  Pending: 'bg-yellow-100 text-yellow-700',
  Confirmed: 'bg-blue-100 text-blue-700',
  Shipped: 'bg-purple-100 text-purple-700',
  Delivered: 'bg-green-100 text-green-700',
  Cancelled: 'bg-red-100 text-red-700',
};

const statusPillActive: Record<string, string> = {
  All: 'bg-slate-800 text-white',
  Pending: 'bg-yellow-500 text-white',
  Confirmed: 'bg-blue-500 text-white',
  Shipped: 'bg-purple-500 text-white',
  Delivered: 'bg-green-500 text-white',
  Cancelled: 'bg-red-500 text-white',
};

function filterOrders(
  orders: Order[],
  statusFilter: string,
  search: string,
  cityFilter: string,
  dateFrom: string,
  dateTo: string,
  sort: 'newest' | 'oldest'
) {
  let result = [...orders];

  if (statusFilter !== 'All') {
    result = result.filter((o) => o.status === statusFilter);
  }

  if (cityFilter !== 'All') {
    result = result.filter((o) => o.city === cityFilter);
  }

  const q = search.trim().toLowerCase();
  if (q) {
    result = result.filter(
      (o) =>
        o.orderNumber.toLowerCase().includes(q) ||
        o.customerName.toLowerCase().includes(q) ||
        o.whatsapp.includes(q) ||
        o.whatsapp.includes(q) ||
        (o.trackingNumber?.toLowerCase().includes(q) ?? false)
    );
  }

  if (dateFrom) {
    const from = new Date(dateFrom);
    from.setHours(0, 0, 0, 0);
    result = result.filter((o) => new Date(o.createdAt) >= from);
  }

  if (dateTo) {
    const to = new Date(dateTo);
    to.setHours(23, 59, 59, 999);
    result = result.filter((o) => new Date(o.createdAt) <= to);
  }

  result.sort((a, b) => {
    const da = new Date(a.createdAt).getTime();
    const db = new Date(b.createdAt).getTime();
    return sort === 'newest' ? db - da : da - db;
  });

  return result;
}

export function AdminOrdersPage() {
  const location = useLocation();
  const queryClient = useQueryClient();
  const created = location.state as { createdOrderNumber?: string; createdTotal?: number; editedOrderNumber?: string } | null;
  const [successMsg, setSuccessMsg] = useState('');
  const [trackingInputs, setTrackingInputs] = useState<Record<number, string>>({});
  const [advanceInputs, setAdvanceInputs] = useState<Record<number, string>>({});
  const [discountInputs, setDiscountInputs] = useState<Record<number, string>>({});
  const [statusFilter, setStatusFilter] = useState('All');
  const [search, setSearch] = useState('');
  const [cityFilter, setCityFilter] = useState('All');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [sort, setSort] = useState<'newest' | 'oldest'>('newest');
  const [showFilters, setShowFilters] = useState(false);
  const [expandedId, setExpandedId] = useState<number | null>(null);

  const { data: orders, isLoading } = useQuery({
    queryKey: ['admin-orders'],
    queryFn: api.adminGetOrders,
  });

  const updateMutation = useMutation({
    mutationFn: ({
      id,
      status,
      trackingNumber,
      advanceAmount,
      discountAmount,
    }: {
      id: number;
      status: string;
      trackingNumber?: string;
      advanceAmount?: number;
      discountAmount?: number;
    }) => api.adminUpdateOrderStatus(id, status, { trackingNumber, advanceAmount, discountAmount }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-orders'] }),
  });

  const parseAdvance = (orderId: number, order: Order) => {
    const raw = advanceInputs[orderId] ?? (order.advanceAmount != null ? String(order.advanceAmount) : '');
    const value = raw.trim() === '' ? 0 : Number(raw);
    return Number.isFinite(value) ? value : 0;
  };

  const parseDiscount = (orderId: number, order: Order) => {
    const raw = discountInputs[orderId] ?? (order.discountAmount != null ? String(order.discountAmount) : '');
    const value = raw.trim() === '' ? 0 : Number(raw);
    return Number.isFinite(value) ? value : 0;
  };

  const paymentDetails = (order: Order) =>
    order.advanceAmount != null || order.discountAmount != null;

  const savePaymentDetails = (order: Order) => {
    updateMutation.mutate({
      id: order.id,
      status: 'Confirmed',
      advanceAmount: parseAdvance(order.id, order),
      discountAmount: parseDiscount(order.id, order),
    });
  };

  const showPaymentDetails = (status: string) =>
    status === 'Confirmed' || status === 'Shipped' || status === 'Delivered';

  const statusCounts = useMemo(() => {
    const counts: Record<string, number> = { All: orders?.length ?? 0 };
    statuses.forEach((s) => {
      counts[s] = orders?.filter((o) => o.status === s).length ?? 0;
    });
    return counts;
  }, [orders]);

  const cities = useMemo(() => {
    const unique = new Set(orders?.map((o) => o.city) ?? []);
    return ['All', ...Array.from(unique).sort()];
  }, [orders]);

  const filteredOrders = useMemo(
    () => (orders ? filterOrders(orders, statusFilter, search, cityFilter, dateFrom, dateTo, sort) : []),
    [orders, statusFilter, search, cityFilter, dateFrom, dateTo, sort]
  );

  const hasActiveFilters =
    statusFilter !== 'All' ||
    search.trim() !== '' ||
    cityFilter !== 'All' ||
    dateFrom !== '' ||
    dateTo !== '' ||
    sort !== 'newest';

  const clearFilters = () => {
    setStatusFilter('All');
    setSearch('');
    setCityFilter('All');
    setDateFrom('');
    setDateTo('');
    setSort('newest');
  };

  const toggleExpand = (id: number) => {
    setExpandedId((prev) => (prev === id ? null : id));
  };

  useEffect(() => {
    if (created?.createdOrderNumber) {
      setSuccessMsg(`Order ${created.createdOrderNumber} created (Pending) — ${formatPrice(created.createdTotal ?? 0)}`);
      setStatusFilter('Pending');
      window.history.replaceState({}, document.title);
    } else if (created?.editedOrderNumber) {
      setSuccessMsg(`Order ${created.editedOrderNumber} updated successfully`);
      window.history.replaceState({}, document.title);
    }
  }, [created?.createdOrderNumber, created?.createdTotal, created?.editedOrderNumber]);

  return (
    <div className="w-full max-w-full overflow-x-hidden">
      <div className="hidden md:flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-slate-800">Orders</h1>
        <div className="flex items-center gap-3">
          <p className="text-sm text-slate-500">{orders?.length ?? 0} total orders</p>
          <Link to="/admin/orders/create">
            <Button><Plus className="w-4 h-4" /> Create Order</Button>
          </Link>
        </div>
      </div>

      <div className="md:hidden fixed bottom-[4.5rem] right-4 z-40">
        <Link to="/admin/orders/create">
          <Button className="rounded-full shadow-lg shadow-brand-500/30 px-5">
            <Plus className="w-5 h-5" />
          </Button>
        </Link>
      </div>

      {successMsg && (
        <div className="mb-4 flex items-center justify-between gap-3 bg-green-50 border border-green-100 text-green-800 rounded-xl px-4 py-3 text-sm">
          <span>{successMsg}</span>
          <button type="button" onClick={() => setSuccessMsg('')} className="text-green-600 hover:text-green-800 shrink-0">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Status summary */}
      <div className="grid grid-cols-3 sm:grid-cols-6 gap-2 mb-4">
        {(['All', ...statuses] as const).map((status) => (
          <button
            key={status}
            type="button"
            onClick={() => setStatusFilter(status)}
            className={`rounded-xl p-2.5 text-center border transition-all ${
              statusFilter === status
                ? `${statusPillActive[status]} border-transparent shadow-sm`
                : 'bg-white border-slate-200 text-slate-600 hover:border-brand-300'
            }`}
          >
            <p className="text-lg font-bold leading-none">{statusCounts[status]}</p>
            <p className="text-[10px] sm:text-xs mt-1 font-medium truncate">{status}</p>
          </button>
        ))}
      </div>

      {/* Search & filters */}
      <div className="bg-white rounded-2xl border border-slate-200 p-4 mb-4 shadow-sm">
        <div className="flex gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search order #, customer, WhatsApp..."
              className="w-full pl-10 pr-4 py-3 rounded-xl border border-slate-200 text-base md:text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            />
          </div>
          <button
            type="button"
            onClick={() => setShowFilters((v) => !v)}
            className={`shrink-0 px-3 py-2 rounded-xl border flex items-center gap-1.5 text-sm font-medium ${
              showFilters || hasActiveFilters ? 'border-brand-400 bg-brand-50 text-brand-700' : 'border-slate-200 text-slate-600'
            }`}
          >
            <Filter className="w-4 h-4" />
            <span className="hidden sm:inline">Filters</span>
          </button>
        </div>

        {showFilters && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mt-4 pt-4 border-t border-slate-100">
            <Select
              label="City"
              options={cities.map((c) => ({ value: c, label: c === 'All' ? 'All Cities' : c }))}
              value={cityFilter}
              onChange={(e) => setCityFilter(e.target.value)}
            />
            <Select
              label="Sort By"
              options={[
                { value: 'newest', label: 'Newest First' },
                { value: 'oldest', label: 'Oldest First' },
              ]}
              value={sort}
              onChange={(e) => setSort(e.target.value as 'newest' | 'oldest')}
            />
            <Input label="From Date" type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
            <Input label="To Date" type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
          </div>
        )}

        <div className="flex flex-wrap items-center justify-between gap-2 mt-3">
          <p className="text-sm text-slate-500">
            Showing <span className="font-semibold text-slate-700">{filteredOrders.length}</span>
            {orders ? ` of ${orders.length}` : ''} orders
          </p>
          {hasActiveFilters && (
            <button type="button" onClick={clearFilters} className="text-sm text-brand-600 font-medium flex items-center gap-1 hover:underline">
              <X className="w-3.5 h-3.5" /> Clear filters
            </button>
          )}
        </div>
      </div>

      {/* Order list */}
      <div className="space-y-3">
        {isLoading ? (
          <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">Loading...</div>
        ) : orders?.length === 0 ? (
          <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">No orders yet</div>
        ) : filteredOrders.length === 0 ? (
          <div className="bg-white rounded-2xl p-8 text-center border">
            <p className="text-slate-500">No orders match your filters</p>
            <Button variant="ghost" size="sm" className="mt-3" onClick={clearFilters}>Clear filters</Button>
          </div>
        ) : (
          filteredOrders.map((order) => {
            const expanded = expandedId === order.id;
            return (
              <div key={order.id} className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
                <button
                  type="button"
                  onClick={() => toggleExpand(order.id)}
                  className="w-full p-4 text-left hover:bg-slate-50/80 transition-colors"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <p className="font-bold text-brand-600 break-all">{order.orderNumber}</p>
                        <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${statusColors[order.status]}`}>
                          {order.status}
                        </span>
                      </div>
                      <p className="text-sm text-slate-600 mt-1 truncate">{order.customerName}</p>
                      <p className="text-xs text-slate-400 mt-0.5">
                        {new Date(order.createdAt).toLocaleString()} · {order.city} · {order.items.length} item{order.items.length !== 1 ? 's' : ''}
                      </p>
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                      <p className="font-bold text-slate-800">{formatPrice(order.total)}</p>
                      {expanded ? <ChevronUp className="w-5 h-5 text-slate-400" /> : <ChevronDown className="w-5 h-5 text-slate-400" />}
                    </div>
                  </div>
                </button>

                {expanded && (
                  <div className="px-4 pb-4 border-t border-slate-100">
                    <div className="pt-4 space-y-4">
                      {order.status !== 'Cancelled' && (
                        <Link to={`/admin/orders/${order.id}/edit`}>
                          <Button variant="outline" size="sm" className="w-full sm:w-auto">
                            <Pencil className="w-4 h-4" /> Edit Order
                          </Button>
                        </Link>
                      )}

                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <div>
                          <label className="block text-xs font-medium text-slate-500 mb-1">Update Status</label>
                          <Select
                            options={statuses.map((s) => ({ value: s, label: s }))}
                            value={order.status}
                            onChange={(e) => {
                              const status = e.target.value;
                              if (status === 'Confirmed') {
                                updateMutation.mutate({
                                  id: order.id,
                                  status,
                                  advanceAmount: parseAdvance(order.id, order),
                                  discountAmount: parseDiscount(order.id, order),
                                });
                                return;
                              }
                              const tracking = status === 'Shipped' ? trackingInputs[order.id] : undefined;
                              updateMutation.mutate({ id: order.id, status, trackingNumber: tracking });
                            }}
                          />
                        </div>
                        {(order.status === 'Confirmed' ||
                          order.status === 'Shipped' ||
                          order.status === 'Delivered' ||
                          order.status === 'Pending') && (
                          <div className="space-y-2 sm:col-span-2">
                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                              <Input
                                label="Advance Amount (Rs.)"
                                type="number"
                                min={0}
                                max={order.total}
                                value={advanceInputs[order.id] ?? (order.advanceAmount != null ? String(order.advanceAmount) : '')}
                                onChange={(e) => setAdvanceInputs((prev) => ({ ...prev, [order.id]: e.target.value }))}
                                placeholder="e.g. 500 or 1000"
                              />
                              <Input
                                label="Discount (Rs.)"
                                type="number"
                                min={0}
                                max={order.total}
                                value={discountInputs[order.id] ?? (order.discountAmount != null ? String(order.discountAmount) : '')}
                                onChange={(e) => setDiscountInputs((prev) => ({ ...prev, [order.id]: e.target.value }))}
                                placeholder="e.g. 200 or 500"
                              />
                            </div>
                            {order.status === 'Confirmed' && (
                              <Button
                                size="sm"
                                className="w-full sm:w-auto"
                                disabled={updateMutation.isPending}
                                onClick={() => savePaymentDetails(order)}
                              >
                                Save Payment Details
                              </Button>
                            )}
                            {showPaymentDetails(order.status) && paymentDetails(order) && (
                              <div className="rounded-xl bg-blue-50 p-3 text-sm space-y-1">
                                <div className="flex justify-between">
                                  <span className="text-slate-600">Total</span>
                                  <span className="font-semibold">{formatPrice(order.total)}</span>
                                </div>
                                {(order.discountAmount ?? 0) > 0 && (
                                  <div className="flex justify-between">
                                    <span className="text-slate-600">Discount</span>
                                    <span className="font-semibold text-orange-700">-{formatPrice(order.discountAmount!)}</span>
                                  </div>
                                )}
                                <div className="flex justify-between">
                                  <span className="text-slate-600">Advance</span>
                                  <span className="font-semibold text-green-700">{formatPrice(order.advanceAmount ?? 0)}</span>
                                </div>
                                <div className="flex justify-between border-t border-blue-100 pt-1">
                                  <span className="text-slate-700 font-medium">Balance</span>
                                  <span className="font-bold text-brand-600">{formatPrice(order.balanceAmount)}</span>
                                </div>
                              </div>
                            )}
                          </div>
                        )}
                        {(order.status === 'Shipped' || order.status === 'Delivered') && (
                          <div className="space-y-2">
                            <Input
                              label="Tracking Number"
                              value={trackingInputs[order.id] ?? order.trackingNumber ?? ''}
                              onChange={(e) => setTrackingInputs((prev) => ({ ...prev, [order.id]: e.target.value }))}
                              placeholder="Courier tracking number"
                            />
                            <Button
                              size="sm"
                              className="w-full sm:w-auto"
                              disabled={updateMutation.isPending}
                              onClick={() =>
                                updateMutation.mutate({
                                  id: order.id,
                                  status: 'Shipped',
                                  trackingNumber: trackingInputs[order.id] ?? order.trackingNumber ?? '',
                                })
                              }
                            >
                              Save Tracking
                            </Button>
                          </div>
                        )}
                      </div>

                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-sm bg-slate-50 rounded-xl p-3">
                        <div className="break-words">
                          <span className="text-slate-500">Customer: </span>
                          <span className="font-medium">{order.customerName}</span>
                        </div>
                        <div>
                          <span className="text-slate-500">WhatsApp: </span>
                          <a
                            href={`https://wa.me/${order.whatsapp.replace(/\D/g, '')}`}
                            target="_blank"
                            rel="noreferrer"
                            className="text-brand-600 hover:underline"
                          >
                            {order.whatsapp}
                          </a>
                        </div>
                        <div className="sm:col-span-2"><span className="text-slate-500">City: </span>{order.city}</div>
                        <div className="sm:col-span-2 break-words"><span className="text-slate-500">Address: </span>{order.address}</div>
                      </div>

                      <div className="border rounded-xl p-3">
                        <div className="flex items-center gap-2 text-sm font-semibold text-slate-700 mb-2">
                          <Package className="w-4 h-4" /> Order Items
                        </div>
                        {order.items.map((item) => (
                          <div key={item.toyId} className="flex justify-between gap-2 text-sm py-1.5 border-b border-slate-50 last:border-0">
                            <span className="truncate flex-1">{item.toyName}</span>
                            <span className="font-medium shrink-0">{formatPrice(item.price)}</span>
                          </div>
                        ))}
                        <div className="flex flex-col sm:flex-row sm:justify-between gap-1 font-bold mt-3 pt-3 border-t text-sm">
                          <span className="text-slate-600">
                            Subtotal {formatPrice(order.subTotal)} + Delivery {formatPrice(order.deliveryCharge)}
                          </span>
                          <span className="text-brand-600 text-base">{formatPrice(order.total)}</span>
                        </div>
                        {showPaymentDetails(order.status) && paymentDetails(order) && (
                          <div className="mt-3 pt-3 border-t space-y-1 text-sm">
                            {(order.discountAmount ?? 0) > 0 && (
                              <div className="flex justify-between text-slate-600">
                                <span>Discount</span>
                                <span className="font-medium text-orange-700">-{formatPrice(order.discountAmount!)}</span>
                              </div>
                            )}
                            <div className="flex justify-between text-slate-600">
                              <span>Advance Paid</span>
                              <span className="font-medium text-green-700">{formatPrice(order.advanceAmount ?? 0)}</span>
                            </div>
                            <div className="flex justify-between font-bold">
                              <span>Balance Due</span>
                              <span className="text-brand-600">{formatPrice(order.balanceAmount)}</span>
                            </div>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                )}
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
