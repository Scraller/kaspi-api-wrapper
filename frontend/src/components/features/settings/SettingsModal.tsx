'use client';

import React, { useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Settings, X } from 'lucide-react';
import { NotificationPermissionComponent } from '../notifications/NotificationPermission';
import { NotificationSettings } from '../notifications/NotificationSettings';
import { PollingIntervalSettings } from '../polling/PollingIntervalSettings';

interface SettingsModalProps {
  className?: string;
}

/**
 * Settings modal component that contains all dashboard settings
 * Consolidates notification and polling settings in a compact popup
 */
export function SettingsModal({ className = "" }: SettingsModalProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <Button 
          variant="outline" 
          size="sm" 
          className={`flex items-center gap-2 ${className}`}
        >
          <Settings className="h-4 w-4" />
          Settings
        </Button>
      </DialogTrigger>
      
      <DialogContent className="max-w-2xl max-h-[85vh] overflow-hidden">
        <DialogHeader>
          <div className="flex items-center justify-between">
            <div>
              <DialogTitle className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                Dashboard Settings
              </DialogTitle>
              <DialogDescription className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                Configure notifications and price monitoring settings
              </DialogDescription>
            </div>
            <Button 
              variant="ghost" 
              size="sm" 
              onClick={() => setIsOpen(false)}
              className="h-6 w-6 p-0 hover:bg-gray-100 dark:hover:bg-gray-800"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        </DialogHeader>

        <div className="mt-6 space-y-6 overflow-y-auto max-h-[calc(85vh-120px)]">
          {/* Notification Settings Section */}
          <div className="space-y-4">
            <div className="border-b border-gray-200 dark:border-gray-700 pb-3">
              <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">
                🔔 Notification Settings
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                Manage how you receive price alerts and notifications
              </p>
            </div>
            
            <div className="space-y-4">
              <NotificationPermissionComponent />
              <NotificationSettings />
            </div>
          </div>

          {/* Price Monitoring Section */}
          <div className="space-y-4">
            <div className="border-b border-gray-200 dark:border-gray-700 pb-3">
              <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">
                ⏱️ Price Monitoring
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                Configure how often prices are checked and monitored
              </p>
            </div>
            
            <PollingIntervalSettings />
          </div>

          {/* Help Section */}
          <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
            <div className="flex items-start gap-3">
              <div className="text-blue-600 dark:text-blue-400 text-xl">💡</div>
              <div className="text-sm text-blue-800 dark:text-blue-200">
                <p className="font-medium mb-2">Tips for optimal monitoring:</p>
                <ul className="text-xs space-y-1 ml-2">
                  <li>• Enable browser notifications for instant price alerts</li>
                  <li>• Set shorter intervals (2-5 min) for time-sensitive purchases</li>
                  <li>• Use longer intervals (10-15 min) for casual price tracking</li>
                  <li>• Check price history charts by clicking on prices</li>
                </ul>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="flex justify-end pt-4 border-t border-gray-200 dark:border-gray-700 mt-6">
          <Button onClick={() => setIsOpen(false)}>
            Done
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
