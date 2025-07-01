'use client';

import React, { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Slider } from '@/components/ui/slider';
import { Clock, Settings, CheckCircle, AlertCircle } from 'lucide-react';
import { 
  setPollingInterval, 
  getSavedPollingInterval,
  getMinPollingIntervalMinutes,
  getMaxPollingIntervalMinutes,
  getCurrentPollingIntervalMinutes,
  getTimeElapsedSinceLastCheck,
  getTimeRemainingUntilNextCheck,
  isPricePollingActive
} from '@/services/pricePolling';

interface PollingIntervalSettingsProps {
  className?: string;
}

/**
 * Component for managing price polling interval settings
 * Allows users to configure how frequently prices are checked (2-15 minutes)
 */
export function PollingIntervalSettings({ className = "" }: PollingIntervalSettingsProps) {
  const [selectedInterval, setSelectedInterval] = useState<number>(15);
  const [pendingInterval, setPendingInterval] = useState<number>(15);
  const [isUpdating, setIsUpdating] = useState(false);
  const [updateSuccess, setUpdateSuccess] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);
  const [timeElapsed, setTimeElapsed] = useState<{ minutes: number; seconds: number } | null>(null);
  const [timeRemaining, setTimeRemaining] = useState<{ minutes: number; seconds: number } | null>(null);
  const [isPollingActive, setIsPollingActive] = useState(false);

  // Load current settings on mount
  useEffect(() => {
    const currentInterval = getSavedPollingInterval();
    setSelectedInterval(currentInterval);
    setPendingInterval(currentInterval);
  }, []);

  // Update time displays every second
  useEffect(() => {
    const updateTimes = () => {
      setTimeElapsed(getTimeElapsedSinceLastCheck());
      setTimeRemaining(getTimeRemainingUntilNextCheck());
      setIsPollingActive(isPricePollingActive());
    };

    updateTimes(); // Initial update
    const interval = setInterval(updateTimes, 1000);

    return () => clearInterval(interval);
  }, []);

  const handleIntervalChange = async () => {
    setIsUpdating(true);
    setUpdateSuccess(false);

    try {
      const success = setPollingInterval(pendingInterval);
      if (success) {
        setSelectedInterval(pendingInterval);
        setHasChanges(false);
        setUpdateSuccess(true);
        setTimeout(() => setUpdateSuccess(false), 3000);
      }
    } catch (error) {
      console.error('Failed to update polling interval:', error);
    } finally {
      setIsUpdating(false);
    }
  };

  const handleSliderChange = (value: number[]) => {
    const newValue = value[0];
    setPendingInterval(newValue);
    setHasChanges(newValue !== selectedInterval);
  };

  const handleReset = () => {
    setPendingInterval(selectedInterval);
    setHasChanges(false);
  };

  const formatTime = (time: { minutes: number; seconds: number } | null): string => {
    if (!time) return '--:--';
    return `${time.minutes}:${time.seconds.toString().padStart(2, '0')}`;
  };

  // Generate interval options for slider
  const minInterval = getMinPollingIntervalMinutes();
  const maxInterval = getMaxPollingIntervalMinutes();

  return (
    <div className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 ${className}`}>
      {/* Header */}
      <div className="flex items-center gap-2 mb-4">
        <Settings className="h-5 w-5 text-gray-600 dark:text-gray-400" />
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
          Price Check Settings
        </h3>
        {updateSuccess && (
          <CheckCircle className="h-4 w-4 text-green-500 ml-auto" />
        )}
      </div>

      {/* Polling Status */}
      <div className="bg-gray-50 dark:bg-gray-900 rounded-lg p-3 mb-4">
        <div className="flex items-center justify-between mb-2">
          <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Polling Status
          </span>
          <div className={`flex items-center gap-1 text-xs px-2 py-1 rounded-full ${
            isPollingActive 
              ? 'bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-300' 
              : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400'
          }`}>
            <div className={`w-2 h-2 rounded-full ${
              isPollingActive ? 'bg-green-500 animate-pulse' : 'bg-gray-400'
            }`} />
            {isPollingActive ? 'Active' : 'Inactive'}
          </div>
        </div>
        
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <span className="text-gray-500 dark:text-gray-500">Time since last check:</span>
            <div className="font-mono text-lg text-gray-900 dark:text-gray-100">
              {formatTime(timeElapsed)}
            </div>
          </div>
          <div>
            <span className="text-gray-500 dark:text-gray-500">Next check in:</span>
            <div className="font-mono text-lg text-gray-900 dark:text-gray-100">
              {formatTime(timeRemaining)}
            </div>
          </div>
        </div>
      </div>

      {/* Interval Selection */}
      <div className="space-y-4">
        <Label htmlFor="polling-interval" className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Check prices every:
        </Label>
        
        {/* Slider and Value Display */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-sm text-gray-600 dark:text-gray-400">
              {minInterval} min
            </span>
            <div className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              {pendingInterval} minute{pendingInterval > 1 ? 's' : ''}
            </div>
            <span className="text-sm text-gray-600 dark:text-gray-400">
              {maxInterval} min
            </span>
          </div>
          
          <Slider
            value={[pendingInterval]}
            onValueChange={handleSliderChange}
            min={minInterval}
            max={maxInterval}
            step={1}
            className="w-full"
            disabled={isUpdating}
          />
          
          {/* Interval Markers */}
          <div className="flex justify-between text-xs text-gray-500 dark:text-gray-500 px-1">
            <span>Fast</span>
            <span>Balanced</span>
            <span>Conservative</span>
          </div>
        </div>

        {/* Action Buttons */}
        {hasChanges && (
          <div className="flex items-center gap-3 pt-2">
            <Button
              onClick={handleIntervalChange}
              disabled={isUpdating}
              className="flex items-center gap-2"
            >
              {isUpdating && (
                <div className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent" />
              )}
              Apply Changes
            </Button>
            <Button
              variant="outline"
              onClick={handleReset}
              disabled={isUpdating}
            >
              Reset
            </Button>
          </div>
        )}
        
        {/* Success Message */}
        {updateSuccess && !hasChanges && (
          <div className="flex items-center gap-2 text-sm text-green-600 dark:text-green-400 pt-2">
            <CheckCircle className="h-4 w-4" />
            <span>Polling interval updated successfully!</span>
          </div>
        )}

        {/* Current Interval Display */}
        <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
          <Clock className="h-4 w-4" />
          <span>
            Currently checking every {getCurrentPollingIntervalMinutes()} minute{getCurrentPollingIntervalMinutes() > 1 ? 's' : ''}
          </span>
        </div>

        {/* Interval Guidelines */}
        <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3 mt-4">
          <div className="flex items-start gap-2">
            <AlertCircle className="h-4 w-4 text-blue-600 dark:text-blue-400 mt-0.5 flex-shrink-0" />
            <div className="text-sm text-blue-800 dark:text-blue-200">
              <p className="font-medium mb-1">Interval Guidelines:</p>
              <ul className="text-xs space-y-1 ml-2">
                <li>• <strong>2-5 minutes:</strong> High frequency, best for active trading</li>
                <li>• <strong>10-15 minutes:</strong> Balanced, recommended for most users</li>
                <li>• <strong>Shorter intervals</strong> may consume more API resources</li>
                <li>• <strong>Changes take effect</strong> on the next polling cycle</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
