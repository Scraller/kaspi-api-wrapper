import Image from 'next/image';
import { Card, CardContent, CardFooter } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ProductSummaryResponse } from '@/types';
import { Heart, ExternalLink, Star, TrendingDown } from 'lucide-react';
import { getOptimizedImageUrl } from '@/utils/imageUtils';
import { useBestPrice } from '@/hooks/useBestPrice';
import { useProductGallery } from '@/hooks/useProductGallery';
import { ImageCarousel } from '@/components/common/ImageCarousel';

interface ProductCardProps {
  product: ProductSummaryResponse;
  onSubscribe?: (productId: string) => void;
  isSubscribed?: boolean;
  showRealTimePrice?: boolean; // New prop to control real-time price fetching
  showImageCarousel?: boolean; // New prop to control image carousel
}

/**
 * ProductCard component displays individual product information
 * Includes image, name, price, rating, and subscription functionality
 * Used in search results and dashboard grids
 */
export function ProductCard({ 
  product, 
  onSubscribe, 
  isSubscribed = false, 
  showRealTimePrice = false,
  showImageCarousel = false
}: ProductCardProps) {
  // Fetch real-time best price if enabled
  const bestPrice = useBestPrice(product.id, showRealTimePrice);
  
  // Fetch product gallery if carousel is enabled
  const gallery = useProductGallery(product.id, showImageCarousel);
  
  // Use best price if available and different from search price
  const displayPrice = bestPrice && !bestPrice.loading && !bestPrice.error && bestPrice.price > 0 
    ? bestPrice.price 
    : product.price;
    
  const hasBetterPrice = bestPrice && !bestPrice.loading && !bestPrice.error && 
    bestPrice.price > 0 && bestPrice.price < product.price;

  // Determine which images to show in carousel
  const carouselImages = showImageCarousel && gallery.images.length > 0 
    ? gallery.images 
    : product.imageUrl ? [product.imageUrl] : [];
  /**
   * Format price with currency symbol
   * Ensures consistent price display across the app
   */
  const formatPrice = (price: number, currency: string = 'KZT') => {
    return new Intl.NumberFormat('kk-KZ', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 0,
    }).format(price);
  };

  /**
   * Handle subscribe button click
   * Prevents action if no handler provided
   */
  const handleSubscribe = () => {
    if (onSubscribe) {
      onSubscribe(product.id);
    }
  };

  /**
   * Handle external link click - opens product on Kaspi.kz
   * Generates URL from product slug and ID
   */
  const handleExternalLink = () => {
    const kaspiUrl = `https://kaspi.kz/shop/p/${product.slug}-${product.id}/`;
    window.open(kaspiUrl, '_blank', 'noopener,noreferrer');
  };

  return (
    <Card className="h-full flex flex-col hover:shadow-lg transition-shadow duration-200">
      {/* Product Image Carousel */}
      <div className="p-4">
        {showImageCarousel ? (
          <ImageCarousel 
            images={carouselImages}
            productName={product.name}
          />
        ) : (
          <div className="relative aspect-square">
            <Image
              src={getOptimizedImageUrl(product.imageUrl, 'preview-large')}
              alt={product.name}
              fill
              className="object-contain rounded-md"
              sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 25vw"
              priority={false}
            />
          </div>
        )}
      </div>

      <CardContent className="flex-1 p-4">
        {/* Product Name */}
        <h3 className="font-semibold text-sm line-clamp-2 mb-2">
          {product.name}
        </h3>

        {/* Price and availability */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <div className="flex flex-col">
              <div className="text-lg font-bold text-primary flex items-center gap-1">
                {formatPrice(displayPrice, product.currency)}
                {hasBetterPrice && (
                  <TrendingDown className="h-4 w-4 text-green-600" />
                )}
                {bestPrice?.loading && showRealTimePrice && (
                  <div className="text-xs text-muted-foreground">Проверяем цену...</div>
                )}
              </div>
              {bestPrice?.merchantName && hasBetterPrice && (
                <div className="text-xs text-green-600">
                  Лучшая цена от {bestPrice.merchantName}
                </div>
              )}
            </div>
            
            {/* Pricing info badge */}
            <Badge variant="outline" className="text-xs bg-green-50 text-green-700 border-green-200">
              {showRealTimePrice ? 'Актуальная цена' : 'Цены на Kaspi'}
            </Badge>
          </div>
          
          {/* Availability badge */}
          <div>
            <Badge 
              variant={product.availability === 'in_stock' ? 'default' : 'secondary'}
              className="text-xs"
            >
              {product.availability === 'in_stock' ? 'В наличии' : 'Нет в наличии'}
            </Badge>
          </div>
        </div>

        {/* Rating and Reviews */}
        {product.rating && (
          <div className="flex items-center gap-1 text-sm text-muted-foreground">
            <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />
            <span>{product.rating}</span>
            {product.reviewCount && (
              <span>({product.reviewCount} отзывов)</span>
            )}
          </div>
        )}

        {/* Category */}
        <div className="text-xs text-muted-foreground mt-1">
          {product.category}
        </div>
      </CardContent>

      <CardFooter className="p-4 pt-0 flex gap-2">
        {/* Subscribe Button */}
        {onSubscribe && (
          <Button
            variant={isSubscribed ? "default" : "outline"}
            size="sm"
            onClick={handleSubscribe}
            className="flex-1"
          >
            <Heart className={`h-4 w-4 mr-1 ${isSubscribed ? 'fill-current' : ''}`} />
            {isSubscribed ? 'Отслеживается' : 'Отслеживать'}
          </Button>
        )}

        {/* External Link - opens product on Kaspi.kz */}
        <Button 
          variant="outline" 
          size="sm"
          onClick={handleExternalLink}
          title="Открыть на Kaspi.kz"
        >
          <ExternalLink className="h-4 w-4" />
        </Button>
      </CardFooter>
    </Card>
  );
}
