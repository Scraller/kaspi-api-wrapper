import { useState, useEffect, useCallback } from 'react';
import {
  getNotificationPermission,
  requestNotificationPermission,
  getNotificationPreferences,
  saveNotificationPreferences,
  areNotificationsAvailable,
  showTestNotification,
  type NotificationPermission,
} from '@/services/notifications';

/**
 * Custom hook for managing notification state and preferences
 * Provides reactive interface for notification functionality
 */
export const useNotifications = () => {
  const [permission, setPermission] = useState<NotificationPermission>('default');
  const [preferences, setPreferences] = useState({
    enabled: true,
    priceDropAlerts: true,
    thresholdAlerts: true,
    soundEnabled: false,
  });
  const [isLoading, setIsLoading] = useState(false);

  // Initialize state on mount
  useEffect(() => {
    const currentPermission = getNotificationPermission();
    const currentPreferences = getNotificationPreferences();
    
    setPermission(currentPermission);
    setPreferences(currentPreferences);
  }, []);

  /**
   * Requests notification permission from browser
   */
  const requestPermission = useCallback(async (): Promise<NotificationPermission> => {
    setIsLoading(true);
    
    try {
      const newPermission = await requestNotificationPermission();
      setPermission(newPermission);
      return newPermission;
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Updates a specific notification preference
   */
  const updatePreference = useCallback((key: keyof typeof preferences, value: boolean): void => {
    const updated = { ...preferences, [key]: value };
    setPreferences(updated);
    saveNotificationPreferences(updated);
  }, [preferences]);

  /**
   * Enables all notifications (requests permission if needed)
   */
  const enableNotifications = useCallback(async (): Promise<boolean> => {
    const currentPermission = permission === 'granted' ? permission : await requestPermission();
    
    if (currentPermission === 'granted') {
      updatePreference('enabled', true);
      return true;
    }
    
    return false;
  }, [permission, requestPermission, updatePreference]);

  /**
   * Disables all notifications
   */
  const disableNotifications = useCallback((): void => {
    updatePreference('enabled', false);
  }, [updatePreference]);

  /**
   * Shows a test notification
   */
  const sendTestNotification = useCallback(async (): Promise<boolean> => {
    return await showTestNotification();
  }, []);

  // Computed values
  const isSupported = 'Notification' in window;
  const isAvailable = areNotificationsAvailable() && preferences.enabled;
  const canRequest = permission === 'default';
  const isDenied = permission === 'denied';

  return {
    // State
    permission,
    preferences,
    isLoading,
    
    // Computed
    isSupported,
    isAvailable,
    canRequest,
    isDenied,
    
    // Actions
    requestPermission,
    updatePreference,
    enableNotifications,
    disableNotifications,
    sendTestNotification,
  };
};
