import { AnonymousSubscription } from '@/types/subscription';

/**
 * Browser notification service for price alerts
 * Handles permission requests, notification display, and user preferences
 */

// Notification permission states
export type NotificationPermission = 'default' | 'granted' | 'denied';

// Notification preferences stored in localStorage
interface NotificationPreferences {
  enabled: boolean;
  priceDropAlerts: boolean;
  thresholdAlerts: boolean;
  soundEnabled: boolean;
}

const NOTIFICATION_PREFS_KEY = 'kaspi_notification_preferences';

/**
 * Gets current notification permission status from browser
 */
export const getNotificationPermission = (): NotificationPermission => {
  if (typeof window !== 'undefined' && 'Notification' in window) {
    return Notification.permission as NotificationPermission;
  }
  return 'denied';
};

/**
 * Requests notification permission from user
 * Returns promise with the permission result
 */
export const requestNotificationPermission = async (): Promise<NotificationPermission> => {
  if (typeof window === 'undefined' || !('Notification' in window)) {
    console.warn('🔕 Browser does not support notifications');
    return 'denied';
  }
  
  if (Notification.permission === 'granted') {
    return 'granted';
  }
  
  try {
    const permission = await Notification.requestPermission();
    console.log(`🔔 Notification permission: ${permission}`);
    return permission as NotificationPermission;
  } catch (error) {
    console.error('❌ Failed to request notification permission:', error);
    return 'denied';
  }
};

/**
 * Gets user notification preferences from localStorage
 * Returns default preferences if none are saved
 */
export const getNotificationPreferences = (): NotificationPreferences => {
  try {
    if (typeof window === 'undefined') return getDefaultPreferences();
    
    const saved = localStorage.getItem(NOTIFICATION_PREFS_KEY);
    if (saved) {
      return { ...getDefaultPreferences(), ...JSON.parse(saved) };
    }
  } catch (error) {
    console.error('❌ Failed to load notification preferences:', error);
  }
  
  return getDefaultPreferences();
};

/**
 * Saves user notification preferences to localStorage
 */
export const saveNotificationPreferences = (preferences: NotificationPreferences): void => {
  try {
    if (typeof window === 'undefined') return;
    
    localStorage.setItem(NOTIFICATION_PREFS_KEY, JSON.stringify(preferences));
    console.log('💾 Notification preferences saved');
  } catch (error) {
    console.error('❌ Failed to save notification preferences:', error);
  }
};

/**
 * Gets default notification preferences
 */
const getDefaultPreferences = (): NotificationPreferences => ({
  enabled: true,
  priceDropAlerts: true,
  thresholdAlerts: true,
  soundEnabled: false, // Disabled by default to avoid annoyance
});

/**
 * Shows a price alert notification to the user
 * This is the main function called when a price change is detected
 */
export const showPriceAlert = async (
  subscription: AnonymousSubscription,
  newPrice: number,
  priceDropPercentage?: number
): Promise<void> => {
  const preferences = getNotificationPreferences();
  
  // Check if notifications are enabled by user
  if (!preferences.enabled) {
    console.log('🔕 Notifications disabled by user');
    return;
  }
  
  // Check if we have permission
  const permission = getNotificationPermission();
  if (permission !== 'granted') {
    console.log('🔕 No notification permission');
    return;
  }
  
  try {
    if (typeof window === 'undefined') return;
    
    const formatPrice = (price: number) => `${price.toLocaleString()} ₸`;
    const oldPrice = subscription.lastCheckedPrice || subscription.currentPrice;
    const priceDrop = oldPrice - newPrice;
    
    let title = '💰 Price Drop Alert!';
    let body = `${subscription.productName}\n`;
    
    if (priceDropPercentage) {
      body += `Price dropped ${priceDropPercentage.toFixed(1)}%\n`;
    }
    
    body += `${formatPrice(oldPrice)} → ${formatPrice(newPrice)}\n`;
    body += `You save: ${formatPrice(priceDrop)}`;
    
    // Create notification with click action
    const notification = new Notification(title, {
      body,
      icon: subscription.productImage || '/favicon.ico',
      tag: `price-alert-${subscription.productId}`, // Prevents duplicate notifications
      requireInteraction: true, // Keeps notification visible until user interacts
    });
    
    // Handle notification click - open product page
    notification.onclick = () => {
      window.focus(); // Focus the browser window
      if (subscription.kaspiUrl) {
        window.open(subscription.kaspiUrl, '_blank');
      } else {
        // Fallback to product detail page
        window.open(`/products/${subscription.productId}`, '_blank');
      }
      notification.close();
    };
    
    // Auto-close notification after 10 seconds
    setTimeout(() => {
      notification.close();
    }, 10000);
    
    console.log(`🔔 Price alert shown for ${subscription.productName}`);
    
  } catch (error) {
    console.error('❌ Failed to show price alert:', error);
  }
};

/**
 * Shows a simple notification for testing purposes
 */
export const showTestNotification = async (): Promise<boolean> => {
  const permission = await requestNotificationPermission();
  
  if (permission !== 'granted') {
    return false;
  }
  
  try {
    const notification = new Notification('🧪 Test Notification', {
      body: 'Kaspi Price Tracker notifications are working!',
      icon: '/favicon.ico',
      tag: 'test-notification'
    });
    
    // Auto-close after 5 seconds
    setTimeout(() => {
      notification.close();
    }, 5000);
    
    return true;
  } catch (error) {
    console.error('❌ Failed to show test notification:', error);
    return false;
  }
};

/**
 * Checks if browser supports notifications
 */
export const isNotificationSupported = (): boolean => {
  return typeof window !== 'undefined' && 'Notification' in window;
};

/**
 * Checks if notifications are currently available (supported + permission granted)
 */
export const areNotificationsAvailable = (): boolean => {
  return isNotificationSupported() && getNotificationPermission() === 'granted';
};

/**
 * Updates notification preferences for specific alert types
 */
export const updateNotificationPreference = (key: keyof NotificationPreferences, value: boolean): void => {
  const current = getNotificationPreferences();
  const updated = { ...current, [key]: value };
  saveNotificationPreferences(updated);
};

/**
 * Disables all notifications
 */
export const disableNotifications = (): void => {
  const current = getNotificationPreferences();
  saveNotificationPreferences({ ...current, enabled: false });
};

/**
 * Enables all notifications (requires permission)
 */
export const enableNotifications = async (): Promise<boolean> => {
  const permission = await requestNotificationPermission();
  
  if (permission === 'granted') {
    const current = getNotificationPreferences();
    saveNotificationPreferences({ ...current, enabled: true });
    return true;
  }
  
  return false;
};
