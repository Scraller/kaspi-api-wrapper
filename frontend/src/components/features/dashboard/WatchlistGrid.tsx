'use client';

import React, { useState, useMemo } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { 
  ExternalLink, 
  Trash2, 
  Clock, 
  Filter,
  ArrowUpDown,
  Eye,
  EyeOff,
  AlertTriangle
} from 'lucide-react';
import { AnonymousSubscription } from '@/types/subscription';
import { PriceChangeBadge } from './PriceIndicator';
import { ClickablePriceDisplay } from '../charts/ClickablePriceDisplay';
import { MerchantInfoInline } from '../merchant/MerchantInfo';
import { formatPrice } from '@/utils/priceUtils';
import Image from 'next/image';
import { getOptimizedImageUrl } from '@/utils/imageUtils';
import { toast } from 'sonner';

interface WatchlistGridProps {
  subscriptions: AnonymousSubscription[];
  onRemoveSubscription: (productId: string, productName: string) => Promise<void>;
  isLoading?: boolean;
}

type SortOption = 'date_added' | 'price_change' | 'alphabetical' | 'expiry' | 'price_low_to_high' | 'price_high_to_low';
type FilterOption = 'all' | 'price_drops' | 'price_increases' | 'in_stock' | 'threshold_reached' | 'expiring_soon';

/**
 * WatchlistGrid component displays subscribed products in a responsive grid layout
 * Includes sorting, filtering, and management functionality
 */
export function WatchlistGrid({ subscriptions, onRemoveSubscription, isLoading }: WatchlistGridProps) {
  const [sortBy, setSortBy] = useState<SortOption>('date_added');
  const [filterBy, setFilterBy] = useState<FilterOption>('all');
  const [removingId, setRemovingId] = useState<string | null>(null);

  /**
   * Handle subscription removal with loading state
   */
  const handleRemove = async (productId: string, productName: string) => {
    setRemovingId(productId);
    try {
      await onRemoveSubscription(productId, productName);
    } catch (error) {
      console.error('Error removing subscription:', error);
      toast.error('Failed to remove subscription');
    } finally {
      setRemovingId(null);
    }
  };

  /**
   * Filter and sort subscriptions based on user selection
   */
  const processedSubscriptions = useMemo(() => {
    let filtered = [...subscriptions];

    // Apply filters
    switch (filterBy) {
      case 'price_drops':
        filtered = filtered.filter(sub => sub.priceChange === 'decrease');
        break;
      case 'price_increases':
        filtered = filtered.filter(sub => sub.priceChange === 'increase');
        break;
      case 'in_stock':
        filtered = filtered.filter(sub => sub.availability === 'in_stock');
        break;
      case 'threshold_reached':
        filtered = filtered.filter(sub => 
          sub.priceThreshold && sub.currentPrice <= sub.priceThreshold
        );
        break;
      case 'expiring_soon':
        const oneHourFromNow = new Date(Date.now() + 60 * 60 * 1000);
        filtered = filtered.filter(sub => {
          const expiresAt = typeof sub.expiresAt === 'string' ? new Date(sub.expiresAt) : sub.expiresAt;
          return expiresAt <= oneHourFromNow;
        });
        break;
    }

    // Apply sorting
    filtered.sort((a, b) => {
      switch (sortBy) {
        case 'alphabetical':
          return a.productName.localeCompare(b.productName);
        
        case 'price_change':
          // Sort by price change: decreases first, then increases, then no change
          const aPriority = a.priceChange === 'decrease' ? 3 : a.priceChange === 'increase' ? 2 : 1;
          const bPriority = b.priceChange === 'decrease' ? 3 : b.priceChange === 'increase' ? 2 : 1;
          return bPriority - aPriority;
        
        case 'expiry':
          const aExpiry = typeof a.expiresAt === 'string' ? new Date(a.expiresAt) : a.expiresAt;
          const bExpiry = typeof b.expiresAt === 'string' ? new Date(b.expiresAt) : b.expiresAt;
          return aExpiry.getTime() - bExpiry.getTime();
        
        case 'price_low_to_high':
          return a.currentPrice - b.currentPrice;
        
        case 'price_high_to_low':
          return b.currentPrice - a.currentPrice;
        
        case 'date_added':
        default:
          const aAdded = typeof a.subscribedAt === 'string' ? new Date(a.subscribedAt) : a.subscribedAt;
          const bAdded = typeof b.subscribedAt === 'string' ? new Date(b.subscribedAt) : b.subscribedAt;
          return bAdded.getTime() - aAdded.getTime(); // Newest first
      }
    });

    return filtered;
  }, [subscriptions, sortBy, filterBy]);

  /**
   * Get time remaining until expiry
   */
  const getTimeRemaining = (expiresAt: Date): { text: string; isExpiring: boolean } => {
    const now = new Date();
    const expiry = typeof expiresAt === 'string' ? new Date(expiresAt) : expiresAt;
    const diffMs = expiry.getTime() - now.getTime();
    
    if (diffMs <= 0) return { text: 'Expired', isExpiring: true };
    
    const hours = Math.floor(diffMs / (1000 * 60 * 60));
    const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
    
    const isExpiring = hours < 2; // Consider expiring if less than 2 hours
    
    if (hours > 0) {
      return { text: `${hours}h ${minutes}m`, isExpiring };
    }
    return { text: `${minutes}m`, isExpiring };
  };

  /**
   * Handle external link to Kaspi.kz
   * Prefers stored kaspiUrl, falls back to constructing direct product URL
   */
  const handleExternalLink = (subscription: AnonymousSubscription) => {
    let kaspiUrl = subscription.kaspiUrl;
    
    // If no kaspiUrl is stored, try to construct a direct product URL
    if (!kaspiUrl && subscription.productId) {
      // Construct direct product URL format: https://kaspi.kz/shop/p/{productName}-{productId}/?c=750000000
      const productSlug = subscription.productName
        .toLowerCase()
        .replace(/[^a-z0-9\s-]/g, '') // Remove special characters
        .replace(/\s+/g, '-') // Replace spaces with hyphens
        .replace(/-+/g, '-') // Replace multiple hyphens with single
        .replace(/^-|-$/g, ''); // Remove leading/trailing hyphens
      
      kaspiUrl = `https://kaspi.kz/shop/p/${productSlug}-${subscription.productId}/?c=750000000`;
    }
    
    // Final fallback to search if still no URL
    if (!kaspiUrl) {
      kaspiUrl = `https://kaspi.kz/shop/search/?text=${encodeURIComponent(subscription.productName)}`;
    }
    
    window.open(kaspiUrl, '_blank', 'noopener,noreferrer');
  };

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {[...Array(6)].map((_, i) => (
          <Card key={i} className="h-80 animate-pulse">
            <div className="p-4">
              <div className="bg-gray-200 aspect-square rounded-md mb-4"></div>
              <div className="bg-gray-200 h-4 rounded mb-2"></div>
              <div className="bg-gray-200 h-4 rounded w-3/4"></div>
            </div>
          </Card>
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Controls */}
      <div className="flex flex-col sm:flex-row gap-4 justify-between items-start sm:items-center">
        <div className="flex flex-wrap gap-2">
          {/* Sort Options */}
          <Select value={sortBy} onValueChange={(value: SortOption) => setSortBy(value)}>
            <SelectTrigger className="w-[180px]">
              <ArrowUpDown className="h-4 w-4 mr-2" />
              <SelectValue placeholder="Sort by" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="date_added">Date Added</SelectItem>
              <SelectItem value="price_change">Price Change</SelectItem>
              <SelectItem value="alphabetical">Alphabetical</SelectItem>
              <SelectItem value="expiry">Expiry Time</SelectItem>
              <SelectItem value="price_low_to_high">Price: Low to High</SelectItem>
              <SelectItem value="price_high_to_low">Price: High to Low</SelectItem>
            </SelectContent>
          </Select>

          {/* Filter Options */}
          <Select value={filterBy} onValueChange={(value: FilterOption) => setFilterBy(value)}>
            <SelectTrigger className="w-[160px]">
              <Filter className="h-4 w-4 mr-2" />
              <SelectValue placeholder="Filter by" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Products</SelectItem>
              <SelectItem value="price_drops">Price Drops</SelectItem>
              <SelectItem value="price_increases">Price Increases</SelectItem>
              <SelectItem value="in_stock">In Stock</SelectItem>
              <SelectItem value="threshold_reached">Target Reached</SelectItem>
              <SelectItem value="expiring_soon">Expiring Soon</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Results count */}
        <div className="text-sm text-gray-600">
          {processedSubscriptions.length} of {subscriptions.length} products
        </div>
      </div>

      {/* Grid */}
      {processedSubscriptions.length === 0 ? (
        <Card className="p-8 text-center">
          <div className="text-gray-500">
            {filterBy === 'all' 
              ? "No products in your watchlist" 
              : "No products match the selected filter"
            }
          </div>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {processedSubscriptions.map((subscription) => {
            const timeRemaining = getTimeRemaining(subscription.expiresAt);
            const isRemoving = removingId === subscription.productId;
            
            // Calculate price change for border styling
            const getPriceChangeBorderClass = () => {
              if (!subscription.lastCheckedPrice || subscription.lastCheckedPrice === subscription.currentPrice) {
                return 'border-gray-200 dark:border-gray-700'; // No change
              }
              
              if (subscription.currentPrice < subscription.lastCheckedPrice) {
                return 'border-green-400 dark:border-green-600 shadow-green-100 dark:shadow-green-900/20'; // Price decreased (good)
              } else {
                return 'border-red-400 dark:border-red-600 shadow-red-100 dark:shadow-red-900/20'; // Price increased (bad)
              }
            };
            
            return (
              <Card 
                key={subscription.productId} 
                className={`h-full flex flex-col hover:shadow-lg transition-all duration-200 border-2 ${getPriceChangeBorderClass()}`}
              >
                {/* Product Image */}
                <div className="p-4">
                  <div className="relative aspect-square">
                    <Image
                      src={getOptimizedImageUrl(subscription.productImage, 'preview-medium')}
                      alt={subscription.productName}
                      fill
                      className="object-contain rounded-md"
                      sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
                    />
                    
                    {/* Price Change Badge Overlay */}
                    <div className="absolute top-2 right-2">
                      <PriceChangeBadge subscription={subscription} />
                    </div>
                  </div>
                </div>

                <CardContent className="flex-1 flex flex-col p-4 pt-0">
                  {/* Product Name */}
                  <h3 className="font-semibold text-sm line-clamp-2 mb-3">
                    {subscription.productName}
                  </h3>

                  {/* Price Information */}
                  <div className="mb-3">
                    <ClickablePriceDisplay 
                      subscription={subscription} 
                      showTrendIcon={true}
                      size="md"
                      className="w-full justify-start"
                    />
                  </div>

                  {/* Merchant and Availability */}
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex-1 min-w-0">
                      {subscription.merchantName && (
                        <MerchantInfoInline
                          merchantName={subscription.merchantName}
                          merchantId={subscription.merchantId}
                          merchantPhone={subscription.merchantPhone}
                          merchantUrl={subscription.merchantUrl}
                          className="truncate"
                        />
                      )}
                    </div>
                    <Badge 
                      variant={subscription.availability === 'in_stock' ? 'secondary' : 'outline'}
                      className={subscription.availability === 'in_stock' 
                        ? 'bg-green-100 text-green-700' 
                        : 'bg-red-100 text-red-700'
                      }
                    >
                      {subscription.availability === 'in_stock' ? 'In Stock' : 'Out of Stock'}
                    </Badge>
                  </div>

                  {/* Price Threshold Alert */}
                  {subscription.priceThreshold && subscription.currentPrice <= subscription.priceThreshold && (
                    <div className="mb-3">
                      <Badge className="bg-green-100 text-green-700 text-xs">
                        <AlertTriangle className="h-3 w-3 mr-1" />
                        Target price reached!
                      </Badge>
                    </div>
                  )}

                  {/* Time Remaining */}
                  <div className="mb-4">
                    <div className="flex items-center text-xs text-gray-500">
                      <Clock className="h-3 w-3 mr-1" />
                      <span className={timeRemaining.isExpiring ? 'text-amber-600 font-medium' : ''}>
                        {timeRemaining.text} remaining
                      </span>
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex gap-2 mt-auto">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleExternalLink(subscription)}
                      className="flex-1"
                    >
                      <ExternalLink className="h-4 w-4 mr-2" />
                      View on Kaspi
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleRemove(subscription.productId, subscription.productName)}
                      disabled={isRemoving}
                      className="text-red-600 hover:text-red-700 hover:bg-red-50"
                    >
                      {isRemoving ? (
                        <div className="h-4 w-4 animate-spin rounded-full border-2 border-red-600 border-t-transparent" />
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
                    </Button>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
