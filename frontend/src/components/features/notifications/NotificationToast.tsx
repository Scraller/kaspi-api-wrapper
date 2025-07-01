import React from 'react';
import Image from 'next/image';
import { X, TrendingDown, TrendingUp, Minus } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { AnonymousSubscription } from '@/types/subscription';

/**
 * In-app notification toast component for price changes
 * Displays alongside browser notifications as fallback
 */
interface NotificationToastProps {
  subscription: AnonymousSubscription;
  newPrice: number;
  oldPrice: number;
  onDismiss: () => void;
  onViewProduct: () => void;
  isVisible: boolean;
}

export const NotificationToast: React.FC<NotificationToastProps> = ({
  subscription,
  newPrice,
  oldPrice,
  onDismiss,
  onViewProduct,
  isVisible,
}) => {
  if (!isVisible) return null;

  const priceDifference = newPrice - oldPrice;
  const percentageChange = ((priceDifference / oldPrice) * 100);
  const isDecrease = priceDifference < 0;
  const isIncrease = priceDifference > 0;

  const formatPrice = (price: number) => `${price.toLocaleString()} ₸`;

  /**
   * Gets appropriate icon for price change direction
   */
  const getPriceIcon = () => {
    if (isDecrease) return <TrendingDown className="text-green-600" size={20} />;
    if (isIncrease) return <TrendingUp className="text-red-600" size={20} />;
    return <Minus className="text-gray-600" size={20} />;
  };

  /**
   * Gets appropriate styles for price change
   */
  const getPriceChangeStyles = () => {
    if (isDecrease) return 'text-green-600 bg-green-100 border-green-200';
    if (isIncrease) return 'text-red-600 bg-red-100 border-red-200';
    return 'text-gray-600 bg-gray-100 border-gray-200';
  };

  /**
   * Gets appropriate title for notification
   */
  const getTitle = () => {
    if (isDecrease) return '💰 Price Drop Alert!';
    if (isIncrease) return '📈 Price Increase';
    return '📊 Price Update';
  };

  return (
    <div className="fixed top-4 right-4 z-50 animate-in slide-in-from-right-full duration-300">
      <Card className={`w-80 shadow-lg border-l-4 ${getPriceChangeStyles()}`}>
        <CardContent className="p-4">
          {/* Header with dismiss button */}
          <div className="flex items-start justify-between mb-3">
            <div className="flex items-center gap-2">
              {getPriceIcon()}
              <h3 className="font-semibold text-sm">
                {getTitle()}
              </h3>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={onDismiss}
              className="h-6 w-6 p-0 hover:bg-gray-100"
            >
              <X size={14} />
            </Button>
          </div>

          {/* Product info */}
          <div className="space-y-2">
            <div className="flex items-start gap-3">
              {subscription.productImage && (
                <Image
                  src={subscription.productImage}
                  alt={subscription.productName}
                  width={48}
                  height={48}
                  className="w-12 h-12 object-cover rounded-md bg-gray-100"
                  onError={(e) => {
                    e.currentTarget.src = '/placeholder-product.svg';
                  }}
                />
              )}
              <div className="flex-1 min-w-0">
                <p className="font-medium text-sm text-gray-900 line-clamp-2">
                  {subscription.productName}
                </p>
                <p className="text-xs text-gray-600 mt-1">
                  {subscription.merchantName}
                </p>
              </div>
            </div>

            {/* Price change info */}
            <div className="bg-white rounded-md p-3 border">
              <div className="flex items-center justify-between mb-1">
                <span className="text-xs text-gray-600">Old Price:</span>
                <span className="text-sm line-through text-gray-500">
                  {formatPrice(oldPrice)}
                </span>
              </div>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs text-gray-600">New Price:</span>
                <span className="text-sm font-semibold text-gray-900">
                  {formatPrice(newPrice)}
                </span>
              </div>
              
              {/* Price difference */}
              <div className="flex items-center justify-between">
                <span className="text-xs text-gray-600">
                  {isDecrease ? 'You Save:' : 'Increase:'}
                </span>
                <div className="text-right">
                  <span className={`text-sm font-medium ${
                    isDecrease ? 'text-green-600' : isIncrease ? 'text-red-600' : 'text-gray-600'
                  }`}>
                    {isDecrease ? '-' : '+'}{formatPrice(Math.abs(priceDifference))}
                  </span>
                  <span className={`block text-xs ${
                    isDecrease ? 'text-green-600' : isIncrease ? 'text-red-600' : 'text-gray-600'
                  }`}>
                    ({isDecrease ? '-' : '+'}{Math.abs(percentageChange).toFixed(1)}%)
                  </span>
                </div>
              </div>
            </div>

            {/* Threshold alert if applicable */}
            {subscription.priceThreshold && newPrice <= subscription.priceThreshold && (
              <div className="bg-green-50 border border-green-200 rounded-md p-2">
                <p className="text-xs text-green-700 font-medium">
                  🎯 Target price reached! Below {formatPrice(subscription.priceThreshold)}
                </p>
              </div>
            )}

            {/* Action buttons */}
            <div className="flex gap-2 pt-2">
              <Button
                onClick={onViewProduct}
                size="sm"
                className="flex-1 text-xs"
              >
                View Product
              </Button>
              {subscription.kaspiUrl && (
                <Button
                  onClick={() => window.open(subscription.kaspiUrl, '_blank')}
                  variant="outline"
                  size="sm"
                  className="flex-1 text-xs"
                >
                  Buy Now
                </Button>
              )}
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
