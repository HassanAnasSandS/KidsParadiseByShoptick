import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Pencil, Star, Upload, X } from 'lucide-react';
import { api } from '@/api/client';
import type { Review } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input, Textarea } from '@/components/ui/Input';
import { StarRating } from '@/components/shop/ReviewForm';
import { AdminPageHeader } from '@/components/admin/AdminPageHeader';

export function AdminReviewsPage() {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<Review | null>(null);
  const [form, setForm] = useState({ reviewerName: '', rating: 5, comment: '', imagePath: '', imageUrl: '' });
  const [uploading, setUploading] = useState(false);

  const { data: reviews, isLoading } = useQuery({
    queryKey: ['admin-reviews'],
    queryFn: api.adminGetReviews,
  });

  const updateMutation = useMutation({
    mutationFn: () => {
      const data: { reviewerName: string; rating: number; comment: string; imagePath?: string } = {
        reviewerName: form.reviewerName,
        rating: form.rating,
        comment: form.comment,
      };
      if (form.imagePath) data.imagePath = form.imagePath;
      else if (!form.imageUrl && editing!.imageUrl) data.imagePath = '';
      return api.adminUpdateReview(editing!.id, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-reviews'] });
      setEditing(null);
    },
  });

  const startEdit = (review: Review) => {
    setEditing(review);
    setForm({
      reviewerName: review.reviewerName,
      rating: review.rating,
      comment: review.comment,
      imagePath: '',
      imageUrl: review.imageUrl ?? '',
    });
  };

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const res = await api.adminUpload(file, 'reviews');
      setForm((f) => ({ ...f, imagePath: res.path, imageUrl: res.url }));
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="w-full max-w-full overflow-x-hidden">
      <AdminPageHeader title="Reviews" />

      {editing && (
        <div className="bg-white rounded-2xl p-4 md:p-6 border border-slate-200 mb-6 shadow-sm">
          <h2 className="font-semibold text-slate-800 mb-4">Edit Review — {editing.toyName}</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input label="Reviewer Name" value={form.reviewerName} onChange={(e) => setForm({ ...form, reviewerName: e.target.value })} />
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Rating</label>
              <StarRating value={form.rating} onChange={(rating) => setForm({ ...form, rating })} />
            </div>
            <div className="sm:col-span-2">
              <Textarea label="Comment" rows={3} value={form.comment} onChange={(e) => setForm({ ...form, comment: e.target.value })} />
            </div>
            <div className="sm:col-span-2">
              <label className="block text-sm font-medium text-slate-700 mb-1">Review Image</label>
              {form.imageUrl ? (
                <div className="flex items-start gap-3">
                  <img src={form.imageUrl} alt="" className="w-20 h-20 rounded-xl object-cover border" />
                  <button type="button" onClick={() => setForm({ ...form, imagePath: '', imageUrl: '' })} className="text-red-500 text-sm flex items-center gap-1">
                    <X className="w-4 h-4" /> Remove
                  </button>
                </div>
              ) : (
                <label className="flex items-center gap-2 px-4 py-2.5 border border-dashed border-slate-300 rounded-xl cursor-pointer hover:bg-slate-50 text-sm text-slate-600 w-fit">
                  <Upload className="w-4 h-4" /> {uploading ? 'Uploading...' : 'Upload image'}
                  <input type="file" accept="image/*" className="hidden" onChange={handleUpload} />
                </label>
              )}
            </div>
          </div>
          <div className="flex flex-col sm:flex-row gap-2 mt-4">
            <Button onClick={() => updateMutation.mutate()} disabled={updateMutation.isPending}>Save Changes</Button>
            <Button variant="ghost" onClick={() => setEditing(null)}>Cancel</Button>
          </div>
        </div>
      )}

      <div className="space-y-3">
        {isLoading ? (
          <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">Loading...</div>
        ) : reviews?.length === 0 ? (
          <div className="bg-white rounded-2xl p-8 text-center text-slate-400 border">No reviews yet</div>
        ) : reviews?.map((review) => (
          <div key={review.id} className="bg-white rounded-2xl border border-slate-200 p-4 md:p-5 shadow-sm">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="font-semibold text-slate-800">{review.reviewerName}</p>
                  <div className="flex">
                    {Array.from({ length: 5 }).map((_, i) => (
                      <Star key={i} className={`w-3.5 h-3.5 ${i < review.rating ? 'fill-accent-500 text-accent-500' : 'text-slate-200'}`} />
                    ))}
                  </div>
                </div>
                <p className="text-sm text-brand-600 mt-1">{review.toyName}</p>
                <p className="text-xs text-slate-400">Order {review.orderNumber} · {new Date(review.createdAt).toLocaleString()}</p>
                <p className="text-sm text-slate-600 mt-2">{review.comment}</p>
                {review.imageUrl && (
                  <img src={review.imageUrl} alt="" className="mt-2 w-20 h-20 rounded-lg object-cover border" />
                )}
              </div>
              <button type="button" onClick={() => startEdit(review)} className="p-2.5 text-brand-600 hover:bg-brand-50 rounded-xl shrink-0">
                <Pencil className="w-4 h-4" />
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
