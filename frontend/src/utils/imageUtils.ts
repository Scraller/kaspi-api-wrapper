/**
 * Image utility functions for handling Kaspi.kz image URLs
 * Provides functions to get different image resolutions and formats
 */

/**
 * Get optimized image URL for Kaspi.kz product images
 * Available formats: preview-small, preview-medium, preview-large, gallery
 */
export const getOptimizedImageUrl = (
  imageUrl: string | undefined, 
  format: 'preview-small' | 'preview-medium' | 'preview-large' | 'gallery' = 'preview-medium'
): string => {
  if (!imageUrl) {
    return '/placeholder-product.svg';
  }

  // Check if it's a Kaspi.kz CDN URL
  if (imageUrl.includes('resources.cdn-kaspi.kz')) {
    // Replace the format parameter or add it if not present
    if (imageUrl.includes('?format=')) {
      return imageUrl.replace(/\?format=[^&]*/, `?format=${format}`);
    } else {
      const separator = imageUrl.includes('?') ? '&' : '?';
      return `${imageUrl}${separator}format=${format}`;
    }
  }

  // Return original URL if not from Kaspi CDN
  return imageUrl;
};

/**
 * Get multiple image resolutions for responsive images
 * Returns srcset string for Next.js Image component
 */
export const getImageSrcSet = (imageUrl: string | undefined): string => {
  if (!imageUrl || !imageUrl.includes('resources.cdn-kaspi.kz')) {
    return '';
  }

  const small = getOptimizedImageUrl(imageUrl, 'preview-small');
  const medium = getOptimizedImageUrl(imageUrl, 'preview-medium');
  const large = getOptimizedImageUrl(imageUrl, 'preview-large');

  return `${small} 200w, ${medium} 400w, ${large} 800w`;
};

/**
 * Get the best quality image URL available
 * Prioritizes gallery format for product detail pages
 */
export const getHighQualityImageUrl = (imageUrl: string | undefined): string => {
  return getOptimizedImageUrl(imageUrl, 'gallery');
};
