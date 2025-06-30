'use client';

import React, { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { RefreshCw, Plus, AlertCircle, Clock, Bell } from 'lucide-react';
import { StatsCards } from '@/components/features/dashboard/StatsCards';
import { WatchlistGrid } from '@/components/features/dashboard/WatchlistGrid';
import { EmptyState } from '@/components/features/dashboard/EmptyState';
import { useWatchlist } from '@/hooks/useWatchlist';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';

/**
 * Dashboard page for managing price tracking subscriptions
 * Shows statistics, watchlist grid, and management tools
 */
export default function DashboardPage() {
  const {
    subscriptions,
    dashboardStats,
    isLoading,
    error,
    lastUpdate,
    hasExpiringSoon,
    hasPriceAlerts,
    isEmpty,
    handleRemoveSubscription,
    refreshData,
    clearError,
    storeStats,
  } = useWatchlist();

  const router = useRouter();
  const [mounted, setMounted] = useState(false);

  // Ensure component is mounted before showing time to avoid hydration errors
  useEffect(() => {
    setMounted(true);
  }, []);

  // Handle refresh action
  const handleRefresh = () => {
    refreshData();
    toast.success('Watchlist refreshed');
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="max-w-6xl mx-auto">
        {/* Page Header */}
        <div className="mb-8">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 mb-2">
                Price Tracking Dashboard
              </h1>
              <p className="text-gray-600">
                Monitor your subscribed products and get notified when prices drop
              </p>
            </div>
            
            <div className="flex items-center gap-3">
              {/* Last updated */}
              <div className="text-sm text-gray-500">
                Updated: {mounted && lastUpdate ? lastUpdate.toLocaleTimeString('en-US', { 
                  hour12: false, 
                  hour: '2-digit', 
                  minute: '2-digit',
                  second: '2-digit'
                }) : '--:--:--'}
              </div>
              
              {/* Refresh button */}
              <Button 
                variant="outline" 
                size="sm" 
                onClick={handleRefresh}
                disabled={isLoading}
              >
                <RefreshCw className={`h-4 w-4 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
                Refresh
              </Button>
            </div>
          </div>

          {/* Usage indicator */}
          <div className="mt-4 flex items-center gap-4">
            <div className="text-sm text-gray-600">
              <span className="font-medium">{storeStats.count}</span> of{' '}
              <span className="font-medium">{storeStats.limit}</span> products tracked
            </div>
            <div className="flex-1 bg-gray-200 rounded-full h-2 max-w-xs">
              <div 
                className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                style={{ width: `${(storeStats.count / storeStats.limit) * 100}%` }}
              />
            </div>
          </div>
        </div>

        {/* Alerts Section */}
        {(hasExpiringSoon || hasPriceAlerts || error) && (
          <div className="mb-6 space-y-3">
            {/* Error Alert */}
            {error && (
              <Card className="border-red-200 bg-red-50">
                <CardContent className="p-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-2 text-red-700">
                      <AlertCircle className="h-5 w-5" />
                      <span className="font-medium">Error loading watchlist</span>
                      <span className="text-sm">- {error}</span>
                    </div>
                    <Button variant="outline" size="sm" onClick={clearError}>
                      Dismiss
                    </Button>
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Price Alerts */}
            {hasPriceAlerts && (
              <Card className="border-green-200 bg-green-50">
                <CardContent className="p-4">
                  <div className="flex items-center space-x-2 text-green-700">
                    <Bell className="h-5 w-5" />
                    <span className="font-medium">Price alerts!</span>
                    <Badge className="bg-green-100 text-green-700">
                      {dashboardStats.thresholdReached} product{dashboardStats.thresholdReached !== 1 ? 's' : ''} reached target price
                    </Badge>
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Expiring Soon Alert */}
            {hasExpiringSoon && (
              <Card className="border-amber-200 bg-amber-50">
                <CardContent className="p-4">
                  <div className="flex items-center space-x-2 text-amber-700">
                    <Clock className="h-5 w-5" />
                    <span className="font-medium">Subscriptions expiring soon</span>
                    <Badge className="bg-amber-100 text-amber-700">
                      {dashboardStats.expiringSoon} expiring within 1 hour
                    </Badge>
                  </div>
                </CardContent>
              </Card>
            )}
          </div>
        )}

        {/* Statistics Cards */}
        {!isEmpty && (
          <StatsCards subscriptions={subscriptions} />
        )}

        {/* Main Content */}
        {isEmpty ? (
          <EmptyState 
            type={subscriptions.length === 0 ? 'no_subscriptions' : 'expired'} 
          />
        ) : (
          <WatchlistGrid
            subscriptions={subscriptions}
            onRemoveSubscription={handleRemoveSubscription}
            isLoading={isLoading}
          />
        )}

        {/* Footer Info */}
        {!isEmpty && (
          <div className="mt-8 p-4 bg-gray-50 rounded-lg">
            <div className="text-sm text-gray-600 text-center">
              <p className="mb-1">
                💡 <strong>Anonymous tracking:</strong> Your subscriptions are stored locally and expire after 24 hours.
              </p>
              <p>
                Prices are checked every 15 minutes when you visit the site. 
                Browser notifications will alert you of price changes.
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
