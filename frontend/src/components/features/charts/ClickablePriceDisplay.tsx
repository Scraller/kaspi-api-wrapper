'use client';

import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { TrendingUp, TrendingDown, Minus, BarChart3 } from 'lucide-react';
import { AnonymousSubscription } from '@/types/subscription';
import { PriceHistoryModal } from '../charts/PriceHistoryModal';

interface ClickablePriceDisplayProps {
  subscription: AnonymousSubscription;
  showTrendIcon?: boolean;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

/**
 * Clickable price display that opens price history chart when clicked
 * Shows current price with trend indicator and click-to-view history functionality
 */
export function ClickablePriceDisplay({ 
  subscription, 
  showTrendIcon = true,
  size = 'md',
  className = ""
}: ClickablePriceDisplayProps) {
  const [isHistoryModalOpen, setIsHistoryModalOpen] = useState(false);

  // Calculate price trend
  const getPriceTrend = () => {
    if (!subscription.lastCheckedPrice) return 'no_change';
    
    const current = subscription.currentPrice;
    const previous = subscription.lastCheckedPrice;
    
    if (current > previous) return 'increase';
    if (current < previous) return 'decrease';
    return 'no_change';
  };

  const priceTrend = getPriceTrend();
  
  // Calculate price change percentage
  const getPriceChangePercentage = () => {
    if (!subscription.lastCheckedPrice || subscription.lastCheckedPrice === subscription.currentPrice) {
      return 0;
    }
    
    const change = subscription.currentPrice - subscription.lastCheckedPrice;
    const percentage = (change / subscription.lastCheckedPrice) * 100;
    return Math.round(percentage * 100) / 100;
  };

  const priceChangePercent = getPriceChangePercentage();

  // Size-based styling
  const sizeClasses = {
    sm: 'text-sm px-2 py-1',
    md: 'text-base px-3 py-2',
    lg: 'text-lg px-4 py-3'
  };

  const iconSizes = {
    sm: 'h-3 w-3',
    md: 'h-4 w-4',
    lg: 'h-5 w-5'
  };

  // Trend styling
  const getTrendClasses = () => {
    switch (priceTrend) {
      case 'decrease':
        return 'text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800 hover:bg-green-100 dark:hover:bg-green-900/30';
      case 'increase':
        return 'text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800 hover:bg-red-100 dark:hover:bg-red-900/30';
      default:
        return 'text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-800 border-gray-200 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-700';
    }
  };

  const getTrendIcon = () => {
    const iconClass = iconSizes[size];
    switch (priceTrend) {
      case 'decrease':
        return <TrendingDown className={iconClass} />;
      case 'increase':
        return <TrendingUp className={iconClass} />;
      default:
        return <Minus className={iconClass} />;
    }
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('kk-KZ', {
      style: 'currency',
      currency: 'KZT',
      minimumFractionDigits: 0,
    }).format(price);
  };

  const hasHistory = subscription.priceHistory && subscription.priceHistory.length > 0;

  return (
    <>
      <Button
        variant="outline"
        onClick={() => setIsHistoryModalOpen(true)}
        className={`
          ${sizeClasses[size]} 
          ${getTrendClasses()} 
          border transition-all duration-200
          hover:shadow-md active:scale-95
          flex items-center gap-2 font-semibold
          ${className}
        `}
        disabled={!hasHistory}
        title={hasHistory ? "Click to view price history" : "No price history available yet"}
      >
        {/* Price Display */}
        <div className="flex items-center gap-1">
          <span>{formatPrice(subscription.currentPrice)}</span>
          {showTrendIcon && getTrendIcon()}
        </div>

        {/* Price Change Indicator */}
        {priceChangePercent !== 0 && subscription.lastCheckedPrice && (
          <div className={`text-xs flex flex-col items-start ${
            size === 'sm' ? 'text-xs' : 
            size === 'md' ? 'text-sm' : 
            'text-base'
          }`}>
            <div>
              ({priceChangePercent > 0 ? '+' : ''}{priceChangePercent}%)
            </div>
            <div className="opacity-75">
              {priceChangePercent > 0 ? '+' : ''}{formatPrice(subscription.currentPrice - subscription.lastCheckedPrice)}
            </div>
          </div>
        )}

        {/* Chart Icon */}
        <BarChart3 className={`${iconSizes[size]} opacity-60`} />
      </Button>

      {/* Price History Modal */}
      <PriceHistoryModal
        isOpen={isHistoryModalOpen}
        onClose={() => setIsHistoryModalOpen(false)}
        subscription={subscription}
      />
    </>
  );
}
