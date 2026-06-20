import { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ImageIcon, Upload, RotateCcw, CheckCircle2 } from 'lucide-react';
import { api, type SiteImageAdmin } from '@/api/client';
import { AdminPageHeader } from '@/components/admin/AdminPageHeader';
import { Button } from '@/components/ui/Button';

export function AdminCustomizationPage() {
  const queryClient = useQueryClient();
  const [uploadingKey, setUploadingKey] = useState<string | null>(null);

  const { data: images, isLoading } = useQuery({
    queryKey: ['admin-site-images'],
    queryFn: api.adminGetSiteImages,
  });

  const uploadMutation = useMutation({
    mutationFn: ({ key, file }: { key: string; file: File }) => api.adminUploadSiteImage(key, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-site-images'] });
      queryClient.invalidateQueries({ queryKey: ['site-images'] });
      setUploadingKey(null);
    },
    onError: () => setUploadingKey(null),
  });

  const resetMutation = useMutation({
    mutationFn: (key: string) => api.adminResetSiteImage(key),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-site-images'] });
      queryClient.invalidateQueries({ queryKey: ['site-images'] });
    },
  });

  const groups = useMemo(() => {
    if (!images) return [];
    const map = new Map<string, SiteImageAdmin[]>();
    images.forEach((img) => {
      const list = map.get(img.group) ?? [];
      list.push(img);
      map.set(img.group, list);
    });
    return Array.from(map.entries());
  }, [images]);

  const handleUpload = async (key: string, e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploadingKey(key);
    uploadMutation.mutate({ key, file });
    e.target.value = '';
  };

  return (
    <div className="w-full max-w-full overflow-x-hidden">
      <AdminPageHeader title="Website Images" />

      <p className="text-sm text-slate-500 mb-6 -mt-2 md:mt-0">
        Change homepage hero slides, banners, and page headers. Uploads apply instantly on the store.
      </p>

      {isLoading ? (
        <div className="bg-white rounded-2xl p-10 text-center text-slate-400 border">Loading...</div>
      ) : (
        <div className="space-y-8">
          {groups.map(([group, items]) => (
            <section key={group}>
              <h2 className="text-lg font-bold text-slate-800 mb-3 flex items-center gap-2">
                <ImageIcon className="w-5 h-5 text-brand-600" />
                {group}
              </h2>
              <div className="grid sm:grid-cols-2 xl:grid-cols-3 gap-4">
                {items.map((img) => (
                  <div key={img.key} className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm">
                    <div className="relative aspect-[16/10] bg-slate-100">
                      <img src={img.imageUrl} alt={img.label} className="w-full h-full object-cover" />
                      {img.isCustom && (
                        <span className="absolute top-2 right-2 inline-flex items-center gap-1 text-xs font-semibold bg-green-600 text-white px-2 py-1 rounded-full">
                          <CheckCircle2 className="w-3 h-3" /> Custom
                        </span>
                      )}
                    </div>
                    <div className="p-4">
                      <h3 className="font-semibold text-slate-800">{img.label}</h3>
                      <p className="text-xs text-slate-500 mt-1 truncate">{img.key}</p>
                      <div className="flex flex-wrap gap-2 mt-4">
                        <label className="flex-1 min-w-[120px]">
                          <span className="flex items-center justify-center gap-2 px-3 py-2.5 rounded-xl bg-brand-600 text-white text-sm font-medium cursor-pointer hover:bg-brand-700 transition-colors">
                            <Upload className="w-4 h-4" />
                            {uploadingKey === img.key ? 'Uploading...' : 'Change'}
                          </span>
                          <input
                            type="file"
                            accept="image/*"
                            className="hidden"
                            disabled={uploadingKey === img.key}
                            onChange={(e) => handleUpload(img.key, e)}
                          />
                        </label>
                        {img.isCustom && (
                          <Button
                            variant="ghost"
                            className="shrink-0"
                            disabled={resetMutation.isPending}
                            onClick={() => {
                              if (confirm(`Reset "${img.label}" to default image?`)) {
                                resetMutation.mutate(img.key);
                              }
                            }}
                          >
                            <RotateCcw className="w-4 h-4" /> Reset
                          </Button>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          ))}
        </div>
      )}
    </div>
  );
}
