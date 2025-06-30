'use client';

import React from 'react';
import { Badge } from '@/components/ui/badge';
import { ShoppingCart, Clock, AlertTriangle, Target } from 'lucide-react';
import { AnonymousSubscription } from '@/types/subscription';
import { getPriceRecommendation } from '@/utils/priceAnalysis';

interface PriceRecommendationProps {
  subscription: AnonymousSubscription;
  size?: 'sm' | 'md';
}

/**
 * PriceRecommendation component displays simple recommendations based on price analysis
 * Uses rule-based logic to help users make informed purchasing decisions
 */
export function PriceRecommendation({ subscription, size = 'md' }: PriceRecommendationProps) {
  const recommendation = getPriceRecommendation(subscription);

  const getRecommendationStyle = () => {
    switch (recommendation.type) {
      case 'buy_now':
        return {
          variant: 'default' as const,
          className: 'bg-green-100 text-green-800 border-green-200',
          icon: ShoppingCart,
        };
      case 'good_deal':
        return {
          variant: 'secondary' as const,
          className: 'bg-blue-100 text-blue-800 border-blue-200',
          icon: Target,
        };
      case 'wait':
        return {
          variant: 'outline' as const,
          className: 'bg-amber-50 text-amber-700 border-amber-200',
          icon: Clock,
        };
      case 'set_alert':
        return {
          variant: 'outline' as const,
          className: 'bg-gray-50 text-gray-700 border-gray-200',
          icon: AlertTriangle,
        };
      default:
        return {
          variant: 'secondary' as const,
          className: 'bg-gray-100 text-gray-600',
          icon: Clock,
        };
    }
  };

  const style = getRecommendationStyle();
  const IconComponent = style.icon;
  const iconSize = size === 'sm' ? 'h-3 w-3' : 'h-4 w-4';
  const textSize = size === 'sm' ? 'text-xs' : 'text-sm';

  return (
    <Badge 
      variant={style.variant}
      className={`${style.className} ${textSize} flex items-center gap-1 max-w-full`}
    >
      <IconComponent className={iconSize} />
      <span className="truncate">{recommendation.message}</span>
    </Badge>
  );
}
