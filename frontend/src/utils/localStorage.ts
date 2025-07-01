import { AnonymousSubscription, STORAGE_KEY, SUBSCRIPTION_CONFIG } from '@/types/subscription';

/**
 * LocalStorage utilities for managing anonymous subscriptions
 * Handles data persistence, validation, and expiry for 24-hour anonymous tracking
 */

/**
 * Safely get subscriptions from localStorage with validation and expiry check
 * Removes expired subscriptions automatically
 */
export function getSubscriptions(): AnonymousSubscription[] {
  try {
    if (typeof window === 'undefined') return [];
    
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) return [];

    const subscriptions: AnonymousSubscription[] = JSON.parse(stored);
    
    // Validate and filter expired subscriptions
    const now = new Date();
    const validSubscriptions = subscriptions.filter(sub => {
      // Parse dates if they're strings
      const expiresAt = typeof sub.expiresAt === 'string' ? new Date(sub.expiresAt) : sub.expiresAt;
      return expiresAt > now;
    });

    // If we filtered out expired ones, update storage
    if (validSubscriptions.length !== subscriptions.length) {
      saveSubscriptions(validSubscriptions);
    }

    return validSubscriptions;
  } catch (error) {
    console.error('Error loading subscriptions from localStorage:', error);
    return [];
  }
}

/**
 * Save subscriptions to localStorage with error handling
 */
export function saveSubscriptions(subscriptions: AnonymousSubscription[]): boolean {
  try {
    if (typeof window === 'undefined') return false;
    
    localStorage.setItem(STORAGE_KEY, JSON.stringify(subscriptions));
    return true;
  } catch (error) {
    console.error('Error saving subscriptions to localStorage:', error);
    
    // Handle quota exceeded error
    if (error instanceof Error && error.name === 'QuotaExceededError') {
      // Try to clean up expired subscriptions and retry
      const validSubscriptions = subscriptions.filter(sub => {
        const expiresAt = typeof sub.expiresAt === 'string' ? new Date(sub.expiresAt) : sub.expiresAt;
        return expiresAt > new Date();
      });
      
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(validSubscriptions));
        return true;
      } catch (retryError) {
        console.error('Still unable to save after cleanup:', retryError);
      }
    }
    
    return false;
  }
}

/**
 * Add a new subscription with automatic expiry and limit checking
 */
export function addSubscription(
  productId: string,
  productName: string,
  currentPrice: number,
  options: {
    productImage?: string;
    priceThreshold?: number;
    merchantName?: string;
    merchantId?: string;
    merchantPhone?: string;
    merchantUrl?: string;
    availability?: 'in_stock' | 'out_of_stock';
    kaspiUrl?: string;
  } = {}
): { success: boolean; message: string } {
  try {
    const subscriptions = getSubscriptions();
    
    // Check if already subscribed
    if (subscriptions.some(sub => sub.productId === productId)) {
      return { success: false, message: 'Already subscribed to this product' };
    }
    
    // Check subscription limit
    if (subscriptions.length >= SUBSCRIPTION_CONFIG.maxSubscriptions) {
      return { 
        success: false, 
        message: `Maximum ${SUBSCRIPTION_CONFIG.maxSubscriptions} subscriptions allowed for anonymous users` 
      };
    }
    
    // Create new subscription with 24h expiry
    const now = new Date();
    const expiresAt = new Date(now.getTime() + SUBSCRIPTION_CONFIG.expiryHours * 60 * 60 * 1000);
    
    const newSubscription: AnonymousSubscription = {
      productId,
      productName,
      currentPrice,
      subscribedAt: now,
      expiresAt,
      lastCheckedPrice: currentPrice,
      priceChange: 'no_change',
      availability: options.availability || 'in_stock',
      merchantName: options.merchantName,
      merchantId: options.merchantId,
      merchantPhone: options.merchantPhone,
      merchantUrl: options.merchantUrl,
      kaspiUrl: options.kaspiUrl,
      productImage: options.productImage,
      priceThreshold: options.priceThreshold,
      priceHistory: [{
        timestamp: now,
        price: currentPrice,
        merchantName: options.merchantName,
        merchantId: options.merchantId,
        merchantPhone: options.merchantPhone,
        merchantUrl: options.merchantUrl
      }],
    };
    
    const updatedSubscriptions = [...subscriptions, newSubscription];
    const saved = saveSubscriptions(updatedSubscriptions);
    
    if (saved) {
      return { success: true, message: 'Successfully subscribed to price alerts' };
    } else {
      return { success: false, message: 'Failed to save subscription' };
    }
  } catch (error) {
    console.error('Error adding subscription:', error);
    return { success: false, message: 'Error adding subscription' };
  }
}

/**
 * Remove a subscription by product ID
 */
export function removeSubscription(productId: string): boolean {
  try {
    const subscriptions = getSubscriptions();
    const filteredSubscriptions = subscriptions.filter(sub => sub.productId !== productId);
    return saveSubscriptions(filteredSubscriptions);
  } catch (error) {
    console.error('Error removing subscription:', error);
    return false;
  }
}

/**
 * Update subscription price and calculate price change
 * Also stores price history for charting purposes
 */
export function updateSubscriptionPrice(
  productId: string, 
  newPrice: number, 
  merchantInfo?: {
    name?: string;
    id?: string;
    phone?: string;
    url?: string;
  }
): boolean {
  try {
    const subscriptions = getSubscriptions();
    const updatedSubscriptions = subscriptions.map(sub => {
      if (sub.productId === productId) {
        const previousPrice = sub.lastCheckedPrice || sub.currentPrice;
        let priceChange: 'increase' | 'decrease' | 'no_change' = 'no_change';
        
        if (newPrice > previousPrice) {
          priceChange = 'increase';
        } else if (newPrice < previousPrice) {
          priceChange = 'decrease';
        }
        
        // Add to price history if price actually changed
        const priceHistory = sub.priceHistory || [];
        if (newPrice !== previousPrice) {
          priceHistory.push({
            timestamp: new Date(),
            price: newPrice,
            merchantName: merchantInfo?.name || sub.merchantName,
            merchantId: merchantInfo?.id || sub.merchantId,
            merchantPhone: merchantInfo?.phone || sub.merchantPhone,
            merchantUrl: merchantInfo?.url || sub.merchantUrl
          });
          
          // Keep only last 100 entries to prevent localStorage bloat
          if (priceHistory.length > 100) {
            priceHistory.shift();
          }
        }
        
        return {
          ...sub,
          currentPrice: newPrice,
          lastCheckedPrice: previousPrice,
          priceChange,
          priceHistory,
          merchantName: merchantInfo?.name || sub.merchantName,
          merchantId: merchantInfo?.id || sub.merchantId,
          merchantPhone: merchantInfo?.phone || sub.merchantPhone,
          merchantUrl: merchantInfo?.url || sub.merchantUrl,
        };
      }
      return sub;
    });
    
    return saveSubscriptions(updatedSubscriptions);
  } catch (error) {
    console.error('Error updating subscription price:', error);
    return false;
  }
}

/**
 * Check if user is subscribed to a product
 */
export function isSubscribed(productId: string): boolean {
  const subscriptions = getSubscriptions();
  return subscriptions.some(sub => sub.productId === productId);
}

/**
 * Get subscription count and remaining slots
 */
export function getSubscriptionStats(): { count: number; remaining: number; limit: number } {
  const subscriptions = getSubscriptions();
  const count = subscriptions.length;
  const limit = SUBSCRIPTION_CONFIG.maxSubscriptions;
  
  return {
    count,
    remaining: Math.max(0, limit - count),
    limit,
  };
}

/**
 * Clear all expired subscriptions manually
 */
export function cleanupExpiredSubscriptions(): number {
  const subscriptions = getSubscriptions();
  const initialCount = subscriptions.length;
  // getSubscriptions() already filters expired ones and saves the result
  const currentCount = getSubscriptions().length;
  return initialCount - currentCount;
}
