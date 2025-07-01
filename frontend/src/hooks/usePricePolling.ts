import { useState, useEffect, useCallback, useRef } from 'react';
import { AnonymousSubscription } from '@/types/subscription';
import { 
  startPricePolling,
  stopPricePolling,
  isPricePollingActive,
  getPollingInterval,
  checkPriceUpdates
} from '@/services/pricePolling';
import { useSubscriptionStore } from '@/stores/subscriptionStore';

/**
 * Custom hook for managing price polling functionality
 * Provides reactive interface for price monitoring and updates
 */
export const usePricePolling = () => {
  const [isActive, setIsActive] = useState(false);
  const [lastCheckTime, setLastCheckTime] = useState<Date | null>(null);
  const [isChecking, setIsChecking] = useState(false);
  const [checkCount, setCheckCount] = useState(0);
  const [error, setError] = useState<string | null>(null);

  // Get subscriptions from store
  const { subscriptions, loadSubscriptions } = useSubscriptionStore();

  // Polling status check interval
  const statusCheckRef = useRef<NodeJS.Timeout | null>(null);

  /**
   * Updates polling status from the service
   */
  const updateStatus = useCallback(() => {
    const active = isPricePollingActive();
    setIsActive(active);
  }, []);

  /**
   * Starts price polling service
   */
  const startPolling = useCallback(() => {
    try {
      startPricePolling();
      updateStatus();
      setError(null);
      console.log('🚀 Price polling started from hook');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to start polling';
      setError(errorMessage);
      console.error('❌ Failed to start price polling:', err);
    }
  }, [updateStatus]);

  /**
   * Stops price polling service
   */
  const stopPolling = useCallback(() => {
    try {
      stopPricePolling();
      updateStatus();
      setError(null);
      console.log('⏹️ Price polling stopped from hook');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to stop polling';
      setError(errorMessage);
      console.error('❌ Failed to stop price polling:', err);
    }
  }, [updateStatus]);

  /**
   * Manually triggers a price check
   */
  const manualCheck = useCallback(async () => {
    if (isChecking) return;

    setIsChecking(true);
    setError(null);

    try {
      console.log('🔍 Manual price check triggered');
      await checkPriceUpdates(subscriptions);
      setLastCheckTime(new Date());
      setCheckCount(prev => prev + 1);
      
      // Reload subscriptions to get updated prices
      loadSubscriptions();
      
      console.log('✅ Manual price check completed');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Price check failed';
      setError(errorMessage);
      console.error('❌ Manual price check failed:', err);
    } finally {
      setIsChecking(false);
    }
  }, [subscriptions, isChecking, loadSubscriptions]);

  /**
   * Toggles polling on/off
   */
  const togglePolling = useCallback(() => {
    if (isActive) {
      stopPolling();
    } else {
      startPolling();
    }
  }, [isActive, startPolling, stopPolling]);

  // Initialize polling based on subscriptions
  useEffect(() => {
    if (subscriptions.length > 0 && !isActive) {
      console.log(`📋 Found ${subscriptions.length} subscriptions, starting polling`);
      startPolling();
    } else if (subscriptions.length === 0 && isActive) {
      console.log('📭 No subscriptions, stopping polling');
      stopPolling();
    }
  }, [subscriptions.length, isActive, startPolling, stopPolling]);

  // Set up status monitoring
  useEffect(() => {
    // Initial status check
    updateStatus();

    // Periodic status checks to keep hook in sync with service
    statusCheckRef.current = setInterval(updateStatus, 5000); // Check every 5 seconds

    return () => {
      if (statusCheckRef.current) {
        clearInterval(statusCheckRef.current);
      }
    };
  }, [updateStatus]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      // Don't stop polling on unmount as it should continue in background
      // Only clean up the status monitoring
      if (statusCheckRef.current) {
        clearInterval(statusCheckRef.current);
      }
    };
  }, []);

  // Calculate time until next check
  const getTimeUntilNextCheck = useCallback((): number | null => {
    if (!lastCheckTime || !isActive) return null;
    
    const interval = getPollingInterval();
    const nextCheckTime = new Date(lastCheckTime.getTime() + interval);
    const timeUntilNext = nextCheckTime.getTime() - Date.now();
    
    return Math.max(0, timeUntilNext);
  }, [lastCheckTime, isActive]);

  // Format time remaining for display
  const getTimeRemainingText = useCallback((): string | null => {
    const timeRemaining = getTimeUntilNextCheck();
    
    if (timeRemaining === null) return null;
    
    const minutes = Math.ceil(timeRemaining / 1000 / 60);
    
    if (minutes <= 0) return 'Checking now...';
    if (minutes === 1) return '1 minute';
    return `${minutes} minutes`;
  }, [getTimeUntilNextCheck]);

  return {
    // Status
    isActive,
    isChecking,
    lastCheckTime,
    checkCount,
    error,
    
    // Computed values
    pollingInterval: getPollingInterval(),
    timeUntilNextCheck: getTimeUntilNextCheck(),
    timeRemainingText: getTimeRemainingText(),
    hasSubscriptions: subscriptions.length > 0,
    
    // Actions
    startPolling,
    stopPolling,
    togglePolling,
    manualCheck,
    clearError: () => setError(null),
  };
};
