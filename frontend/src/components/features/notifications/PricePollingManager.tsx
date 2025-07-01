'use client';

import React, { useEffect } from 'react';
import { usePricePolling } from '@/hooks/usePricePolling';
import { useNotifications } from '@/hooks/useNotifications';

/**
 * Price Polling Manager Component
 * Handles automatic price polling lifecycle and integrates with notifications
 * This component should be included in the main layout to enable price monitoring
 */
interface PricePollingManagerProps {
  /**
   * Whether to automatically start polling when subscriptions exist
   * Default: true
   */
  autoStart?: boolean;
  
  /**
   * Whether to request notification permissions on mount
   * Default: false (let user control when to request)
   */
  requestNotificationPermission?: boolean;
}

export const PricePollingManager: React.FC<PricePollingManagerProps> = ({
  autoStart = true,
  requestNotificationPermission = false,
}) => {
  const {
    isActive,
    hasSubscriptions,
    startPolling,
    error: pollingError,
    clearError,
  } = usePricePolling();

  const {
    permission,
    isSupported: isNotificationSupported,
    requestPermission,
  } = useNotifications();

  // Auto-start polling when subscriptions exist
  useEffect(() => {
    if (autoStart && hasSubscriptions && !isActive) {
      console.log('🤖 Auto-starting price polling manager');
      startPolling();
    }
  }, [autoStart, hasSubscriptions, isActive, startPolling]);

  // Request notification permission if enabled
  useEffect(() => {
    if (requestNotificationPermission && isNotificationSupported && permission === 'default') {
      console.log('🔔 Auto-requesting notification permission');
      requestPermission();
    }
  }, [requestNotificationPermission, isNotificationSupported, permission, requestPermission]);

  // Handle polling errors
  useEffect(() => {
    if (pollingError) {
      console.error('🚨 Price polling error:', pollingError);
      
      // Auto-clear errors after 30 seconds to prevent permanent error state
      const timeout = setTimeout(() => {
        clearError();
      }, 30000);

      return () => clearTimeout(timeout);
    }
  }, [pollingError, clearError]);

  // Log status changes for debugging
  useEffect(() => {
    if (hasSubscriptions) {
      console.log(`📊 Price polling manager: ${isActive ? 'ACTIVE' : 'INACTIVE'} with ${hasSubscriptions ? 'subscriptions' : 'no subscriptions'}`);
    }
  }, [isActive, hasSubscriptions]);

  // This component doesn't render anything - it's just for background management
  return null;
};

export default PricePollingManager;
