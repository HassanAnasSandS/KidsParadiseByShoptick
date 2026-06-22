import { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, Upload, Search, Filter, X } from 'lucide-react';
import { api } from '@/api/client';
import type { Category } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input, Select } from '@/components/ui/Input';
import { AdminPageHeader, AdminFormCard } from '@/components/admin/AdminPageHeader';
import { AdminListImage } from '@/components/admin/AdminListImage';

type SortOption = 'name-asc' | 'name-desc' | 'toys-high' | 'toys-low';
type ToyCountFilter = 'All' | 'Empty' | 'HasToys';

function filterCategories(
  categories: Category[],
  search: string,
  toyFilter: ToyCountFilter,
  sort: SortOption
) {
  let result = [...categories];

  const q = search.trim().toLowerCase();
  if (q) {
    result = result.filter((c) => c.name.toLowerCase().includes(q));
  }

  if (toyFilter === 'Empty') {
    result = result.filter((c) => c.toyCount === 0);
  } else if (toyFilter === 'HasToys') {
    result = result.filter((c) => c.toyCount > 0);
  }

  result.sort((a, b) => {
    if (sort === 'name-asc') return a.name.localeCompare(b.name);
    if (sort === 'name-desc') return b.name.localeCompare(a.name);
    if (sort === 'toys-high') return b.toyCount - a.toyCount;
    return a.toyCount - b.toyCount;
  });

  return result;
}

export function AdminCategoriesPage() {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<number | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', imagePath: '', imageUrl: '' });
  const [uploading, setUploading] = useState(false);
  const [search, setSearch] = useState('');
  const [toyFilter, setToyFilter] = useState<ToyCountFilter>('All');
  const [sort, setSort] = useState<SortOption>('name-asc');
  const [showFilters, setShowFilters] = useState(false);

  const { data: categories, isLoading } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: api.adminGetCategories,
  });

  const filteredCategories = useMemo(
    () => (categories ? filterCategories(categories, search, toyFilter, sort) : []),
    [categories, search, toyFilter, sort]
  );

  const hasActiveFilters = search.trim() !== '' || toyFilter !== 'All' || sort !== 'name-asc';

  const clearFilters = () => {
    setSearch('');
    setToyFilter('All');
    setSort('name-asc');
  };

  const createMutation = useMutation({
    mutationFn: () => api.adminCreateCategory(form),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['admin-categories'] }); resetForm(); },
  });

  const updateMutation = useMutation({
    mutationFn: (id: number) => api.adminUpdateCategory(id, form),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['admin-categories'] }); resetForm(); },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => api.adminDeleteCategory(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-categories'] }),
  });

  const resetForm = () => {
    setForm({ name: '', imagePath: '', imageUrl: '' });
    setEditing(null);
    setShowForm(false);
  };

  const startEdit = (cat: Category) => {
    setForm({
      name: cat.name,
      imagePath: cat.imagePath ?? '',
      imageUrl: cat.imageUrl ?? '',
    });
    setEditing(cat.id);
    setShowForm(true);
  };

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const res = await api.adminUpload(file, 'categories');
      setForm((f) => ({ ...f, imagePath: res.path, imageUrl: res.url }));
    } finally {
      setUploading(false);
    }
  };

  const handleSubmit = () => {
    if (editing) updateMutation.mutate(editing);
    else createMutation.mutate();
  };

  const renderEmpty = () => {
    if (isLoading) return <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">Loading...</div>;
    if (categories?.length === 0) return <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">No categories yet</div>;
    return (
      <div className="bg-white rounded-2xl p-8 text-center border">
        <p className="text-slate-500">No categories match your filters</p>
        <Button variant="ghost" size="sm" className="mt-3" onClick={clearFilters}>Clear filters</Button>
      </div>
    );
  };

  return (
    <div className="w-full max-w-full overflow-x-hidden">
      <AdminPageHeader
        title="Categories"
        action={
          <Button onClick={() => { resetForm(); setShowForm(true); }} className="md:w-auto shadow-lg shadow-brand-500/30 rounded-full md:rounded-xl px-5">
            <Plus className="w-5 h-5" /> <span className="hidden md:inline">Add Category</span>
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
              placeholder="Search by category name..."
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
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-4 pt-4 border-t border-slate-100">
            <Select
              label="Toys in Category"
              options={[
                { value: 'All', label: 'All Categories' },
                { value: 'HasToys', label: 'Has Toys (1+)' },
                { value: 'Empty', label: 'Empty (0 toys)' },
              ]}
              value={toyFilter}
              onChange={(e) => setToyFilter(e.target.value as ToyCountFilter)}
            />
            <Select
              label="Sort By"
              options={[
                { value: 'name-asc', label: 'Name (A–Z)' },
                { value: 'name-desc', label: 'Name (Z–A)' },
                { value: 'toys-high', label: 'Most Toys' },
                { value: 'toys-low', label: 'Fewest Toys' },
              ]}
              value={sort}
              onChange={(e) => setSort(e.target.value as SortOption)}
            />
          </div>
        )}

        <div className="flex flex-wrap items-center justify-between gap-2 mt-3">
          <p className="text-sm text-slate-500">
            Showing <span className="font-semibold text-slate-700">{filteredCategories.length}</span>
            {categories ? ` of ${categories.length}` : ''} categories
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
          title={editing ? 'Edit Category' : 'New Category'}
          onCancel={resetForm}
          onSubmit={handleSubmit}
          submitLabel={editing ? 'Update' : 'Create'}
          submitDisabled={!form.name}
        >
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            <div className="sm:col-span-2">
              <label className="block text-sm font-medium text-slate-700 mb-1">Image</label>
              {form.imageUrl ? (
                <div className="flex items-start gap-3">
                  <img src={form.imageUrl} alt="" className="w-20 h-20 rounded-xl object-cover border border-slate-200 bg-slate-50" />
                  <div className="flex flex-col gap-2">
                    <label className="flex items-center gap-2 px-4 py-2 border border-dashed border-slate-300 rounded-xl cursor-pointer hover:bg-slate-50 text-sm text-slate-600 w-fit">
                      <Upload className="w-4 h-4 shrink-0" />
                      {uploading ? 'Uploading...' : 'Change image'}
                      <input type="file" accept="image/*" className="hidden" onChange={handleUpload} />
                    </label>
                    <button
                      type="button"
                      onClick={() => setForm((f) => ({ ...f, imagePath: '', imageUrl: '' }))}
                      className="text-red-500 text-sm flex items-center gap-1 w-fit"
                    >
                      <X className="w-4 h-4" /> Remove
                    </button>
                  </div>
                </div>
              ) : (
                <label className="flex items-center gap-2 px-4 py-2.5 border border-dashed border-slate-300 rounded-xl cursor-pointer hover:bg-slate-50 text-sm text-slate-600 w-fit">
                  <Upload className="w-4 h-4 shrink-0" />
                  {uploading ? 'Uploading...' : 'Upload image'}
                  <input type="file" accept="image/*" className="hidden" onChange={handleUpload} />
                </label>
              )}
            </div>
          </div>
        </AdminFormCard>
      )}

      {/* Mobile cards */}
      <div className="md:hidden space-y-3">
        {filteredCategories.length === 0 ? renderEmpty() : filteredCategories.map((cat) => (
          <div key={cat.id} className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0 flex-1 flex items-center gap-3">
                <AdminListImage src={cat.imageUrl} name={cat.name} />
                <div className="min-w-0">
                  <p className="font-semibold text-slate-800 truncate">{cat.name}</p>
                  <p className="text-sm text-slate-500 mt-0.5">{cat.toyCount} toys</p>
                </div>
              </div>
              <div className="flex gap-1 shrink-0">
                <button type="button" onClick={() => startEdit(cat)} className="p-2.5 text-brand-600 hover:bg-brand-50 rounded-xl">
                  <Pencil className="w-4 h-4" />
                </button>
                <button
                  type="button"
                  onClick={() => { if (confirm('Delete?')) deleteMutation.mutate(cat.id); }}
                  className="p-2.5 text-red-500 hover:bg-red-50 rounded-xl"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Desktop table */}
      <div className="hidden md:block bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-slate-600">
            <tr>
              <th className="text-left p-4 w-16">Image</th>
              <th className="text-left p-4">Name</th>
              <th className="text-left p-4">Toys</th>
              <th className="text-right p-4">Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredCategories.length === 0 ? (
              <tr>
                <td colSpan={4} className="p-8 text-center">
                  {isLoading ? (
                    <span className="text-slate-400">Loading...</span>
                  ) : categories?.length === 0 ? (
                    <span className="text-slate-400">No categories yet</span>
                  ) : (
                    <>
                      <p className="text-slate-500">No categories match your filters</p>
                      <Button variant="ghost" size="sm" className="mt-3" onClick={clearFilters}>Clear filters</Button>
                    </>
                  )}
                </td>
              </tr>
            ) : filteredCategories.map((cat) => (
              <tr key={cat.id} className="border-t border-slate-100 hover:bg-slate-50">
                <td className="p-4">
                  <AdminListImage src={cat.imageUrl} name={cat.name} size="sm" />
                </td>
                <td className="p-4 font-medium">{cat.name}</td>
                <td className="p-4">{cat.toyCount}</td>
                <td className="p-4 text-right">
                  <button type="button" onClick={() => startEdit(cat)} className="p-2 text-brand-600 hover:bg-brand-50 rounded-lg"><Pencil className="w-4 h-4" /></button>
                  <button type="button" onClick={() => { if (confirm('Delete?')) deleteMutation.mutate(cat.id); }} className="p-2 text-red-500 hover:bg-red-50 rounded-lg"><Trash2 className="w-4 h-4" /></button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
