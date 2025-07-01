// Subscription types for anonymous localStorage-based subscriptions
export interface PriceHistoryEntry {
  timestamp: Date;
  price: number;
  merchantName?: string;
  merchantId?: string;
  merchantPhone?: string;
  merchantUrl?: string;
}

export interface MerchantInfo {
  id: string;
  name: string;
  phone?: string;
  url?: string;
}

export interface AnonymousSubscription {
  productId: string;
  productName: string;
  productImage?: string;
  currentPrice: number;
  priceThreshold?: number;
  subscribedAt: Date;
  expiresAt: Date; // 24h from creation
  lastCheckedPrice?: number;
  priceChange?: 'increase' | 'decrease' | 'no_change';
  merchantName?: string;
  merchantId?: string;
  merchantPhone?: string;
  merchantUrl?: string;
  availability: 'in_stock' | 'out_of_stock';
  kaspiUrl?: string;
  priceHistory?: PriceHistoryEntry[]; // Store price changes over time
}

export interface SubscriptionLimits {
  maxSubscriptions: number;
  expiryHours: number;
}

export interface PriceChangeInfo {
  amount: number;
  percentage: number;
  direction: 'increase' | 'decrease' | 'no_change';
}

// Configuration constants
export const SUBSCRIPTION_CONFIG: SubscriptionLimits = {
  maxSubscriptions: 10,
  expiryHours: 24,
};

export const STORAGE_KEY = 'kaspi_subscriptions';
