import { useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Star, ShoppingCart, Truck, Zap, ZoomIn, Trash2, Play } from 'lucide-react';
import { api, effectivePrice, toyPrimaryImage } from '@/api/client';
import { useCartStore } from '@/store/cart';
import { Button } from '@/components/ui/Button';
import { ToyInquiryButton } from '@/components/shop/ToyInquiryButton';
import { ImageLightbox } from '@/components/shop/ImageLightbox';
import { formatPrice, placeholderImage, PAYMENT_POLICY } from '@/lib/utils';
import { SeoHead } from '@/components/seo/SeoHead';
import { buildBreadcrumbJsonLd, buildProductJsonLd } from '@/lib/seo';
import { buildShopPath, mergeShopFilters } from '@/lib/shopFilters';
import { useShopPath, useShopFiltersStore } from '@/store/shopFilters';

function BackToShopButton({ shopPath }: { shopPath: string }) {
  const navigate = useNavigate();
  return (
    <button
      type="button"
      onClick={() => {
        if (window.history.length > 1) navigate(-1);
        else navigate(shopPath);
      }}
      className="text-brand-600 mt-2 inline-block hover:underline"
    >
      Back to shop
    </button>
  );
}

export function ProductPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const shopPath = useShopPath();
  const [activeImage, setActiveImage] = useState(0);
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const addItem = useCartStore((s) => s.addItem);
  const removeItem = useCartStore((s) => s.removeItem);
  const inCart = useCartStore((s) => s.items.some((i) => i.toyId === Number(id)));

  const { data: toy, isLoading } = useQuery({
    queryKey: ['toy', id],
    queryFn: () => api.getToy(Number(id)),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 py-8 grid md:grid-cols-2 gap-8">
        <div className="aspect-square skeleton rounded-2xl" />
        <div className="space-y-4">
          <div className="h-8 skeleton rounded w-3/4" />
          <div className="h-6 skeleton rounded w-1/4" />
        </div>
      </div>
    );
  }

  if (!toy) {
    return (
      <div className="text-center py-20">
        <h2 className="text-xl font-semibold">Product not found</h2>
        <BackToShopButton shopPath={shopPath} />
      </div>
    );
  }

  const images = toy.imageUrls.length > 0 ? toy.imageUrls : [placeholderImage(toy.name)];
  const price = effectivePrice(toy);
  const onSale = toy.salePrice != null && toy.salePrice < toy.price;

  const categoryShopPath = buildShopPath(
    mergeShopFilters(useShopFiltersStore.getState().filters, { categoryId: toy.categoryId }),
  );

  const productJsonLd = [
    buildProductJsonLd({ ...toy, price: toy.price, salePrice: toy.salePrice }),
    buildBreadcrumbJsonLd([
      { name: 'Home', path: '/' },
      { name: 'Shop', path: shopPath },
      { name: toy.categoryName, path: categoryShopPath },
      { name: toy.name, path: `/product/${toy.id}` },
    ]),
  ];

  const handleAddToCart = () => {
    addItem({
      toyId: toy.id,
      name: toy.name,
      price: toy.price,
      salePrice: toy.salePrice,
      imageUrl: toyPrimaryImage(toy),
    });
  };

  const handleRemoveFromCart = () => {
    removeItem(toy.id);
  };

  const handleOrderNow = () => {
    navigate('/checkout', {
      state: {
        buyNow: {
          toyId: toy.id,
          name: toy.name,
          price: toy.price,
          salePrice: toy.salePrice,
          imageUrl: toyPrimaryImage(toy),
        },
      },
    });
  };

  const handleWatchVideo = () => {
    if (!toy.videoLink?.trim()) return;
    const href = /^https?:\/\//i.test(toy.videoLink) ? toy.videoLink.trim() : `https://${toy.videoLink.trim()}`;
    window.open(href, '_blank', 'noopener,noreferrer');
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 py-8">
      <SeoHead
        title={`${toy.name} — Buy Online`}
        description={`Buy ${toy.name} online at Kids Paradise by Shoptick. ${toy.categoryName}. Rs. ${price.toLocaleString()} with delivery across Pakistan.`}
        path={`/product/${toy.id}`}
        image={toyPrimaryImage(toy)}
        jsonLd={productJsonLd}
      />
      <div className="grid md:grid-cols-2 gap-8 lg:gap-12">
        <div>
          <button
            type="button"
            onClick={() => setLightboxOpen(true)}
            className="group relative w-full bg-white rounded-2xl overflow-hidden border border-slate-100 shadow-sm cursor-zoom-in"
            aria-label="View full size image"
          >
            <img src={images[activeImage]} alt={toy.name} className="w-full aspect-square object-contain bg-slate-50 p-2" />
            <span className="absolute bottom-3 right-3 flex items-center gap-1.5 bg-black/55 text-white text-xs font-medium px-3 py-1.5 rounded-full sm:opacity-0 sm:group-hover:opacity-100 transition-opacity">
              <ZoomIn className="w-3.5 h-3.5" /> Tap to zoom
            </span>
          </button>
          {images.length > 1 && (
            <div className="flex gap-2 mt-3 overflow-x-auto">
              {images.map((img, i) => (
                <button
                  key={i}
                  onClick={() => setActiveImage(i)}
                  className={`shrink-0 w-16 h-16 rounded-lg overflow-hidden border-2 ${activeImage === i ? 'border-brand-500' : 'border-transparent'}`}
                >
                  <img src={img} alt="" className="w-full h-full object-contain bg-slate-50 p-0.5" />
                </button>
              ))}
            </div>
          )}
        </div>

        <div className="animate-fade-in">
          <p className="text-sm text-brand-500 font-medium">
            <Link to={`/category/${toy.categoryId}`}>{toy.categoryName}</Link>
          </p>
          <h1 className="text-3xl font-bold text-slate-800 mt-1">{toy.name}</h1>

          {toy.averageRating != null && toy.reviewCount > 0 && (
            <Link to="/reviews" className="flex items-center gap-2 mt-2 hover:opacity-80">
              <div className="flex">
                {Array.from({ length: 5 }).map((_, i) => (
                  <Star key={i} className={`w-4 h-4 ${i < Math.round(toy.averageRating!) ? 'fill-accent-500 text-accent-500' : 'text-slate-200'}`} />
                ))}
              </div>
              <span className="text-sm text-slate-500">({toy.reviewCount} reviews)</span>
            </Link>
          )}

          <div className="flex items-center gap-3 mt-4">
            <p className="text-3xl font-bold text-brand-600">{formatPrice(price)}</p>
            {onSale && <p className="text-lg text-slate-400 line-through">{formatPrice(toy.price)}</p>}
          </div>

          <div className="flex items-center gap-2 mt-4 text-sm text-slate-500">
            <Truck className="w-4 h-4" />
            Karachi Rs.300 | Other cities Rs.400 delivery · {PAYMENT_POLICY}
          </div>

          <p className={`mt-2 text-sm font-medium ${toy.isSold ? 'text-red-500' : 'text-green-600'}`}>
            {toy.isSold ? 'Sold — no longer available' : 'Available — unique item'}
          </p>

          {toy.videoLink?.trim() && (
            <Button variant="outline" onClick={handleWatchVideo} className="mt-4 w-full sm:w-auto">
              <Play className="w-4 h-4" /> Watch Video
            </Button>
          )}

          {!toy.isSold && (
            <div className="flex flex-col sm:flex-row gap-3 mt-6">
              {inCart ? (
                <Button variant="danger" onClick={handleRemoveFromCart} className="w-full sm:w-auto">
                  <Trash2 className="w-4 h-4" /> Remove from Cart
                </Button>
              ) : (
                <Button onClick={handleAddToCart} className="w-full sm:w-auto">
                  <ShoppingCart className="w-4 h-4" /> Add to Cart
                </Button>
              )}
              <Button variant="outline" onClick={handleOrderNow} className="w-full sm:w-auto">
                <Zap className="w-4 h-4" /> Order Now
              </Button>
              <ToyInquiryButton toy={toy} showLabel className="w-full sm:w-auto" />
            </div>
          )}
        </div>
      </div>

      {lightboxOpen && (
        <ImageLightbox
          images={images}
          initialIndex={activeImage}
          alt={toy.name}
          onClose={() => setLightboxOpen(false)}
        />
      )}
    </div>
  );
}
