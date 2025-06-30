'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { TrendingDown, TrendingUp, Eye, Clock } from 'lucide-react';
import { AnonymousSubscription } from '@/types/subscription';
import { formatPrice } from '@/utils/priceUtils';

interface StatsCardsProps {
  subscriptions: AnonymousSubscription[];
}

/**
 * StatsCards component displays summary statistics for user's watchlist
 * Shows total tracked products, price drops, increases, and potential savings
 */
export function StatsCards({ subscriptions }: StatsCardsProps) {
  // Calculate statistics from subscriptions
  const stats = React.useMemo(() => {
    const total = subscriptions.length;
    let priceDrops = 0;
    let priceIncreases = 0;
    let totalSavings = 0;
    let expiringCount = 0;
    
    const now = new Date();
    const oneHourFromNow = new Date(now.getTime() + 60 * 60 * 1000);
    
    subscriptions.forEach(sub => {
      // Count price changes
      if (sub.priceChange === 'decrease') {
        priceDrops++;
        // Calculate savings if there was a price drop
        if (sub.lastCheckedPrice && sub.lastCheckedPrice > sub.currentPrice) {
          totalSavings += sub.lastCheckedPrice - sub.currentPrice;
        }
      } else if (sub.priceChange === 'increase') {
        priceIncreases++;
      }
      
      // Count expiring subscriptions (within 1 hour)
      const expiresAt = typeof sub.expiresAt === 'string' ? new Date(sub.expiresAt) : sub.expiresAt;
      if (expiresAt <= oneHourFromNow) {
        expiringCount++;
      }
    });
    
    return {
      total,
      priceDrops,
      priceIncreases,
      totalSavings,
      expiringCount,
    };
  }, [subscriptions]);

  const statCards = [
    {
      title: 'Total Tracked',
      value: stats.total.toString(),
      description: `out of 10 products`,
      icon: Eye,
      color: 'text-blue-600',
      bgColor: 'bg-blue-50',
    },
    {
      title: 'Price Drops',
      value: stats.priceDrops.toString(),
      description: 'products decreased in price',
      icon: TrendingDown,
      color: 'text-green-600',
      bgColor: 'bg-green-50',
    },
    {
      title: 'Price Increases',
      value: stats.priceIncreases.toString(),
      description: 'products increased in price',
      icon: TrendingUp,
      color: 'text-red-600',
      bgColor: 'bg-red-50',
    },
    {
      title: 'Expiring Soon',
      value: stats.expiringCount.toString(),
      description: 'expiring within 1 hour',
      icon: Clock,
      color: 'text-amber-600',
      bgColor: 'bg-amber-50',
    },
  ];

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
      {statCards.map((stat, index) => {
        const IconComponent = stat.icon;
        return (
          <Card key={index} className="hover:shadow-md transition-shadow">
            <CardHeader className="pb-2">
              <div className="flex items-center justify-between">
                <CardTitle className="text-sm font-medium text-gray-600">
                  {stat.title}
                </CardTitle>
                <div className={`p-2 rounded-md ${stat.bgColor}`}>
                  <IconComponent className={`h-4 w-4 ${stat.color}`} />
                </div>
              </div>
            </CardHeader>
            <CardContent className="pt-0">
              <div className="text-2xl font-bold text-gray-900">
                {stat.value}
              </div>
              <p className="text-xs text-gray-500 mt-1">
                {stat.description}
              </p>
              {/* Show potential savings for price drops */}
              {stat.title === 'Price Drops' && stats.totalSavings > 0 && (
                <p className="text-xs text-green-600 font-medium mt-1">
                  Potential savings: {formatPrice(stats.totalSavings)}
                </p>
              )}
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
