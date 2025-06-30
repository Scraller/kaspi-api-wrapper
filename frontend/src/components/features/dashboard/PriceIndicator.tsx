'use client';

import React from 'react';
import { TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { AnonymousSubscription } from '@/types/subscription';
import { formatPrice } from '@/utils/priceUtils';

interface PriceIndicatorProps {
  subscription: AnonymousSubscription;
  showChange?: boolean;
  showPercentage?: boolean;
  size?: 'sm' | 'md' | 'lg';
}

/**
 * PriceIndicator component displays price change information with visual indicators
 * Shows price direction, amount, and percentage change with appropriate colors
 */
export function PriceIndicator({ 
  subscription, 
  showChange = true, 
  showPercentage = true,
  size = 'md'
}: PriceIndicatorProps) {
  
  /**
   * Calculate price change information
   */
  const getPriceChange = () => {
    if (!subscription.lastCheckedPrice || !subscription.priceChange || subscription.priceChange === 'no_change') {
      return null;
    }

    const oldPrice = subscription.lastCheckedPrice;
    const newPrice = subscription.currentPrice;
    const change = newPrice - oldPrice;
    const percentage = Math.abs((change / oldPrice) * 100);

    return {
      amount: Math.abs(change),
      percentage: Math.round(percentage * 100) / 100,
      direction: subscription.priceChange,
      isIncrease: subscription.priceChange === 'increase',
      isDecrease: subscription.priceChange === 'decrease',
    };
  };

  const priceChange = getPriceChange();

  /**
   * Get appropriate styling based on price change direction
   */
  const getIndicatorStyles = () => {
    if (!priceChange) {
      return {
        badgeVariant: 'secondary' as const,
        textColor: 'text-gray-600',
        bgColor: 'bg-gray-100',
        icon: Minus,
      };
    }

    if (priceChange.isDecrease) {
      return {
        badgeVariant: 'secondary' as const,
        textColor: 'text-green-700',
        bgColor: 'bg-green-100',
        icon: TrendingDown,
      };
    }

    return {
      badgeVariant: 'secondary' as const,
      textColor: 'text-red-700',
      bgColor: 'bg-red-100',
      icon: TrendingUp,
    };
  };

  const styles = getIndicatorStyles();
  const IconComponent = styles.icon;

  /**
   * Get size-specific classes
   */
  const getSizeClasses = () => {
    switch (size) {
      case 'sm':
        return {
          icon: 'h-3 w-3',
          text: 'text-xs',
          badge: 'text-xs px-2 py-1',
        };
      case 'lg':
        return {
          icon: 'h-5 w-5',
          text: 'text-sm',
          badge: 'text-sm px-3 py-2',
        };
      default:
        return {
          icon: 'h-4 w-4',
          text: 'text-sm',
          badge: 'text-xs px-2 py-1',
        };
    }
  };

  const sizeClasses = getSizeClasses();

  // Current price display
  const currentPriceElement = (
    <span className={`font-semibold ${sizeClasses.text}`}>
      {formatPrice(subscription.currentPrice, 'KZT')}
    </span>
  );

  // If no price change to show, just return current price
  if (!priceChange || !showChange) {
    return currentPriceElement;
  }

  return (
    <div className="flex items-center space-x-2">
      {/* Current Price */}
      {currentPriceElement}
      
      {/* Price Change Indicator */}
      <Badge 
        variant={styles.badgeVariant}
        className={`${styles.bgColor} ${styles.textColor} ${sizeClasses.badge} flex items-center space-x-1`}
      >
        <IconComponent className={sizeClasses.icon} />
        <span>
          {priceChange.isDecrease ? '-' : '+'}
          {formatPrice(priceChange.amount, 'KZT')}
          {showPercentage && ` (${priceChange.percentage}%)`}
        </span>
      </Badge>

      {/* Threshold Alert */}
      {subscription.priceThreshold && 
       subscription.currentPrice <= subscription.priceThreshold && (
        <Badge variant="outline" className="bg-green-50 text-green-700 border-green-200">
          Target Reached!
        </Badge>
      )}
    </div>
  );
}

/**
 * Simplified price change badge for compact displays
 */
export function PriceChangeBadge({ subscription }: { subscription: AnonymousSubscription }) {
  const priceChange = subscription.priceChange;
  
  if (!priceChange || priceChange === 'no_change') {
    return null;
  }

  const isDecrease = priceChange === 'decrease';
  const icon = isDecrease ? TrendingDown : TrendingUp;
  const colorClass = isDecrease ? 'text-green-600' : 'text-red-600';

  return React.createElement(icon, { 
    className: `h-4 w-4 ${colorClass}`,
  });
}
