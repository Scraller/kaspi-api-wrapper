import React, { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Bell, BellOff, Check, X } from 'lucide-react';
import {
  getNotificationPermission,
  requestNotificationPermission,
  isNotificationSupported,
  showTestNotification,
  type NotificationPermission,
} from '@/services/notifications';

/**
 * Component for managing browser notification permissions
 * Handles permission requests and provides user feedback
 */
interface NotificationPermissionProps {
  onPermissionChange?: (permission: NotificationPermission) => void;
  showTestButton?: boolean;
}

export const NotificationPermissionComponent: React.FC<NotificationPermissionProps> = ({
  onPermissionChange,
  showTestButton = true,
}) => {
  const [permission, setPermission] = useState<NotificationPermission>('default');
  const [isRequesting, setIsRequesting] = useState(false);
  const [testResult, setTestResult] = useState<boolean | null>(null);

  useEffect(() => {
    // Initialize permission state
    const currentPermission = getNotificationPermission();
    setPermission(currentPermission);
  }, []);

  /**
   * Handles permission request from user
   * Shows loading state and updates permission status
   */
  const handleRequestPermission = async (): Promise<void> => {
    setIsRequesting(true);
    setTestResult(null);

    try {
      const newPermission = await requestNotificationPermission();
      setPermission(newPermission);
      onPermissionChange?.(newPermission);
    } catch (error) {
      console.error('Failed to request notification permission:', error);
    } finally {
      setIsRequesting(false);
    }
  };

  /**
   * Handles test notification to verify everything works
   */
  const handleTestNotification = async (): Promise<void> => {
    try {
      const success = await showTestNotification();
      setTestResult(success);
    } catch (error) {
      console.error('Failed to show test notification:', error);
      setTestResult(false);
    }
  };

  // Don't render if notifications are not supported
  if (!isNotificationSupported()) {
    return (
      <Card className="border-orange-200 bg-orange-50">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-orange-700">
            <BellOff size={20} />
            Notifications Not Supported
          </CardTitle>
          <CardDescription className="text-orange-600">
            Your browser doesn&apos;t support notifications. You won&apos;t receive price alerts automatically.
          </CardDescription>
        </CardHeader>
      </Card>
    );
  }

  return (
    <Card className={`${getCardStyles(permission)}`}>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          {getIcon(permission)}
          {getTitle(permission)}
        </CardTitle>
        <CardDescription>
          {getDescription(permission)}
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Permission Actions */}
        {permission === 'default' && (
          <Button
            onClick={handleRequestPermission}
            disabled={isRequesting}
            className="w-full"
          >
            {isRequesting ? 'Requesting Permission...' : 'Enable Notifications'}
          </Button>
        )}

        {permission === 'denied' && (
          <div className="text-sm text-gray-600">
            <p className="mb-2">To enable notifications:</p>
            <ol className="list-decimal list-inside space-y-1 text-xs">
              <li>Click the lock icon in your browser&apos;s address bar</li>
              <li>Change notifications from &quot;Block&quot; to &quot;Allow&quot;</li>
              <li>Refresh the page</li>
            </ol>
          </div>
        )}

        {/* Test Notification */}
        {permission === 'granted' && showTestButton && (
          <div className="space-y-2">
            <Button
              variant="outline"
              onClick={handleTestNotification}
              className="w-full"
              size="sm"
            >
              Send Test Notification
            </Button>

            {testResult !== null && (
              <div className={`flex items-center gap-2 text-sm ${
                testResult ? 'text-green-600' : 'text-red-600'
              }`}>
                {testResult ? <Check size={16} /> : <X size={16} />}
                {testResult 
                  ? 'Test notification sent successfully!' 
                  : 'Failed to send test notification.'
                }
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
};

/**
 * Gets appropriate icon for permission state
 */
const getIcon = (permission: NotificationPermission): React.ReactNode => {
  switch (permission) {
    case 'granted':
      return <Bell size={20} className="text-green-600" />;
    case 'denied':
      return <BellOff size={20} className="text-red-600" />;
    default:
      return <Bell size={20} className="text-gray-600" />;
  }
};

/**
 * Gets appropriate title for permission state
 */
const getTitle = (permission: NotificationPermission): string => {
  switch (permission) {
    case 'granted':
      return 'Notifications Enabled';
    case 'denied':
      return 'Notifications Blocked';
    default:
      return 'Enable Price Alerts';
  }
};

/**
 * Gets appropriate description for permission state
 */
const getDescription = (permission: NotificationPermission): string => {
  switch (permission) {
    case 'granted':
      return 'You&apos;ll receive notifications when product prices drop or reach your threshold.';
    case 'denied':
      return 'Notifications are blocked. You won&apos;t receive price alerts automatically.';
    default:
      return 'Get notified when product prices drop below your threshold or when significant discounts are available.';
  }
};

/**
 * Gets appropriate card styles for permission state
 */
const getCardStyles = (permission: NotificationPermission): string => {
  switch (permission) {
    case 'granted':
      return 'border-green-200 bg-green-50';
    case 'denied':
      return 'border-red-200 bg-red-50';
    default:
      return 'border-blue-200 bg-blue-50';
  }
};
