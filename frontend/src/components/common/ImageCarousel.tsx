import { useState } from 'react';
import Image from 'next/image';
import { Button } from '@/components/ui/button';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { getOptimizedImageUrl } from '@/utils/imageUtils';

interface ImageCarouselProps {
  images: string[];
  productName: string;
  className?: string;
}

/**
 * Image carousel component for product cards
 * Displays multiple product images with navigation controls
 * Optimizes images for better quality display
 */
export function ImageCarousel({ images, productName, className = '' }: ImageCarouselProps) {
  const [currentIndex, setCurrentIndex] = useState(0);
  
  // Filter out empty/invalid images and ensure we have at least one
  const validImages = images.filter(img => img && img.trim() !== '');
  
  if (validImages.length === 0) {
    return (
      <div className={`relative aspect-square ${className}`}>
        <Image
          src="/placeholder-product.svg"
          alt={productName}
          fill
          className="object-contain rounded-md"
          sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 25vw"
        />
      </div>
    );
  }

  // If only one image, show it without carousel controls
  if (validImages.length === 1) {
    return (
      <div className={`relative aspect-square ${className}`}>
        <Image
          src={getOptimizedImageUrl(validImages[0], 'preview-large')}
          alt={productName}
          fill
          className="object-contain rounded-md"
          sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 25vw"
        />
      </div>
    );
  }

  const goToPrevious = () => {
    setCurrentIndex((prevIndex) => 
      prevIndex === 0 ? validImages.length - 1 : prevIndex - 1
    );
  };

  const goToNext = () => {
    setCurrentIndex((prevIndex) => 
      prevIndex === validImages.length - 1 ? 0 : prevIndex + 1
    );
  };

  return (
    <div className={`relative aspect-square group ${className}`}>
      {/* Main image */}
      <Image
        src={getOptimizedImageUrl(validImages[currentIndex], 'preview-large')}
        alt={`${productName} - изображение ${currentIndex + 1}`}
        fill
        className="object-contain rounded-md transition-opacity duration-300"
        sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 25vw"
      />

      {/* Navigation arrows - only visible on hover */}
      <div className="absolute inset-0 flex items-center justify-between p-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
        <Button
          variant="secondary"
          size="icon"
          className="h-8 w-8 rounded-full bg-white/80 hover:bg-white shadow-md"
          onClick={goToPrevious}
          aria-label="Предыдущее изображение"
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>
        
        <Button
          variant="secondary"
          size="icon"
          className="h-8 w-8 rounded-full bg-white/80 hover:bg-white shadow-md"
          onClick={goToNext}
          aria-label="Следующее изображение"
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>

      {/* Image indicators/dots */}
      <div className="absolute bottom-2 left-1/2 transform -translate-x-1/2 flex space-x-1">
        {validImages.map((_, index) => (
          <button
            key={index}
            className={`w-2 h-2 rounded-full transition-colors duration-200 ${
              index === currentIndex 
                ? 'bg-white shadow-md' 
                : 'bg-white/50 hover:bg-white/75'
            }`}
            onClick={() => setCurrentIndex(index)}
            aria-label={`Показать изображение ${index + 1}`}
          />
        ))}
      </div>

      {/* Image counter */}
      {validImages.length > 1 && (
        <div className="absolute top-2 right-2 bg-black/60 text-white text-xs px-2 py-1 rounded-full">
          {currentIndex + 1} / {validImages.length}
        </div>
      )}
    </div>
  );
}
