import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Bell, Settings, TrendingDown, Target } from 'lucide-react';
import {
  getNotificationPreferences,
  saveNotificationPreferences,
  getNotificationPermission,
  areNotificationsAvailable,
} from '@/services/notifications';

/**
 * Component for managing notification preferences and settings
 * Allows users to control different types of price alerts
 */
interface NotificationSettingsProps {
  className?: string;
}

export const NotificationSettings: React.FC<NotificationSettingsProps> = ({ className }) => {
  const [preferences, setPreferences] = useState({
    enabled: true,
    priceDropAlerts: true,
    thresholdAlerts: true,
    soundEnabled: false,
  });

  const [permissionStatus, setPermissionStatus] = useState<'granted' | 'denied' | 'default'>('default');

  useEffect(() => {
    // Load current preferences
    const current = getNotificationPreferences();
    setPreferences(current);

    // Check permission status
    const permission = getNotificationPermission();
    setPermissionStatus(permission);
  }, []);

  /**
   * Handles preference changes and saves to localStorage
   */
  const handlePreferenceChange = (key: keyof typeof preferences, value: boolean): void => {
    const updated = { ...preferences, [key]: value };
    setPreferences(updated);
    saveNotificationPreferences(updated);
  };

  const isNotificationsActive = areNotificationsAvailable() && preferences.enabled;

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Settings size={20} />
          Notification Settings
        </CardTitle>
        <CardDescription>
          Customize when and how you receive price alerts.
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-6">
        {/* Permission Status */}
        <div className="flex items-center justify-between p-3 bg-gray-50 rounded-md">
          <div className="flex items-center gap-2">
            <Bell size={16} />
            <span className="text-sm font-medium">Browser Notifications</span>
          </div>
          <Badge variant={permissionStatus === 'granted' ? 'default' : 'secondary'}>
            {permissionStatus === 'granted' ? 'Enabled' : 
             permissionStatus === 'denied' ? 'Blocked' : 'Not Set'}
          </Badge>
        </div>

        {/* Main notification toggle */}
        <div className="flex items-center justify-between space-x-2">
          <div className="space-y-0.5">
            <Label htmlFor="notifications-enabled" className="text-base font-medium">
              Enable Notifications
            </Label>
            <div className="text-sm text-gray-600">
              Receive all price alerts and updates
            </div>
          </div>
          <Switch
            id="notifications-enabled"
            checked={preferences.enabled}
            onCheckedChange={(checked) => handlePreferenceChange('enabled', checked)}
            disabled={permissionStatus !== 'granted'}
          />
        </div>

        {/* Price drop alerts */}
        <div className="flex items-center justify-between space-x-2 opacity-75 hover:opacity-100 transition-opacity">
          <div className="space-y-0.5">
            <Label htmlFor="price-drop-alerts" className="text-base font-medium flex items-center gap-2">
              <TrendingDown size={16} className="text-green-600" />
              Price Drop Alerts
            </Label>
            <div className="text-sm text-gray-600">
              Notify when prices decrease by 5% or more
            </div>
          </div>
          <Switch
            id="price-drop-alerts"
            checked={preferences.priceDropAlerts}
            onCheckedChange={(checked) => handlePreferenceChange('priceDropAlerts', checked)}
            disabled={!preferences.enabled || permissionStatus !== 'granted'}
          />
        </div>

        {/* Threshold alerts */}
        <div className="flex items-center justify-between space-x-2 opacity-75 hover:opacity-100 transition-opacity">
          <div className="space-y-0.5">
            <Label htmlFor="threshold-alerts" className="text-base font-medium flex items-center gap-2">
              <Target size={16} className="text-blue-600" />
              Threshold Alerts
            </Label>
            <div className="text-sm text-gray-600">
              Notify when prices reach your target price
            </div>
          </div>
          <Switch
            id="threshold-alerts"
            checked={preferences.thresholdAlerts}
            onCheckedChange={(checked) => handlePreferenceChange('thresholdAlerts', checked)}
            disabled={!preferences.enabled || permissionStatus !== 'granted'}
          />
        </div>

        {/* Sound notifications (disabled for MVP to avoid annoyance) */}
        <div className="flex items-center justify-between space-x-2 opacity-50">
          <div className="space-y-0.5">
            <Label htmlFor="sound-enabled" className="text-base font-medium">
              Sound Alerts
            </Label>
            <div className="text-sm text-gray-600">
              Play sound with notifications (coming soon)
            </div>
          </div>
          <Switch
            id="sound-enabled"
            checked={false}
            disabled={true}
          />
        </div>

        {/* Status summary */}
        {isNotificationsActive && (
          <div className="mt-4 p-3 bg-green-50 border border-green-200 rounded-md">
            <div className="flex items-center gap-2 text-green-700">
              <Bell size={16} />
              <span className="text-sm font-medium">Notifications Active</span>
            </div>
            <p className="text-xs text-green-600 mt-1">
              You&apos;ll receive alerts for{' '}
              {preferences.priceDropAlerts && preferences.thresholdAlerts
                ? 'price drops and threshold targets'
                : preferences.priceDropAlerts
                ? 'price drops only'
                : preferences.thresholdAlerts
                ? 'threshold targets only'
                : 'no events (all disabled)'}
            </p>
          </div>
        )}

        {/* Warning for disabled notifications */}
        {(!isNotificationsActive && permissionStatus === 'granted') && (
          <div className="mt-4 p-3 bg-yellow-50 border border-yellow-200 rounded-md">
            <div className="flex items-center gap-2 text-yellow-700">
              <Bell size={16} />
              <span className="text-sm font-medium">Notifications Disabled</span>
            </div>
            <p className="text-xs text-yellow-600 mt-1">
              Enable notifications to receive price alerts automatically.
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  );
};
