import { AnonymousSubscription } from '@/types/subscription';

export interface PriceChangeCalculation {
  amount: number;
  percentage: number;
  direction: 'increase' | 'decrease' | 'no_change';
  isSignificant: boolean; // > 5% change
  isBargain: boolean; // > 20% decrease
}

/**
 * Calculate price change information between current and previous price
 * Provides detailed analysis of price movements for dashboard display
 */
export function calculatePriceChange(
  currentPrice: number,
  previousPrice?: number
): PriceChangeCalculation {
  // No previous price data
  if (!previousPrice || previousPrice === currentPrice) {
    return {
      amount: 0,
      percentage: 0,
      direction: 'no_change',
      isSignificant: false,
      isBargain: false,
    };
  }

  const change = currentPrice - previousPrice;
  const percentage = Math.abs((change / previousPrice) * 100);
  const direction = change > 0 ? 'increase' : change < 0 ? 'decrease' : 'no_change';

  return {
    amount: Math.abs(change),
    percentage: Math.round(percentage * 100) / 100,
    direction,
    isSignificant: percentage > 5,
    isBargain: direction === 'decrease' && percentage > 20,
  };
}

/**
 * Calculate potential savings from all price drops
 */
export function calculateTotalSavings(subscriptions: AnonymousSubscription[]): number {
  return subscriptions.reduce((total, sub) => {
    if (sub.priceChange === 'decrease' && sub.lastCheckedPrice) {
      return total + (sub.lastCheckedPrice - sub.currentPrice);
    }
    return total;
  }, 0);
}

/**
 * Get best price from subscription history
 */
export function getBestPrice(subscription: AnonymousSubscription): number {
  const prices = [subscription.currentPrice];
  if (subscription.lastCheckedPrice) {
    prices.push(subscription.lastCheckedPrice);
  }
  return Math.min(...prices);
}

/**
 * Check if current price meets the user's threshold
 */
export function isThresholdMet(subscription: AnonymousSubscription): boolean {
  return !!(subscription.priceThreshold && subscription.currentPrice <= subscription.priceThreshold);
}

/**
 * Get price trend direction over time
 */
export function getPriceTrend(subscription: AnonymousSubscription): 'up' | 'down' | 'stable' {
  if (!subscription.lastCheckedPrice) return 'stable';
  
  const change = subscription.currentPrice - subscription.lastCheckedPrice;
  const threshold = subscription.lastCheckedPrice * 0.01; // 1% threshold for "stable"
  
  if (Math.abs(change) <= threshold) return 'stable';
  return change > 0 ? 'up' : 'down';
}

/**
 * Format price change for display
 */
export function formatPriceChange(
  calculation: PriceChangeCalculation,
  currency: string = 'KZT'
): string {
  if (calculation.direction === 'no_change') {
    return 'No change';
  }

  const sign = calculation.direction === 'increase' ? '+' : '-';
  const amount = new Intl.NumberFormat('kk-KZ', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
  }).format(calculation.amount);

  return `${sign}${amount} (${calculation.percentage}%)`;
}

/**
 * Get recommendation based on simple price rules
 */
export function getPriceRecommendation(subscription: AnonymousSubscription): {
  type: 'buy_now' | 'wait' | 'set_alert' | 'good_deal';
  message: string;
} {
  const change = calculatePriceChange(subscription.currentPrice, subscription.lastCheckedPrice);

  // Threshold reached
  if (isThresholdMet(subscription)) {
    return {
      type: 'buy_now',
      message: 'Target price reached! Consider purchasing now.',
    };
  }

  // Big discount
  if (change.isBargain) {
    return {
      type: 'good_deal',
      message: `Great deal! Price dropped by ${change.percentage}%`,
    };
  }

  // Price increased significantly
  if (change.direction === 'increase' && change.isSignificant) {
    return {
      type: 'wait',
      message: 'Price increased recently. Consider waiting for a drop.',
    };
  }

  // No threshold set
  if (!subscription.priceThreshold) {
    return {
      type: 'set_alert',
      message: 'Set a target price to get notified when this product goes on sale.',
    };
  }

  return {
    type: 'wait',
    message: 'Monitoring price changes...',
  };
}
