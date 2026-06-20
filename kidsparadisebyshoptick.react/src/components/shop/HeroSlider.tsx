import { useState, useEffect, useCallback, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { ChevronLeft, ChevronRight, ArrowRight } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { BrandName } from '@/components/ui/BrandName';
import { useSiteImages } from '@/hooks/useSiteImages';
import type { SiteImageKey } from '@/lib/siteImages';

const slideMeta = [
  {
    imageKey: 'hero_slide_1' as SiteImageKey,
    title: 'Where Every Child\'s Dream Comes True',
    subtitle: 'Unique toys — one of a kind. Grab yours before it\'s gone!',
    cta: 'Shop Now',
    link: '/shop',
  },
  {
    imageKey: 'hero_slide_2' as SiteImageKey,
    title: 'Joy in Every Box',
    subtitle: 'Quality toys delivered across Pakistan with love.',
    cta: 'Explore Toys',
    link: '/shop',
  },
  {
    imageKey: 'hero_slide_3' as SiteImageKey,
    title: 'Soft Hugs & Big Smiles',
    subtitle: 'From plush friends to learning fun — find the perfect gift.',
    cta: 'Browse Collection',
    link: '/shop',
  },
  {
    imageKey: 'hero_slide_4' as SiteImageKey,
    title: 'Easy Ordering',
    subtitle: 'Karachi Rs.300 | Other cities Rs.400 — 10% advance payment required.',
    cta: 'Order Today',
    link: '/shop',
  },
];

export function HeroSlider() {
  const { get, images } = useSiteImages();
  const [current, setCurrent] = useState(0);

  const slides = useMemo(
    () => slideMeta.map((s) => ({ ...s, image: get(s.imageKey) })),
    [get, images]
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
        <div className="max-w-7xl mx-auto px-4 sm:px-6 w-full">
          <div className="max-w-xl animate-fade-in">
            <span className="inline-flex items-center gap-2 bg-white/20 backdrop-blur text-white px-4 py-2 rounded-full mb-4 border border-white/20">
              <span className="text-base">🧸</span>
              <BrandName variant="hero" />
            </span>
            <h1 className="text-3xl md:text-5xl font-extrabold text-white leading-tight mb-3 drop-shadow-lg">
              {slide.title}
            </h1>
            <p className="text-lg text-white/90 mb-6 leading-relaxed max-w-md">
              {slide.subtitle}
            </p>
            <Link to={slide.link}>
              <Button size="lg" className="bg-accent-500 hover:bg-accent-400 text-white shadow-lg shadow-accent-500/30 border-0">
                {slide.cta} <ArrowRight className="w-4 h-4" />
              </Button>
            </Link>
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

      <div className="absolute bottom-5 left-1/2 -translate-x-1/2 z-40 flex gap-2">
        {slides.map((_, i) => (
          <button
            key={i}
            onClick={() => setCurrent(i)}
            className={`h-2 rounded-full transition-all ${i === current ? 'w-8 bg-white' : 'w-2 bg-white/50 hover:bg-white/80'}`}
            aria-label={`Go to slide ${i + 1}`}
          />
        ))}
      </div>
    </section>
  );
}
