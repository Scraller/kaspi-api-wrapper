import { AnonymousSubscription } from '@/types/subscription';
import { getProductDetails, getMerchantDetails } from '@/services/api';
import { updateSubscriptionPrice, getSubscriptions } from '@/utils/localStorage';
import { showPriceAlert } from './notifications';

/**
 * Price polling service that checks for price updates with configurable intervals
 * Supports intervals from 2 to 15 minutes with enhanced time tracking
 */

// Default polling interval: 15 minutes (as per MVP requirements)
const DEFAULT_POLLING_INTERVAL = 15 * 60 * 1000; // 15 minutes in milliseconds
const MIN_POLLING_INTERVAL_MINUTES = 2; // 2 minutes minimum
const MAX_POLLING_INTERVAL_MINUTES = 15; // 15 minutes maximum

let pollingInterval: NodeJS.Timeout | null = null;
let isPolling = false;
let lastCheckTime: Date | null = null;
let currentPollingIntervalMs = DEFAULT_POLLING_INTERVAL;

/**
 * Checks price updates for all active subscriptions
 * Core function that fetches current prices and compares with stored prices
 */
export const checkPriceUpdates = async (subscriptions: AnonymousSubscription[]): Promise<void> => {
  console.log(`🔍 Starting price check for ${subscriptions.length} subscriptions`);
  
  for (const subscription of subscriptions) {
    try {
      console.log(`📊 Checking price for product: ${subscription.productName}`);
      
      // Fetch current product data from API
      const productData = await getProductDetails(subscription.productId);
      
      // Find the best offer (lowest price) from all available offers
      if (!productData.offers || productData.offers.length === 0) {
        console.log(`⚠️ No offers available for ${subscription.productName}`);
        continue;
      }
      
      // Find the offer with the lowest price
      const bestOffer = productData.offers.reduce((best, current) => 
        current.price < best.price ? current : best
      );
      
      const currentPrice = bestOffer.price;
      
      // Fetch detailed merchant information including phone number
      let merchantInfo = {
        name: bestOffer.merchantName,
        id: bestOffer.id,
        phone: undefined as string | undefined,
        url: undefined as string | undefined
      };
      
      try {
        console.log(`📞 Fetching merchant details for: ${bestOffer.id}`);
        const merchantDetails = await getMerchantDetails(bestOffer.id);
        
        merchantInfo = {
          name: merchantDetails.name,
          id: merchantDetails.id,
          phone: merchantDetails.contactInfo?.phone,
          url: `https://kaspi.kz/shop/info/merchant/${merchantDetails.id}/address-tab/`
        };
        
        console.log(`✅ Merchant info updated: ${merchantInfo.name} - Phone: ${merchantInfo.phone || 'N/A'}`);
      } catch (merchantError) {
        console.warn(`⚠️ Failed to fetch merchant details for ${bestOffer.id}:`, merchantError);
        // Continue with basic merchant info from product offer
      }
      
      if (currentPrice !== (subscription.lastCheckedPrice || subscription.currentPrice)) {
        console.log(`💰 Price change detected for ${subscription.productName}: ${subscription.currentPrice} → ${currentPrice}`);
        
        const updated = await updateSubscriptionPrice(subscription.productId, currentPrice, merchantInfo);
        
        if (updated) {
          // Check if price dropped below threshold for notification
          if (subscription.priceThreshold && currentPrice <= subscription.priceThreshold) {
            console.log(`🚨 Price alert triggered for ${subscription.productName}: ${currentPrice} <= ${subscription.priceThreshold}`);
            await showPriceAlert(subscription, currentPrice);
          }
          
          // Also notify for any significant price drop (>5%)
          const previousPrice = subscription.lastCheckedPrice || subscription.currentPrice;
          const priceDropPercentage = ((previousPrice - currentPrice) / previousPrice) * 100;
          
          if (priceDropPercentage >= 5) {
            console.log(`📉 Significant price drop detected: ${priceDropPercentage.toFixed(1)}%`);
            await showPriceAlert(subscription, currentPrice, priceDropPercentage);
          }
        }
      } else {
        console.log(`✅ No price change for ${subscription.productName}`);
      }
    } catch (error) {
      console.error(`❌ Price check failed for product ${subscription.productId}:`, error);
      // Continue checking other products even if one fails
    }
    
    // Add small delay between API calls to avoid overwhelming the backend
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
  
  console.log('✅ Price check cycle completed');
};

/**
 * Starts the price polling service
 * Only starts if not already running and if there are active subscriptions
 */
export const startPricePolling = (): void => {
  if (isPolling) {
    console.log('⚠️ Price polling already running');
    return;
  }
  
  // Load saved polling interval
  const savedIntervalMinutes = getSavedPollingInterval();
  currentPollingIntervalMs = savedIntervalMinutes * 60 * 1000;
  
  // Initial check
  performPriceCheck();
  
  // Set up recurring polling with current interval
  pollingInterval = setInterval(performPriceCheck, currentPollingIntervalMs);
  isPolling = true;
  
  console.log(`🚀 Price polling started with ${savedIntervalMinutes}min interval`);
};

/**
 * Stops the price polling service
 * Cleans up the interval and resets state
 */
export const stopPricePolling = (): void => {
  if (pollingInterval) {
    clearInterval(pollingInterval);
    pollingInterval = null;
  }
  
  isPolling = false;
  console.log('⏹️ Price polling stopped');
};

/**
 * Performs a single price check cycle
 * Fetches subscriptions and checks their prices
 */
const performPriceCheck = async (): Promise<void> => {
  try {
    lastCheckTime = new Date(); // Update last check time
    const subscriptions = getSubscriptions();
    
    if (subscriptions.length === 0) {
      console.log('📭 No active subscriptions, skipping price check');
      return;
    }
    
    await checkPriceUpdates(subscriptions);
  } catch (error) {
    console.error('❌ Price check cycle failed:', error);
  }
};

/**
 * Checks if price polling is currently active
 */
export const isPricePollingActive = (): boolean => {
  return isPolling;
};

/**
 * Gets the current polling interval in milliseconds
 */
export const getPollingInterval = (): number => {
  return currentPollingIntervalMs;
};

/**
 * Handles page visibility changes to pause/resume polling when tab is inactive
 * This optimizes performance and reduces unnecessary API calls
 */
export const handleVisibilityChange = (): void => {
  if (typeof document === 'undefined') return;
  
  if (document.hidden) {
    console.log('👁️ Tab became inactive, continuing polling in background');
    // Keep polling even when tab is inactive for MVP
    // In production, you might want to reduce frequency or pause
  } else {
    console.log('👁️ Tab became active, ensuring polling is running');
    if (!isPolling) {
      startPricePolling();
    }
  }
};

/**
 * Gets the saved polling interval from localStorage (in minutes)
 * Returns default if not set or invalid
 */
export const getSavedPollingInterval = (): number => {
  try {
    if (typeof window === 'undefined') return 15;
    
    const saved = localStorage.getItem('kaspi_polling_interval');
    if (saved) {
      const minutes = parseInt(saved, 10);
      if (minutes >= MIN_POLLING_INTERVAL_MINUTES && minutes <= MAX_POLLING_INTERVAL_MINUTES) {
        return minutes;
      }
    }
  } catch (error) {
    console.error('❌ Failed to load polling interval:', error);
  }
  
  return 15; // Default to 15 minutes
};

/**
 * Sets the polling interval and saves to localStorage
 * Restarts polling with new interval if currently active
 */
export const setPollingInterval = (minutes: number): boolean => {
  try {
    if (minutes < MIN_POLLING_INTERVAL_MINUTES || minutes > MAX_POLLING_INTERVAL_MINUTES) {
      console.error(`❌ Invalid polling interval. Must be between ${MIN_POLLING_INTERVAL_MINUTES}-${MAX_POLLING_INTERVAL_MINUTES} minutes`);
      return false;
    }
    
    if (typeof window !== 'undefined') {
      localStorage.setItem('kaspi_polling_interval', minutes.toString());
    }
    
    currentPollingIntervalMs = minutes * 60 * 1000;
    console.log(`⏱️ Polling interval updated to ${minutes} minutes`);
    
    // Restart polling with new interval if currently active
    if (isPolling) {
      stopPricePolling();
      startPricePolling();
    }
    
    return true;
  } catch (error) {
    console.error('❌ Failed to set polling interval:', error);
    return false;
  }
};

/**
 * Gets the time elapsed since last price check
 */
export const getTimeElapsedSinceLastCheck = (): { minutes: number; seconds: number } | null => {
  if (!lastCheckTime) return null;
  
  const now = new Date();
  const elapsedMs = now.getTime() - lastCheckTime.getTime();
  const elapsedMinutes = Math.floor(elapsedMs / (60 * 1000));
  const elapsedSeconds = Math.floor((elapsedMs % (60 * 1000)) / 1000);
  
  return { minutes: elapsedMinutes, seconds: elapsedSeconds };
};

/**
 * Gets the time remaining until next price check
 */
export const getTimeRemainingUntilNextCheck = (): { minutes: number; seconds: number } | null => {
  if (!lastCheckTime || !isPolling) return null;
  
  const now = new Date();
  const nextCheckTime = new Date(lastCheckTime.getTime() + currentPollingIntervalMs);
  const remainingMs = nextCheckTime.getTime() - now.getTime();
  
  if (remainingMs <= 0) return { minutes: 0, seconds: 0 };
  
  const remainingMinutes = Math.floor(remainingMs / (60 * 1000));
  const remainingSeconds = Math.floor((remainingMs % (60 * 1000)) / 1000);
  
  return { minutes: remainingMinutes, seconds: remainingSeconds };
};

/**
 * Gets current polling interval in minutes
 */
export const getCurrentPollingIntervalMinutes = (): number => {
  return Math.floor(currentPollingIntervalMs / (60 * 1000));
};

/**
 * Gets the minimum allowed polling interval in minutes
 */
export const getMinPollingIntervalMinutes = (): number => {
  return MIN_POLLING_INTERVAL_MINUTES;
};

/**
 * Gets the maximum allowed polling interval in minutes
 */
export const getMaxPollingIntervalMinutes = (): number => {
  return MAX_POLLING_INTERVAL_MINUTES;
};

// Set up visibility change listener when service is imported
if (typeof window !== 'undefined' && typeof document !== 'undefined') {
  document.addEventListener('visibilitychange', handleVisibilityChange);
}
