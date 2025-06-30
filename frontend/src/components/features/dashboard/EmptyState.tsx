'use client';

import React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Search, ShoppingBag, Clock } from 'lucide-react';
import { useRouter } from 'next/navigation';

interface EmptyStateProps {
  type?: 'no_subscriptions' | 'no_results' | 'expired';
  className?: string;
}

/**
 * EmptyState component displays appropriate messaging when there are no subscriptions
 * Provides different states based on the context (no subscriptions, expired, filtered results)
 */
export function EmptyState({ type = 'no_subscriptions', className }: EmptyStateProps) {
  const router = useRouter();

  const stateConfig = {
    no_subscriptions: {
      icon: ShoppingBag,
      title: 'No products in your watchlist',
      description: 'Start tracking prices by searching for products and clicking "Watch Price"',
      action: {
        text: 'Search Products',
        onClick: () => router.push('/search'),
      },
    },
    no_results: {
      icon: Search,
      title: 'No products match your filter',
      description: 'Try adjusting your sort and filter options to see more products',
      action: null,
    },
    expired: {
      icon: Clock,
      title: 'All subscriptions have expired',
      description: 'Anonymous subscriptions last 24 hours. Search for new products to start tracking again.',
      action: {
        text: 'Search New Products',
        onClick: () => router.push('/search'),
      },
    },
  };

  const config = stateConfig[type];
  const IconComponent = config.icon;

  return (
    <Card className={`text-center py-12 px-6 ${className}`}>
      <CardContent className="space-y-6">
        {/* Icon */}
        <div className="flex justify-center">
          <div className="p-4 bg-gray-100 rounded-full">
            <IconComponent className="h-12 w-12 text-gray-400" />
          </div>
        </div>

        {/* Title */}
        <div>
          <h3 className="text-lg font-semibold text-gray-900 mb-2">
            {config.title}
          </h3>
          <p className="text-gray-600 max-w-md mx-auto">
            {config.description}
          </p>
        </div>

        {/* Action Button */}
        {config.action && (
          <div>
            <Button onClick={config.action.onClick} className="mt-4">
              {config.action.text}
            </Button>
          </div>
        )}

        {/* Additional helpful info for first-time users */}
        {type === 'no_subscriptions' && (
          <div className="mt-8 p-4 bg-blue-50 rounded-lg">
            <h4 className="font-medium text-blue-900 mb-2">How to start tracking prices:</h4>
            <ol className="text-sm text-blue-800 space-y-1 text-left max-w-sm mx-auto">
              <li>1. Search for any product on Kaspi.kz</li>
              <li>2. Click the &ldquo;Watch Price&rdquo; button on products you want to track</li>
              <li>3. Set your target price (optional)</li>
              <li>4. Get notified when prices drop!</li>
            </ol>
          </div>
        )}

        {/* Anonymous user limitations reminder */}
        {(type === 'no_subscriptions' || type === 'expired') && (
          <div className="mt-6 text-xs text-gray-500 border-t pt-4">
            <p>
              💡 <strong>Anonymous users</strong> can track up to 10 products for 24 hours each.
              <br />
              No registration required!
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
