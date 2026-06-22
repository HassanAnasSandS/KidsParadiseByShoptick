import { useQuery } from '@tanstack/react-query';
import { useParams, Link } from 'react-router-dom';
import { api } from '@/api/client';
import { ToyCard, ToyCardSkeleton } from '@/components/shop/ToyCard';
import { SeoHead } from '@/components/seo/SeoHead';
import { buildBreadcrumbJsonLd } from '@/lib/seo';

export function CategoryPage() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading } = useQuery({
    queryKey: ['category', id],
    queryFn: () => api.getCategory(Number(id)),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 py-8">
        <div className="h-8 w-48 skeleton rounded mb-6" />
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => <ToyCardSkeleton key={i} />)}
        </div>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="text-center py-20">
        <h2 className="text-xl font-semibold">Category not found</h2>
        <Link to="/shop" className="text-brand-600 mt-2 inline-block">Back to shop</Link>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 py-8">
      <SeoHead
        title={`${data.name} Toys`}
        description={`Shop ${data.name} toys online at Kids Paradise by Shoptick. ${data.toys.length} unique kids toys with delivery in Karachi & across Pakistan.`}
        path={`/category/${data.id}`}
        image={data.imageUrl}
        jsonLd={buildBreadcrumbJsonLd([
          { name: 'Home', path: '/' },
          { name: 'Shop', path: '/shop' },
          { name: data.name, path: `/category/${data.id}` },
        ])}
      />
      <div className="mb-8">
        <p className="text-sm text-brand-500 font-medium"><Link to="/shop">Shop</Link> / {data.name}</p>
        <h1 className="text-3xl font-bold text-slate-800 mt-1">{data.name}</h1>
        <p className="text-slate-500 mt-1">{data.toys.length} toys available</p>
      </div>
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 md:gap-6">
        {data.toys.map((toy) => <ToyCard key={toy.id} toy={toy} />)}
      </div>
      {data.toys.length === 0 && (
        <div className="text-center py-16 text-slate-500">No toys available in this category.</div>
      )}
    </div>
  );
}
