import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { Star, Upload, X } from 'lucide-react';
import { api } from '@/api/client';
import type { PendingReview } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input, Textarea } from '@/components/ui/Input';

export function StarRating({ value, onChange, size = 'md' }: { value: number; onChange?: (v: number) => void; size?: 'sm' | 'md' }) {
  const cls = size === 'sm' ? 'w-4 h-4' : 'w-6 h-6';
  return (
    <div className="flex gap-1">
      {Array.from({ length: 5 }).map((_, i) => (
        <button
          key={i}
          type="button"
          onClick={() => onChange?.(i + 1)}
          disabled={!onChange}
          className={onChange ? 'cursor-pointer' : 'cursor-default'}
        >
          <Star className={`${cls} ${i < value ? 'fill-accent-500 text-accent-500' : 'text-slate-200'}`} />
        </button>
      ))}
    </div>
  );
}

export function ReviewForm({ item, whatsapp, onSuccess }: { item: PendingReview; whatsapp: string; onSuccess: () => void }) {
  const [name, setName] = useState('');
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const [imagePath, setImagePath] = useState('');
  const [imagePreview, setImagePreview] = useState('');
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');

  const submit = useMutation({
    mutationFn: () => api.createReview({
      whatsapp,
      orderId: item.orderId,
      toyId: item.toyId,
      reviewerName: name,
      rating,
      comment,
      imagePath: imagePath || undefined,
    }),
    onSuccess: () => {
      setName('');
      setComment('');
      setImagePath('');
      setImagePreview('');
      onSuccess();
    },
    onError: (err: Error) => setError(err.message),
  });

  const handleImage = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    setError('');
    try {
      const res = await api.uploadReviewImage(file);
      setImagePath(res.path);
      setImagePreview(res.url);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm">
      <div className="flex items-start gap-4 mb-4">
        {item.toyImageUrl && (
          <img src={item.toyImageUrl} alt="" className="w-16 h-16 rounded-xl object-cover border shrink-0" />
        )}
        <div>
          <p className="font-semibold text-slate-800">{item.toyName}</p>
          <p className="text-xs text-slate-500 mt-0.5">Order {item.orderNumber}</p>
        </div>
      </div>
      <div className="space-y-3">
        <Input label="Your Name" value={name} onChange={(e) => setName(e.target.value)} />
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Rating</label>
          <StarRating value={rating} onChange={setRating} />
        </div>
        <Textarea label="Your Review" rows={3} value={comment} onChange={(e) => setComment(e.target.value)} />
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Photo (optional)</label>
          {imagePreview ? (
            <div className="relative inline-block">
              <img src={imagePreview} alt="" className="w-24 h-24 rounded-xl object-cover border" />
              <button
                type="button"
                onClick={() => { setImagePath(''); setImagePreview(''); }}
                className="absolute -top-2 -right-2 bg-red-500 text-white rounded-full p-1"
              >
                <X className="w-3 h-3" />
              </button>
            </div>
          ) : (
            <label className="flex items-center gap-2 px-4 py-2.5 border border-dashed border-slate-300 rounded-xl cursor-pointer hover:bg-slate-50 text-sm text-slate-600 w-fit">
              <Upload className="w-4 h-4" /> {uploading ? 'Uploading...' : 'Add one photo'}
              <input type="file" accept="image/*" className="hidden" onChange={handleImage} />
            </label>
          )}
        </div>
        {error && <p className="text-sm text-red-500">{error}</p>}
        <Button
          onClick={() => submit.mutate()}
          disabled={submit.isPending || !name.trim() || !comment.trim()}
          className="w-full sm:w-auto"
        >
          Submit Review
        </Button>
      </div>
    </div>
  );
}

export function StarRatingDisplay({ value, size = 'sm' }: { value: number; size?: 'sm' | 'md' }) {
  return <StarRating value={value} size={size} />;
}
