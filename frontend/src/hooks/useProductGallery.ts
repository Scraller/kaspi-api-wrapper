import { useState, useEffect } from 'react';
import { getProductDetails } from '@/services/api';

interface ProductGallery {
  images: string[];
  loading: boolean;
  error: boolean;
}

/**
 * Hook to fetch product gallery images for carousel display
 * Fetches detailed product info to get all available images
 */
export const useProductGallery = (productId: string, enabled: boolean = true) => {
  const [gallery, setGallery] = useState<ProductGallery>({
    images: [],
    loading: false,
    error: false
  });

  useEffect(() => {
    if (!enabled || !productId) {
      return;
    }

    let isCancelled = false;

    const fetchGallery = async () => {
      setGallery(prev => ({ ...prev, loading: true }));

      try {
        const productDetails = await getProductDetails(productId);
        
        if (isCancelled) return;

        // Get gallery images or fallback to single image
        const images = productDetails.galleryImages && productDetails.galleryImages.length > 0
          ? productDetails.galleryImages
          : []; // Will use the original imageUrl from search results

        setGallery({
          images,
          loading: false,
          error: false
        });

      } catch (error) {
        if (!isCancelled) {
          console.error('Failed to fetch gallery for product:', productId, error);
          setGallery({
            images: [],
            loading: false,
            error: true
          });
        }
      }
    };

    fetchGallery();

    return () => {
      isCancelled = true;
    };
  }, [productId, enabled]);

  return gallery;
};
