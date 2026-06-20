import { useCallback, useEffect, useRef, useState } from 'react';
import { ChevronLeft, ChevronRight, X, ZoomIn, ZoomOut, RotateCcw } from 'lucide-react';

const MIN_ZOOM = 1;
const MAX_ZOOM = 4;
const ZOOM_STEP = 0.5;

interface ImageLightboxProps {
  images: string[];
  initialIndex?: number;
  alt?: string;
  onClose: () => void;
}

export function ImageLightbox({ images, initialIndex = 0, alt = '', onClose }: ImageLightboxProps) {
  const [index, setIndex] = useState(initialIndex);
  const [zoom, setZoom] = useState(1);
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const [dragging, setDragging] = useState(false);
  const dragStart = useRef({ x: 0, y: 0, panX: 0, panY: 0 });

  const resetView = useCallback(() => {
    setZoom(1);
    setPan({ x: 0, y: 0 });
  }, []);

  useEffect(() => {
    setIndex(initialIndex);
    resetView();
  }, [initialIndex, resetView]);

  useEffect(() => {
    document.body.style.overflow = 'hidden';
    return () => { document.body.style.overflow = ''; };
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
      if (e.key === 'ArrowLeft' && images.length > 1) {
        setIndex((i) => (i - 1 + images.length) % images.length);
        resetView();
      }
      if (e.key === 'ArrowRight' && images.length > 1) {
        setIndex((i) => (i + 1) % images.length);
        resetView();
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [images.length, onClose, resetView]);

  const zoomIn = () => setZoom((z) => Math.min(MAX_ZOOM, z + ZOOM_STEP));
  const zoomOut = () => {
    setZoom((z) => {
      const next = Math.max(MIN_ZOOM, z - ZOOM_STEP);
      if (next === MIN_ZOOM) setPan({ x: 0, y: 0 });
      return next;
    });
  };

  const onWheel = (e: React.WheelEvent) => {
    e.preventDefault();
    if (e.deltaY < 0) zoomIn();
    else zoomOut();
  };

  const onPointerDown = (e: React.PointerEvent) => {
    if (zoom <= 1) return;
    setDragging(true);
    dragStart.current = { x: e.clientX, y: e.clientY, panX: pan.x, panY: pan.y };
    (e.target as HTMLElement).setPointerCapture(e.pointerId);
  };

  const onPointerMove = (e: React.PointerEvent) => {
    if (!dragging) return;
    setPan({
      x: dragStart.current.panX + (e.clientX - dragStart.current.x),
      y: dragStart.current.panY + (e.clientY - dragStart.current.y),
    });
  };

  const onPointerUp = () => setDragging(false);

  const goPrev = () => {
    setIndex((i) => (i - 1 + images.length) % images.length);
    resetView();
  };

  const goNext = () => {
    setIndex((i) => (i + 1) % images.length);
    resetView();
  };

  return (
    <div className="fixed inset-0 z-[100] flex flex-col bg-black/95" role="dialog" aria-modal="true">
      <div className="flex items-center justify-between gap-2 px-3 py-3 sm:px-4 shrink-0 safe-area-pt">
        <div className="flex items-center gap-1 sm:gap-2">
          <button
            type="button"
            onClick={zoomOut}
            disabled={zoom <= MIN_ZOOM}
            className="p-2.5 rounded-xl text-white/90 hover:bg-white/10 disabled:opacity-40"
            aria-label="Zoom out"
          >
            <ZoomOut className="w-5 h-5" />
          </button>
          <span className="text-white/80 text-sm font-medium min-w-[3rem] text-center">{Math.round(zoom * 100)}%</span>
          <button
            type="button"
            onClick={zoomIn}
            disabled={zoom >= MAX_ZOOM}
            className="p-2.5 rounded-xl text-white/90 hover:bg-white/10 disabled:opacity-40"
            aria-label="Zoom in"
          >
            <ZoomIn className="w-5 h-5" />
          </button>
          <button
            type="button"
            onClick={resetView}
            className="p-2.5 rounded-xl text-white/90 hover:bg-white/10"
            aria-label="Reset zoom"
          >
            <RotateCcw className="w-5 h-5" />
          </button>
        </div>

        {images.length > 1 && (
          <p className="text-white/70 text-sm hidden sm:block">
            {index + 1} / {images.length}
          </p>
        )}

        <button
          type="button"
          onClick={onClose}
          className="p-2.5 rounded-xl text-white hover:bg-white/10"
          aria-label="Close"
        >
          <X className="w-6 h-6" />
        </button>
      </div>

      <div
        className="flex-1 relative flex items-center justify-center overflow-hidden touch-none"
        onWheel={onWheel}
        onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
      >
        {images.length > 1 && (
          <>
            <button
              type="button"
              onClick={goPrev}
              className="absolute left-2 sm:left-4 z-10 p-3 rounded-full bg-white/10 hover:bg-white/20 text-white"
              aria-label="Previous image"
            >
              <ChevronLeft className="w-6 h-6" />
            </button>
            <button
              type="button"
              onClick={goNext}
              className="absolute right-2 sm:right-4 z-10 p-3 rounded-full bg-white/10 hover:bg-white/20 text-white"
              aria-label="Next image"
            >
              <ChevronRight className="w-6 h-6" />
            </button>
          </>
        )}

        <img
          src={images[index]}
          alt={alt}
          draggable={false}
          className={`max-w-[min(100%,900px)] max-h-[min(80dvh,900px)] object-contain select-none transition-transform duration-150 ${
            zoom > 1 ? (dragging ? 'cursor-grabbing' : 'cursor-grab') : 'cursor-zoom-in'
          }`}
          style={{ transform: `translate(${pan.x}px, ${pan.y}px) scale(${zoom})` }}
          onClick={(e) => {
            e.stopPropagation();
            if (zoom === 1) zoomIn();
          }}
          onPointerDown={onPointerDown}
          onPointerMove={onPointerMove}
          onPointerUp={onPointerUp}
          onPointerCancel={onPointerUp}
        />
      </div>

      <p className="text-center text-white/50 text-xs pb-4 safe-area-pb px-4">
        Scroll or tap image to zoom · Drag when zoomed · Esc to close
      </p>
    </div>
  );
}
