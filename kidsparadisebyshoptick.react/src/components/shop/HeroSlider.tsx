import { useState, useEffect, useCallback, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { ChevronLeft, ChevronRight, ArrowRight } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { BrandName } from '@/components/ui/BrandName';
import { BrandLogo } from '@/components/ui/BrandLogo';
import { useSiteImages } from '@/hooks/useSiteImages';
import { useDeliveryRates } from '@/hooks/useDeliveryRates';
import { resolveSiteColor, resolveSiteText, type SiteImageKey } from '@/lib/siteImages';
import { withAffiliatePath } from '@/store/affiliate';
import { useShopPath } from '@/store/shopFilters';

const HERO_KEYS: SiteImageKey[] = [
  'hero_slide_1',
  'hero_slide_2',
  'hero_slide_3',
  'hero_slide_4',
];

export function HeroSlider() {
  const { getContent, images } = useSiteImages();
  const { label: deliveryLabel } = useDeliveryRates();
  const shopPath = useShopPath();
  const [current, setCurrent] = useState(0);

  const slides = useMemo(
    () =>
      HERO_KEYS.map((imageKey) => {
        const content = getContent(imageKey);
        const title = content.title?.trim() || '';
        const subtitle = resolveSiteText(content.subtitle, { delivery: deliveryLabel });
        const cta = content.ctaText?.trim() || '';
        return {
          imageKey,
          image: content.imageUrl,
          title,
          subtitle,
          cta,
          link: withAffiliatePath(content.linkUrl?.trim() || shopPath),
          titleColor: resolveSiteColor(content.titleColor),
          subtitleColor: resolveSiteColor(content.subtitleColor, '#FFFFFFE6'),
          ctaColor: resolveSiteColor(content.ctaColor),
        };
      }),
    [getContent, images, deliveryLabel, shopPath],
  );

  const next = useCallback(() => setCurrent((c) => (c + 1) % slides.length), [slides.length]);
  const prev = () => setCurrent((c) => (c - 1 + slides.length) % slides.length);

  useEffect(() => {
    const timer = setInterval(next, 5000);
    return () => clearInterval(timer);
  }, [next]);

  const slide = slides[current];

  return (
    <section className="relative h-[420px] md:h-[520px] overflow-hidden rounded-b-3xl shadow-lg bg-slate-900">
      {slides.map((s, i) => (
        <div
          key={s.imageKey}
          className={`absolute inset-0 transition-opacity duration-700 ${i === current ? 'opacity-100 z-10' : 'opacity-0 z-0'}`}
        >
          <img
            src={s.image}
            alt=""
            className="absolute inset-0 w-full h-full object-cover object-center md:object-right"
          />
          <div className="absolute inset-0 bg-gradient-to-r from-slate-900/92 via-slate-900/55 to-slate-900/5 md:from-slate-900/90 md:via-slate-900/45 md:to-transparent" />
        </div>
      ))}

      <div
        className="absolute inset-0 opacity-[0.07] pointer-events-none z-20"
        style={{ backgroundImage: 'url(/watermark.svg)', backgroundRepeat: 'repeat', backgroundSize: '180px' }}
      />

      <div className="absolute inset-0 z-30 flex items-center">
        <div className="max-w-7xl mx-auto px-14 sm:px-6 w-full">
          <div className="max-w-xl animate-fade-in">
            <span className="inline-flex items-center gap-2 bg-white/20 backdrop-blur text-white px-4 py-2 rounded-full mb-4 border border-white/20">
              <BrandLogo size={20} className="w-5 h-5 rounded-md" />
              <BrandName variant="hero" />
            </span>
            {slide.title ? (
              <h1
                className="text-2xl sm:text-3xl md:text-5xl font-extrabold leading-tight mb-3 drop-shadow-lg"
                style={{ color: slide.titleColor }}
              >
                {slide.title}
              </h1>
            ) : null}
            {slide.subtitle ? (
              <p
                className="text-base sm:text-lg mb-6 leading-relaxed max-w-md"
                style={{ color: slide.subtitleColor }}
              >
                {slide.subtitle}
              </p>
            ) : null}
            {slide.cta ? (
              <Link to={slide.link}>
                <Button
                  size="lg"
                  className="bg-accent-500 hover:bg-accent-400 shadow-lg shadow-accent-500/30 border-0"
                  style={{ color: slide.ctaColor }}
                >
                  {slide.cta} <ArrowRight className="w-4 h-4" />
                </Button>
              </Link>
            ) : null}
          </div>
        </div>
      </div>

      <button
        onClick={prev}
        className="absolute left-4 top-1/2 -translate-y-1/2 z-40 w-10 h-10 rounded-full bg-white/20 backdrop-blur hover:bg-white/40 text-white flex items-center justify-center transition-colors"
        aria-label="Previous slide"
      >
        <ChevronLeft className="w-6 h-6" />
      </button>
      <button
        onClick={next}
        className="absolute right-4 top-1/2 -translate-y-1/2 z-40 w-10 h-10 rounded-full bg-white/20 backdrop-blur hover:bg-white/40 text-white flex items-center justify-center transition-colors"
        aria-label="Next slide"
      >
        <ChevronRight className="w-6 h-6" />
      </button>

      <div className="absolute bottom-3 left-1/2 -translate-x-1/2 z-40 flex">
        {slides.map((_, i) => (
          <button
            key={i}
            onClick={() => setCurrent(i)}
            className="h-8 px-1.5 flex items-center"
            aria-label={`Go to slide ${i + 1}`}
          >
            <span className={`h-2 rounded-full transition-all ${i === current ? 'w-8 bg-white' : 'w-2 bg-white/50 hover:bg-white/80'}`} />
          </button>
        ))}
      </div>
    </section>
  );
}
