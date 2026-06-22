import { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, Upload, X, Search, Filter } from 'lucide-react';
import { api, toyPrimaryImage } from '@/api/client';
import type { ToyListItem } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input, Select } from '@/components/ui/Input';
import { formatPrice } from '@/lib/utils';
import { AdminPageHeader, AdminFormCard } from '@/components/admin/AdminPageHeader';
import { AdminListImage } from '@/components/admin/AdminListImage';

const emptyForm = {
  categoryId: 0, name: '', price: 0, salePrice: '' as string | number,
  imagePaths: [] as string[],
  imageUrls: [] as string[],
};

function filterToys(
  toys: ToyListItem[],
  search: string,
  categoryFilter: string,
  saleFilter: string,
  statusFilter: string,
  sort: 'name' | 'price-low' | 'price-high'
) {
  let result = [...toys];

  const q = search.trim().toLowerCase();
  if (q) {
    result = result.filter((t) => t.name.toLowerCase().includes(q));
  }

  if (categoryFilter !== 'All') {
    result = result.filter((t) => t.categoryName === categoryFilter);
  }

  if (saleFilter === 'OnSale') {
    result = result.filter((t) => t.salePrice != null);
  } else if (saleFilter === 'Regular') {
    result = result.filter((t) => t.salePrice == null);
  }

  if (statusFilter === 'Available') {
    result = result.filter((t) => !t.isSold);
  } else if (statusFilter === 'Sold') {
    result = result.filter((t) => t.isSold);
  }

  result.sort((a, b) => {
    if (sort === 'name') return a.name.localeCompare(b.name);
    const priceA = a.salePrice ?? a.price;
    const priceB = b.salePrice ?? b.price;
    return sort === 'price-low' ? priceA - priceB : priceB - priceA;
  });

  return result;
}

export function AdminToysPage() {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<number | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [uploading, setUploading] = useState(false);
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('All');
  const [saleFilter, setSaleFilter] = useState('All');
  const [statusFilter, setStatusFilter] = useState('All');
  const [sort, setSort] = useState<'name' | 'price-low' | 'price-high'>('name');
  const [showFilters, setShowFilters] = useState(false);

  const { data: toys, isLoading } = useQuery({ queryKey: ['admin-toys'], queryFn: api.adminGetToys });
  const { data: categories } = useQuery({ queryKey: ['admin-categories'], queryFn: api.adminGetCategories });

  const buildPayload = () => ({
    categoryId: form.categoryId,
    name: form.name,
    price: form.price,
    salePrice: form.salePrice === '' ? null : Number(form.salePrice),
    imagePaths: form.imagePaths,
  });

  const createMutation = useMutation({
    mutationFn: () => api.adminCreateToy(buildPayload()),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['admin-toys'] }); resetForm(); },
  });

  const updateMutation = useMutation({
    mutationFn: (id: number) => api.adminUpdateToy(id, buildPayload()),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['admin-toys'] }); resetForm(); },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => api.adminDeleteToy(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-toys'] }),
  });

  const resetForm = () => {
    setForm({ ...emptyForm, categoryId: categories?.[0]?.id ?? 0 });
    setEditing(null);
    setShowForm(false);
  };

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files?.length) return;
    setUploading(true);
    try {
      const uploads: { path: string; url: string }[] = [];
      for (const file of Array.from(files)) {
        const res = await api.adminUpload(file, 'toys');
        uploads.push({ path: res.path, url: res.url });
      }
      setForm((f) => ({
        ...f,
        imagePaths: [...f.imagePaths, ...uploads.map((u) => u.path)],
        imageUrls: [...f.imageUrls, ...uploads.map((u) => u.url)],
      }));
    } finally {
      setUploading(false);
      e.target.value = '';
    }
  };

  const removeImage = (index: number) => {
    setForm((f) => ({
      ...f,
      imagePaths: f.imagePaths.filter((_, i) => i !== index),
      imageUrls: f.imageUrls.filter((_, i) => i !== index),
    }));
  };

  const loadToyForEdit = async (toyId: number) => {
    const detail = await api.adminGetToy(toyId);
    setForm({
      categoryId: detail.categoryId,
      name: detail.name,
      price: detail.price,
      salePrice: detail.salePrice ?? '',
      imagePaths: detail.imagePaths,
      imageUrls: detail.imageUrls,
    });
    setEditing(toyId);
    setShowForm(true);
  };

  const categoryOptions = categories?.map((c) => ({ value: String(c.id), label: c.name })) ?? [];

  const categoryFilterOptions = useMemo(
    () => ['All', ...(categories?.map((c) => c.name) ?? [])],
    [categories]
  );

  const filteredToys = useMemo(
    () => (toys ? filterToys(toys, search, categoryFilter, saleFilter, statusFilter, sort) : []),
    [toys, search, categoryFilter, saleFilter, statusFilter, sort]
  );

  const hasActiveFilters =
    search.trim() !== '' ||
    categoryFilter !== 'All' ||
    saleFilter !== 'All' ||
    statusFilter !== 'All' ||
    sort !== 'name';

  const clearFilters = () => {
    setSearch('');
    setCategoryFilter('All');
    setSaleFilter('All');
    setStatusFilter('All');
    setSort('name');
  };

  const handleSubmit = () => {
    if (editing) updateMutation.mutate(editing);
    else createMutation.mutate();
  };

  return (
    <div className="w-full max-w-full overflow-x-hidden">
      <AdminPageHeader
        title="Toys"
        action={
          <Button onClick={() => { setForm({ ...emptyForm, categoryId: categories?.[0]?.id ?? 0 }); setShowForm(true); }} className="md:w-auto shadow-lg shadow-brand-500/30 rounded-full md:rounded-xl px-5">
            <Plus className="w-5 h-5" /> <span className="hidden md:inline">Add Toy</span>
          </Button>
        }
      />

      <div className="bg-white rounded-2xl border border-slate-200 p-4 mb-4 shadow-sm">
        <div className="flex gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by toy name..."
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
              label="Category"
              options={categoryFilterOptions.map((c) => ({ value: c, label: c === 'All' ? 'All Categories' : c }))}
              value={categoryFilter}
              onChange={(e) => setCategoryFilter(e.target.value)}
            />
            <Select
              label="Sale"
              options={[
                { value: 'All', label: 'All Prices' },
                { value: 'OnSale', label: 'On Sale' },
                { value: 'Regular', label: 'Regular Price' },
              ]}
              value={saleFilter}
              onChange={(e) => setSaleFilter(e.target.value)}
            />
            <Select
              label="Status"
              options={[
                { value: 'All', label: 'All Status' },
                { value: 'Available', label: 'Available' },
                { value: 'Sold', label: 'Sold' },
              ]}
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
            />
            <Select
              label="Sort By"
              options={[
                { value: 'name', label: 'Name (A-Z)' },
                { value: 'price-low', label: 'Price: Low to High' },
                { value: 'price-high', label: 'Price: High to Low' },
              ]}
              value={sort}
              onChange={(e) => setSort(e.target.value as 'name' | 'price-low' | 'price-high')}
            />
          </div>
        )}

        <div className="flex flex-wrap items-center justify-between gap-2 mt-3">
          <p className="text-sm text-slate-500">
            Showing <span className="font-semibold text-slate-700">{filteredToys.length}</span>
            {toys ? ` of ${toys.length}` : ''} toys
          </p>
          {hasActiveFilters && (
            <button type="button" onClick={clearFilters} className="text-sm text-brand-600 font-medium flex items-center gap-1 hover:underline">
              <X className="w-3.5 h-3.5" /> Clear filters
            </button>
          )}
        </div>
      </div>

      {showForm && (
        <AdminFormCard
          title={editing ? 'Edit Toy' : 'New Toy'}
          onCancel={resetForm}
          onSubmit={handleSubmit}
          submitLabel={editing ? 'Update' : 'Create'}
          submitDisabled={!form.name || !form.categoryId}
        >
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            <Select label="Category" options={categoryOptions} value={String(form.categoryId)} onChange={(e) => setForm({ ...form, categoryId: Number(e.target.value) })} />
            <Input label="Price (Rs.)" type="number" value={form.price} onChange={(e) => setForm({ ...form, price: Number(e.target.value) })} />
            <Input label="Sale Price (optional)" type="number" value={form.salePrice} onChange={(e) => setForm({ ...form, salePrice: e.target.value })} placeholder="Leave empty if no sale" />
            <div className="sm:col-span-2">
              <label className="block text-sm font-medium text-slate-700 mb-1">Images (multiple)</label>
              <label className="flex items-center gap-2 px-4 py-2.5 border border-dashed border-slate-300 rounded-xl cursor-pointer hover:bg-slate-50 text-sm text-slate-600 w-fit">
                <Upload className="w-4 h-4 shrink-0" /> {uploading ? 'Uploading...' : 'Add images'}
                <input type="file" accept="image/*" multiple className="hidden" onChange={handleUpload} />
              </label>
              {form.imageUrls.length > 0 && (
                <div className="flex flex-wrap gap-3 mt-3">
                  {form.imageUrls.map((url, i) => (
                    <div key={`${url}-${i}`} className="relative">
                      <img
                        src={url}
                        alt=""
                        className="w-20 h-20 rounded-xl object-cover border border-slate-200 bg-slate-50"
                      />
                      <button
                        type="button"
                        onClick={() => removeImage(i)}
                        className="absolute -top-2 -right-2 p-1 bg-red-500 text-white rounded-full shadow hover:bg-red-600"
                        aria-label="Remove image"
                      >
                        <X className="w-3 h-3" />
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </AdminFormCard>
      )}

      {/* Mobile cards */}
      <div className="md:hidden space-y-3">
        {isLoading ? (
          <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">Loading...</div>
        ) : toys?.length === 0 ? (
          <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">No toys yet</div>
        ) : filteredToys.length === 0 ? (
          <div className="bg-white rounded-2xl p-8 text-center border">
            <p className="text-slate-500">No toys match your filters</p>
            <Button variant="ghost" size="sm" className="mt-3" onClick={clearFilters}>Clear filters</Button>
          </div>
        ) : filteredToys.map((toy) => (
          <div key={toy.id} className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0 flex-1 flex items-start gap-3">
                <AdminListImage src={toyPrimaryImage(toy)} name={toy.name} />
                <div className="min-w-0">
                <p className="font-semibold text-slate-800">{toy.name}</p>
                <p className="text-xs text-slate-500 mt-0.5">{toy.categoryName}</p>
                <p className="text-brand-600 font-bold mt-1">
                  {formatPrice(toy.salePrice ?? toy.price)}
                  {toy.salePrice && <span className="text-slate-400 line-through text-xs ml-1 font-normal">{formatPrice(toy.price)}</span>}
                  {toy.salePrice && <span className="ml-2 text-xs font-medium text-orange-600 bg-orange-50 px-1.5 py-0.5 rounded">On Sale</span>}
                </p>
                <div className="flex flex-wrap gap-2 mt-2">
                  <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${toy.isSold ? 'bg-red-100 text-red-700' : 'bg-green-100 text-green-700'}`}>
                    {toy.isSold ? 'Sold' : 'Available'}
                  </span>
                </div>
                </div>
              </div>
              <div className="flex gap-1 shrink-0">
                <button type="button" onClick={() => loadToyForEdit(toy.id)} className="p-2.5 text-brand-600 hover:bg-brand-50 rounded-xl">
                  <Pencil className="w-4 h-4" />
                </button>
                <button type="button" onClick={() => { if (confirm('Delete?')) deleteMutation.mutate(toy.id); }} className="p-2.5 text-red-500 hover:bg-red-50 rounded-xl">
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Desktop table */}
      <div className="hidden md:block bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full text-sm min-w-[600px]">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="text-left p-4 w-16">Image</th>
                <th className="text-left p-4">Name</th>
                <th className="text-left p-4">Category</th>
                <th className="text-left p-4">Price</th>
                <th className="text-left p-4">Status</th>
                <th className="text-right p-4">Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr><td colSpan={6} className="p-8 text-center text-slate-400">Loading...</td></tr>
              ) : toys?.length === 0 ? (
                <tr><td colSpan={6} className="p-8 text-center text-slate-400">No toys yet</td></tr>
              ) : filteredToys.length === 0 ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center">
                    <p className="text-slate-500">No toys match your filters</p>
                    <Button variant="ghost" size="sm" className="mt-3" onClick={clearFilters}>Clear filters</Button>
                  </td>
                </tr>
              ) : filteredToys.map((toy) => (
                <tr key={toy.id} className="border-t border-slate-100 hover:bg-slate-50">
                  <td className="p-4">
                    <AdminListImage src={toyPrimaryImage(toy)} name={toy.name} size="sm" />
                  </td>
                  <td className="p-4 font-medium">{toy.name}</td>
                  <td className="p-4 text-slate-600">{toy.categoryName || '—'}</td>
                  <td className="p-4">
                    {formatPrice(toy.salePrice ?? toy.price)}
                    {toy.salePrice && <span className="text-slate-400 line-through ml-1 text-xs">{formatPrice(toy.price)}</span>}
                    {toy.salePrice && <span className="ml-2 text-xs font-medium text-orange-600">Sale</span>}
                  </td>
                  <td className="p-4">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${toy.isSold ? 'bg-red-100 text-red-700' : 'bg-green-100 text-green-700'}`}>
                      {toy.isSold ? 'Sold' : 'Available'}
                    </span>
                  </td>
                  <td className="p-4 text-right">
                    <button type="button" onClick={() => loadToyForEdit(toy.id)} className="p-2 text-brand-600 hover:bg-brand-50 rounded-lg"><Pencil className="w-4 h-4" /></button>
                    <button type="button" onClick={() => { if (confirm('Delete?')) deleteMutation.mutate(toy.id); }} className="p-2 text-red-500 hover:bg-red-50 rounded-lg"><Trash2 className="w-4 h-4" /></button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
