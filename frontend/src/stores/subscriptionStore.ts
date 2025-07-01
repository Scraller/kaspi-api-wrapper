import { create } from 'zustand';
import { AnonymousSubscription } from '@/types/subscription';
import {
  getSubscriptions,
  addSubscription as addToLocalStorage,
  removeSubscription as removeFromLocalStorage,
  updateSubscriptionPrice as updatePriceInLocalStorage,
  isSubscribed as checkIsSubscribed,
  getSubscriptionStats,
} from '@/utils/localStorage';

/**
 * Zustand store for managing anonymous subscriptions with localStorage persistence
 * Provides reactive state management for subscription operations
 */
interface SubscriptionStore {
  // State
  subscriptions: AnonymousSubscription[];
  isLoading: boolean;
  error: string | null;
  
  // Stats
  stats: {
    count: number;
    remaining: number;
    limit: number;
  };
  
  // Actions
  loadSubscriptions: () => void;
  addSubscription: (
    productId: string,
    productName: string,
    currentPrice: number,
    options?: {
      productImage?: string;
      priceThreshold?: number;
      merchantName?: string;
      merchantId?: string;
      merchantPhone?: string;
      merchantUrl?: string;
      availability?: 'in_stock' | 'out_of_stock';
      kaspiUrl?: string;
    }
  ) => Promise<{ success: boolean; message: string }>;
  removeSubscription: (productId: string) => Promise<boolean>;
  updateSubscriptionPrice: (productId: string, newPrice: number) => Promise<boolean>;
  isSubscribed: (productId: string) => boolean;
  clearError: () => void;
  refreshStats: () => void;
}

export const useSubscriptionStore = create<SubscriptionStore>((set, get) => ({
  // Initial state
  subscriptions: [],
  isLoading: false,
  error: null,
  stats: {
    count: 0,
    remaining: 10,
    limit: 10,
  },

  // Load subscriptions from localStorage
  loadSubscriptions: () => {
    try {
      set({ isLoading: true, error: null });
      const subscriptions = getSubscriptions();
      const stats = getSubscriptionStats();
      set({ 
        subscriptions, 
        stats,
        isLoading: false 
      });
    } catch (error) {
      console.error('Error loading subscriptions:', error);
      set({ 
        error: 'Failed to load subscriptions',
        isLoading: false 
      });
    }
  },

  // Add new subscription
  addSubscription: async (productId, productName, currentPrice, options = {}) => {
    try {
      set({ isLoading: true, error: null });
      
      const result = addToLocalStorage(productId, productName, currentPrice, options);
      
      if (result.success) {
        // Reload subscriptions and stats from localStorage
        const subscriptions = getSubscriptions();
        const stats = getSubscriptionStats();
        set({ 
          subscriptions,
          stats,
          isLoading: false 
        });
      } else {
        set({ 
          error: result.message,
          isLoading: false 
        });
      }
      
      return result;
    } catch (error) {
      console.error('Error adding subscription:', error);
      const errorMessage = 'Failed to add subscription';
      set({ 
        error: errorMessage,
        isLoading: false 
      });
      return { success: false, message: errorMessage };
    }
  },

  // Remove subscription
  removeSubscription: async (productId) => {
    try {
      set({ isLoading: true, error: null });
      
      const success = removeFromLocalStorage(productId);
      
      if (success) {
        // Reload subscriptions and stats from localStorage
        const subscriptions = getSubscriptions();
        const stats = getSubscriptionStats();
        set({ 
          subscriptions,
          stats,
          isLoading: false 
        });
      } else {
        set({ 
          error: 'Failed to remove subscription',
          isLoading: false 
        });
      }
      
      return success;
    } catch (error) {
      console.error('Error removing subscription:', error);
      set({ 
        error: 'Failed to remove subscription',
        isLoading: false 
      });
      return false;
    }
  },

  // Update subscription price
  updateSubscriptionPrice: async (productId, newPrice) => {
    try {
      const success = updatePriceInLocalStorage(productId, newPrice);
      
      if (success) {
        // Reload subscriptions from localStorage to get updated data
        const subscriptions = getSubscriptions();
        set({ subscriptions });
      }
      
      return success;
    } catch (error) {
      console.error('Error updating subscription price:', error);
      return false;
    }
  },

  // Check if subscribed to product
  isSubscribed: (productId) => {
    return checkIsSubscribed(productId);
  },

  // Clear error state
  clearError: () => {
    set({ error: null });
  },

  // Refresh stats manually
  refreshStats: () => {
    const stats = getSubscriptionStats();
    set({ stats });
  },
}));
