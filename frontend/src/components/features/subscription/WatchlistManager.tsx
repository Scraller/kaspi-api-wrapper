'use client';

import React, { useEffect, useState } from 'react';
import Image from 'next/image';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { AlertCircle, Clock, Trash2, ExternalLink, TrendingUp, TrendingDown } from 'lucide-react';
import { useSubscriptionStore } from '@/stores/subscriptionStore';
import { formatPrice } from '@/utils/priceUtils';
import { AnonymousSubscription } from '@/types/subscription';
import { toast } from 'sonner';

interface WatchlistManagerProps {
  className?: string;
}

/**
 * Component for managing user's watchlist subscriptions
 * Shows all subscribed products with ability to remove and view details
 */
export function WatchlistManager({ className }: WatchlistManagerProps) {
  const {
    subscriptions,
    stats,
    loadSubscriptions,
    removeSubscription,
    isLoading,
    error,
    clearError,
  } = useSubscriptionStore();

  const [removingId, setRemovingId] = useState<string | null>(null);

  // Load subscriptions on component mount
  useEffect(() => {
    loadSubscriptions();
  }, [loadSubscriptions]);

  /**
   * Handle subscription removal with loading state
   */
  const handleRemoveSubscription = async (productId: string, productName: string) => {
    setRemovingId(productId);
    try {
      const success = await removeSubscription(productId);
      if (success) {
        toast.success(`Removed "${productName}" from watchlist`);
      } else {
        toast.error('Failed to remove subscription');
      }
    } catch (error) {
      console.error('Error removing subscription:', error);
      toast.error('Something went wrong');
    } finally {
      setRemovingId(null);
    }
  };

  /**
   * Calculate time remaining until expiry
   */
  const getTimeRemaining = (expiresAt: Date): string => {
    const now = new Date();
    const expiry = typeof expiresAt === 'string' ? new Date(expiresAt) : expiresAt;
    const diffMs = expiry.getTime() - now.getTime();
    
    if (diffMs <= 0) return 'Expired';
    
    const hours = Math.floor(diffMs / (1000 * 60 * 60));
    const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
    
    if (hours > 0) {
      return `${hours}h ${minutes}m remaining`;
    }
    return `${minutes}m remaining`;
  };

  /**
   * Get price change indicator
   */
  const getPriceChangeIndicator = (subscription: AnonymousSubscription) => {
    if (!subscription.priceChange || subscription.priceChange === 'no_change') {
      return null;
    }

    const isIncrease = subscription.priceChange === 'increase';
    const icon = isIncrease ? TrendingUp : TrendingDown;
    const color = isIncrease ? 'text-red-600' : 'text-green-600';

    return React.createElement(icon, { className: `h-4 w-4 ${color}` });
  };

  if (error) {
    return (
      <Card className={className}>
        <CardContent className="pt-6">
          <div className="flex items-center space-x-2 text-red-600">
            <AlertCircle className="h-5 w-5" />
            <span>{error}</span>
            <Button variant="outline" size="sm" onClick={clearError}>
              Retry
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className={className}>
      {/* Stats Card */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Watchlist Summary</CardTitle>
          <CardDescription>
            Track up to {stats.limit} products with anonymous 24-hour subscriptions
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div className="space-y-1">
              <div className="text-2xl font-bold">{stats.count}</div>
              <div className="text-sm text-gray-600">
                Products tracked
              </div>
            </div>
            <div className="space-y-1 text-right">
              <div className="text-lg font-semibold text-green-600">{stats.remaining}</div>
              <div className="text-sm text-gray-600">
                Slots remaining
              </div>
            </div>
          </div>
          
          {stats.count > 0 && (
            <div className="mt-4 w-full bg-gray-200 rounded-full h-2">
              <div
                className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                style={{ width: `${(stats.count / stats.limit) * 100}%` }}
              />
            </div>
          )}
        </CardContent>
      </Card>

      {/* Subscriptions List */}
      {isLoading ? (
        <Card>
          <CardContent className="pt-6">
            <div className="animate-pulse space-y-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-20 bg-gray-200 rounded" />
              ))}
            </div>
          </CardContent>
        </Card>
      ) : subscriptions.length === 0 ? (
        <Card>
          <CardContent className="pt-6 text-center">
            <div className="text-gray-500 space-y-2">
              <div className="text-lg font-medium">No products in watchlist</div>
              <div className="text-sm">
                Start tracking prices by searching for products and clicking &ldquo;Watch Price&rdquo;
              </div>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {subscriptions.map((subscription) => (
            <Card key={subscription.productId}>
              <CardContent className="pt-6">
                <div className="flex items-start space-x-4">
                  {/* Product Image */}
                  {subscription.productImage && (
                    <div className="flex-shrink-0">
                      <Image
                        src={subscription.productImage}
                        alt={subscription.productName}
                        width={64}
                        height={64}
                        className="w-16 h-16 object-cover rounded border"
                      />
                    </div>
                  )}

                  {/* Product Info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <h3 className="font-medium text-gray-900 truncate">
                          {subscription.productName}
                        </h3>
                        
                        {/* Price Info */}
                        <div className="flex items-center space-x-2 mt-1">
                          <span className="text-lg font-semibold">
                            {formatPrice(subscription.currentPrice, 'KZT')}
                          </span>
                          {getPriceChangeIndicator(subscription)}
                          
                          {/* Availability Badge */}
                          <Badge 
                            variant={subscription.availability === 'in_stock' ? 'default' : 'secondary'}
                          >
                            {subscription.availability === 'in_stock' ? 'In Stock' : 'Out of Stock'}
                          </Badge>
                        </div>

                        {/* Price Threshold */}
                        {subscription.priceThreshold && (
                          <div className="text-sm text-gray-600 mt-1">
                            Alert when below: {formatPrice(subscription.priceThreshold, 'KZT')}
                          </div>
                        )}

                        {/* Time Remaining */}
                        <div className="flex items-center text-sm text-gray-500 mt-2">
                          <Clock className="h-4 w-4 mr-1" />
                          {getTimeRemaining(subscription.expiresAt)}
                        </div>
                      </div>

                      {/* Actions */}
                      <div className="flex space-x-2 ml-4">
                        {/* Visit Kaspi Link */}
                        {subscription.kaspiUrl && (
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => window.open(subscription.kaspiUrl, '_blank')}
                          >
                            <ExternalLink className="h-4 w-4" />
                          </Button>
                        )}

                        {/* Remove Button */}
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleRemoveSubscription(subscription.productId, subscription.productName)}
                          disabled={removingId === subscription.productId}
                          className="text-red-600 hover:text-red-700 hover:border-red-300"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
