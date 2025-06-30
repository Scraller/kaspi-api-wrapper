'use client';

import React, { useState } from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { TrendingDown, AlertCircle } from 'lucide-react';
import { formatPrice } from '@/utils/priceUtils';

interface PriceThresholdInputProps {
  currentPrice: number;
  currency?: string;
  defaultThreshold?: number;
  onThresholdSet: (threshold: number | undefined) => void;
  className?: string;
}

/**
 * Component for setting price threshold alerts
 * Allows users to set custom price alerts below current price
 */
export function PriceThresholdInput({
  currentPrice,
  currency = 'KZT',
  defaultThreshold,
  onThresholdSet,
  className,
}: PriceThresholdInputProps) {
  const [threshold, setThreshold] = useState<string>(
    defaultThreshold ? defaultThreshold.toString() : ''
  );
  const [error, setError] = useState<string>('');

  /**
   * Validate and apply threshold value
   */
  const handleThresholdChange = (value: string) => {
    setThreshold(value);
    setError('');

    // Clear threshold if empty
    if (!value.trim()) {
      onThresholdSet(undefined);
      return;
    }

    const numericValue = parseFloat(value);

    // Validation
    if (isNaN(numericValue)) {
      setError('Please enter a valid number');
      return;
    }

    if (numericValue < 0) {
      setError('Price cannot be negative');
      return;
    }

    if (numericValue >= currentPrice) {
      setError('Alert price must be lower than current price');
      return;
    }

    // Valid threshold
    onThresholdSet(numericValue);
  };

  /**
   * Set quick threshold percentages (5%, 10%, 20% below current price)
   */
  const setQuickThreshold = (percentageOff: number) => {
    const thresholdPrice = currentPrice * (1 - percentageOff / 100);
    const roundedThreshold = Math.round(thresholdPrice);
    setThreshold(roundedThreshold.toString());
    handleThresholdChange(roundedThreshold.toString());
  };

  const thresholdValue = parseFloat(threshold);
  const isValidThreshold = !isNaN(thresholdValue) && thresholdValue < currentPrice && thresholdValue >= 0;
  const savingsAmount = isValidThreshold ? currentPrice - thresholdValue : 0;
  const savingsPercentage = isValidThreshold ? ((savingsAmount / currentPrice) * 100).toFixed(1) : '0';

  return (
    <Card className={className}>
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center text-lg">
          <TrendingDown className="h-5 w-5 mr-2 text-green-600" />
          Price Alert Settings
        </CardTitle>
        <CardDescription>
          Get notified when the price drops below your target price
        </CardDescription>
      </CardHeader>
      
      <CardContent className="space-y-4">
        {/* Current Price Display */}
        <div className="bg-gray-50 p-3 rounded-lg">
          <Label className="text-sm text-gray-600">Current Price</Label>
          <div className="text-lg font-semibold">
            {formatPrice(currentPrice, currency)}
          </div>
        </div>

        {/* Threshold Input */}
        <div className="space-y-2">
          <Label htmlFor="threshold">Alert me when price drops to:</Label>
          <Input
            id="threshold"
            type="number"
            placeholder={`Enter price (${currency})`}
            value={threshold}
            onChange={(e) => handleThresholdChange(e.target.value)}
            className={error ? 'border-red-500' : ''}
            min="0"
            max={currentPrice - 1}
            step="1"
          />
          {error && (
            <div className="flex items-center text-sm text-red-600">
              <AlertCircle className="h-4 w-4 mr-1" />
              {error}
            </div>
          )}
        </div>

        {/* Quick Threshold Buttons */}
        <div className="space-y-2">
          <Label className="text-sm text-gray-600">Quick options:</Label>
          <div className="flex gap-2 flex-wrap">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setQuickThreshold(5)}
            >
              5% off ({formatPrice(currentPrice * 0.95, currency)})
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setQuickThreshold(10)}
            >
              10% off ({formatPrice(currentPrice * 0.9, currency)})
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setQuickThreshold(20)}
            >
              20% off ({formatPrice(currentPrice * 0.8, currency)})
            </Button>
          </div>
        </div>

        {/* Savings Preview */}
        {isValidThreshold && (
          <div className="bg-green-50 border border-green-200 p-3 rounded-lg">
            <div className="flex items-center text-green-800 text-sm">
              <TrendingDown className="h-4 w-4 mr-1" />
              You&apos;ll save {formatPrice(savingsAmount, currency)} ({savingsPercentage}%) when this price is reached
            </div>
          </div>
        )}

        {/* Clear Threshold */}
        {threshold && (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => {
              setThreshold('');
              setError('');
              onThresholdSet(undefined);
            }}
            className="text-gray-500 hover:text-gray-700"
          >
            Clear price alert
          </Button>
        )}
      </CardContent>
    </Card>
  );
}
