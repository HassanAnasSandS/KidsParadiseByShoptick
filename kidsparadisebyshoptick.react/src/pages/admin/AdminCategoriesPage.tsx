import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, Upload } from 'lucide-react';
import { api } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { AdminPageHeader, AdminFormCard } from '@/components/admin/AdminPageHeader';

export function AdminCategoriesPage() {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<number | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', imagePath: '' });
  const [uploading, setUploading] = useState(false);

  const { data: categories, isLoading } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: api.adminGetCategories,
  });

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
    setForm({ name: '', imagePath: '' });
    setEditing(null);
    setShowForm(false);
  };

  const startEdit = (cat: { id: number; name: string }) => {
    setForm({ name: cat.name, imagePath: '' });
    setEditing(cat.id);
    setShowForm(true);
  };

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const res = await api.adminUpload(file, 'categories');
      setForm((f) => ({ ...f, imagePath: res.path }));
    } finally {
      setUploading(false);
    }
  };

  const handleSubmit = () => {
    if (editing) updateMutation.mutate(editing);
    else createMutation.mutate();
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
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Image</label>
              <label className="flex items-center gap-2 px-4 py-2.5 border border-dashed border-slate-300 rounded-xl cursor-pointer hover:bg-slate-50 text-sm text-slate-600 break-all">
                <Upload className="w-4 h-4 shrink-0" />
                <span className="truncate">{uploading ? 'Uploading...' : form.imagePath || 'Choose image'}</span>
                <input type="file" accept="image/*" className="hidden" onChange={handleUpload} />
              </label>
            </div>
          </div>
        </AdminFormCard>
      )}

      {/* Mobile cards */}
      <div className="md:hidden space-y-3">
        {isLoading ? (
          <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">Loading...</div>
        ) : categories?.length === 0 ? (
          <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">No categories yet</div>
        ) : categories?.map((cat) => (
          <div key={cat.id} className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0 flex-1">
                <p className="font-semibold text-slate-800 truncate">{cat.name}</p>
                <p className="text-sm text-slate-500 mt-0.5">{cat.toyCount} toys</p>
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
              <th className="text-left p-4">Name</th>
              <th className="text-left p-4">Toys</th>
              <th className="text-right p-4">Actions</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr><td colSpan={3} className="p-8 text-center text-slate-400">Loading...</td></tr>
            ) : categories?.length === 0 ? (
              <tr><td colSpan={3} className="p-8 text-center text-slate-400">No categories yet</td></tr>
            ) : categories?.map((cat) => (
              <tr key={cat.id} className="border-t border-slate-100 hover:bg-slate-50">
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
