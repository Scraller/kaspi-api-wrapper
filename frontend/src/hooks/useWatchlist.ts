'use client';

import { useState, useEffect, useMemo } from 'react';
import { useSubscriptionStore } from '@/stores/subscriptionStore';
import { AnonymousSubscription } from '@/types/subscription';

export interface DashboardStats {
  total: number;
  priceDrops: number;
  priceIncreases: number;
  inStock: number;
  outOfStock: number;
  thresholdReached: number;
  expiringSoon: number;
  totalSavings: number;
}

/**
 * Custom hook for dashboard functionality
 * Manages subscription data, statistics, and provides dashboard-specific operations
 */
export function useWatchlist() {
  const {
    subscriptions,
    isLoading,
    error,
    loadSubscriptions,
    removeSubscription,
    clearError,
    stats: storeStats,
  } = useSubscriptionStore();

  const [lastUpdate, setLastUpdate] = useState<Date | null>(null);

  // Load subscriptions on hook initialization
  useEffect(() => {
    loadSubscriptions();
    setLastUpdate(new Date());
  }, [loadSubscriptions]);

  /**
   * Calculate detailed dashboard statistics
   */
  const dashboardStats: DashboardStats = useMemo(() => {
    let priceDrops = 0;
    let priceIncreases = 0;
    let inStock = 0;
    let outOfStock = 0;
    let thresholdReached = 0;
    let expiringSoon = 0;
    let totalSavings = 0;

    const now = new Date();
    const oneHourFromNow = new Date(now.getTime() + 60 * 60 * 1000);

    subscriptions.forEach(sub => {
      // Count price changes
      if (sub.priceChange === 'decrease') {
        priceDrops++;
        // Calculate potential savings
        if (sub.lastCheckedPrice && sub.lastCheckedPrice > sub.currentPrice) {
          totalSavings += sub.lastCheckedPrice - sub.currentPrice;
        }
      } else if (sub.priceChange === 'increase') {
        priceIncreases++;
      }

      // Count availability
      if (sub.availability === 'in_stock') {
        inStock++;
      } else {
        outOfStock++;
      }

      // Count threshold reached
      if (sub.priceThreshold && sub.currentPrice <= sub.priceThreshold) {
        thresholdReached++;
      }

      // Count expiring soon
      const expiresAt = typeof sub.expiresAt === 'string' ? new Date(sub.expiresAt) : sub.expiresAt;
      if (expiresAt <= oneHourFromNow) {
        expiringSoon++;
      }
    });

    return {
      total: subscriptions.length,
      priceDrops,
      priceIncreases,
      inStock,
      outOfStock,
      thresholdReached,
      expiringSoon,
      totalSavings,
    };
  }, [subscriptions]);

  /**
   * Handle subscription removal with proper error handling
   */
  const handleRemoveSubscription = async (productId: string, productName: string): Promise<void> => {
    const success = await removeSubscription(productId);
    if (!success) {
      throw new Error(`Failed to remove subscription for ${productName}`);
    }
  };

  /**
   * Refresh subscription data
   */
  const refreshData = () => {
    loadSubscriptions();
    setLastUpdate(new Date());
  };

  /**
   * Check if any subscriptions are expiring soon (within 1 hour)
   */
  const hasExpiringSoon = dashboardStats.expiringSoon > 0;

  /**
   * Check if there are any price alerts (threshold reached)
   */
  const hasPriceAlerts = dashboardStats.thresholdReached > 0;

  /**
   * Get subscriptions grouped by status
   */
  const groupedSubscriptions = useMemo(() => {
    const groups = {
      priceDrops: [] as AnonymousSubscription[],
      priceIncreases: [] as AnonymousSubscription[],
      thresholdReached: [] as AnonymousSubscription[],
      expiringSoon: [] as AnonymousSubscription[],
      noChange: [] as AnonymousSubscription[],
    };

    const now = new Date();
    const oneHourFromNow = new Date(now.getTime() + 60 * 60 * 1000);

    subscriptions.forEach(sub => {
      // Group by price change
      if (sub.priceChange === 'decrease') {
        groups.priceDrops.push(sub);
      } else if (sub.priceChange === 'increase') {
        groups.priceIncreases.push(sub);
      } else {
        groups.noChange.push(sub);
      }

      // Group by threshold reached
      if (sub.priceThreshold && sub.currentPrice <= sub.priceThreshold) {
        groups.thresholdReached.push(sub);
      }

      // Group by expiring soon
      const expiresAt = typeof sub.expiresAt === 'string' ? new Date(sub.expiresAt) : sub.expiresAt;
      if (expiresAt <= oneHourFromNow) {
        groups.expiringSoon.push(sub);
      }
    });

    return groups;
  }, [subscriptions]);

  return {
    // Data
    subscriptions,
    dashboardStats,
    groupedSubscriptions,
    
    // State
    isLoading,
    error,
    lastUpdate,
    
    // Computed flags
    hasExpiringSoon,
    hasPriceAlerts,
    isEmpty: subscriptions.length === 0,
    
    // Actions
    handleRemoveSubscription,
    refreshData,
    clearError,
    
    // Store stats (for limits)
    storeStats,
  };
}
