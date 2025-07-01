'use client';

import React from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Area,
  AreaChart
} from 'recharts';
import { PriceHistoryEntry } from '@/types/subscription';
import { format } from 'date-fns';
import { MerchantInfoInline } from '../merchant/MerchantInfo';

interface PriceHistoryChartProps {
  priceHistory: PriceHistoryEntry[];
  productName: string;
  currentPrice: number;
  className?: string;
}

/**
 * PriceHistoryChart component for displaying price changes over time
 * Uses Recharts for interactive data visualization with price trends
 */
export function PriceHistoryChart({ 
  priceHistory, 
  productName, 
  currentPrice,
  className = ""
}: PriceHistoryChartProps) {
  // Transform price history data for Recharts
  const chartData = priceHistory.map((entry, index) => ({
    timestamp: typeof entry.timestamp === 'string' ? new Date(entry.timestamp) : entry.timestamp,
    price: entry.price,
    merchantName: entry.merchantName,
    merchantId: entry.merchantId,
    merchantPhone: entry.merchantPhone,
    merchantUrl: entry.merchantUrl,
    index,
    // Format for display
    formattedTime: format(
      typeof entry.timestamp === 'string' ? new Date(entry.timestamp) : entry.timestamp,
      'MMM dd, HH:mm'
    ),
    formattedPrice: new Intl.NumberFormat('kk-KZ', {
      style: 'currency',
      currency: 'KZT',
      minimumFractionDigits: 0,
    }).format(entry.price)
  }));

  // Calculate price trend for color
  const firstPrice = chartData[0]?.price || currentPrice;
  const lastPrice = chartData[chartData.length - 1]?.price || currentPrice;
  const priceDirection = lastPrice < firstPrice ? 'decrease' : lastPrice > firstPrice ? 'increase' : 'stable';
  
  // Calculate dynamic Y-axis range for better visualization
  const calculateYAxisDomain = (): [number, number] => {
    if (chartData.length === 0) return [0, currentPrice * 1.1];
    
    const prices = chartData.map(d => d.price);
    const minPrice = Math.min(...prices);
    const maxPrice = Math.max(...prices);
    const priceRange = maxPrice - minPrice;
    
    // If the price range is very small compared to the absolute values,
    // create a focused view around the actual price variation
    if (priceRange < minPrice * 0.1) { // If variation is less than 10% of the price
      const padding = Math.max(priceRange * 0.2, minPrice * 0.02); // 20% of range or 2% of price, whichever is larger
      return [
        Math.max(0, minPrice - padding), // Don't go below 0
        maxPrice + padding
      ];
    }
    
    // For larger variations, use a smaller padding
    const padding = priceRange * 0.1; // 10% padding
    return [
      Math.max(0, minPrice - padding),
      maxPrice + padding
    ];
  };

  const yAxisDomain = calculateYAxisDomain();
  
  // Color based on overall trend
  const lineColor = priceDirection === 'decrease' ? '#10b981' : // green for price drop (good)
                   priceDirection === 'increase' ? '#ef4444' : // red for price increase (bad)
                   '#6366f1'; // blue for stable

  const gradientColor = priceDirection === 'decrease' ? '#10b98120' : 
                       priceDirection === 'increase' ? '#ef444420' : 
                       '#6366f120';

  // Custom tooltip component
  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      const data = payload[0].payload;
      return (
        <div className="bg-white dark:bg-gray-800 p-3 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg max-w-64">
          <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
            {data.formattedTime}
          </p>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            Price: <span className="font-semibold text-gray-900 dark:text-gray-100">{data.formattedPrice}</span>
          </p>
          {data.merchantName && (
            <div className="mt-2 pt-2 border-t border-gray-200 dark:border-gray-600">
              <MerchantInfoInline
                merchantName={data.merchantName}
                merchantId={data.merchantId}
                merchantPhone={data.merchantPhone}
                merchantUrl={data.merchantUrl}
                className="text-xs"
              />
            </div>
          )}
        </div>
      );
    }
    return null;
  };

  // Show message if no price history
  if (!priceHistory || priceHistory.length === 0) {
    return (
      <div className={`flex items-center justify-center h-64 bg-gray-50 dark:bg-gray-900 rounded-lg ${className}`}>
        <div className="text-center">
          <div className="text-gray-400 mb-2">📊</div>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            No price history available yet
          </p>
          <p className="text-xs text-gray-500 dark:text-gray-500 mt-1">
            Price tracking will start after the first check
          </p>
        </div>
      </div>
    );
  }

  // Show single point if only one data point
  if (priceHistory.length === 1) {
    return (
      <div className={`flex items-center justify-center h-64 bg-gray-50 dark:bg-gray-900 rounded-lg ${className}`}>
        <div className="text-center">
          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-1">
            {chartData[0].formattedPrice}
          </div>
          <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">
            Initial price recorded
          </p>
          <p className="text-xs text-gray-500 dark:text-gray-500">
            {chartData[0].formattedTime}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className={`w-full ${className}`}>
      {/* Chart Header */}
      <div className="mb-4">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-1">
          Price History
        </h3>
        <p className="text-sm text-gray-600 dark:text-gray-400">
          {productName}
        </p>
        <div className="flex items-center gap-4 mt-2">
          <div className="text-sm">
            <span className="text-gray-500 dark:text-gray-500">Current: </span>
            <span className="font-semibold text-gray-900 dark:text-gray-100">
              {new Intl.NumberFormat('kk-KZ', {
                style: 'currency',
                currency: 'KZT',
                minimumFractionDigits: 0,
              }).format(currentPrice)}
            </span>
          </div>
          <div className="text-sm">
            <span className="text-gray-500 dark:text-gray-500">Trend: </span>
            <span className={`font-semibold ${
              priceDirection === 'decrease' ? 'text-green-600' :
              priceDirection === 'increase' ? 'text-red-600' :
              'text-blue-600'
            }`}>
              {priceDirection === 'decrease' ? '📉 Decreasing' :
               priceDirection === 'increase' ? '📈 Increasing' :
               '➡️ Stable'}
            </span>
          </div>
          {chartData.length > 1 && (
            <div className="text-sm">
              <span className="text-gray-500 dark:text-gray-500">Range: </span>
              <span className="font-medium text-gray-700 dark:text-gray-300">
                {new Intl.NumberFormat('kk-KZ', {
                  style: 'currency',
                  currency: 'KZT',
                  minimumFractionDigits: 0,
                  notation: 'compact'
                }).format(yAxisDomain[0])} - {new Intl.NumberFormat('kk-KZ', {
                  style: 'currency',
                  currency: 'KZT',
                  minimumFractionDigits: 0,
                  notation: 'compact'
                }).format(yAxisDomain[1])}
              </span>
            </div>
          )}
        </div>
      </div>

      {/* Chart Container */}
      <div className="h-80 w-full bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart
            data={chartData}
            margin={{
              top: 5,
              right: 30,
              left: 20,
              bottom: 5,
            }}
          >
            <defs>
              <linearGradient id="priceGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor={lineColor} stopOpacity={0.3}/>
                <stop offset="95%" stopColor={lineColor} stopOpacity={0.05}/>
              </linearGradient>
            </defs>
            <CartesianGrid 
              strokeDasharray="3 3" 
              stroke="#e5e7eb"
              className="dark:stroke-gray-600"
            />
            <XAxis 
              dataKey="formattedTime"
              stroke="#6b7280"
              className="dark:stroke-gray-400"
              fontSize={12}
              tick={{ fontSize: 11 }}
            />
            <YAxis 
              stroke="#6b7280"
              className="dark:stroke-gray-400"
              fontSize={12}
              tick={{ fontSize: 11 }}
              domain={yAxisDomain}
              tickFormatter={(value) => 
                new Intl.NumberFormat('kk-KZ', {
                  style: 'currency',
                  currency: 'KZT',
                  minimumFractionDigits: 0,
                  notation: 'compact'
                }).format(value)
              }
            />
            <Tooltip content={<CustomTooltip />} />
            <Area
              type="monotone"
              dataKey="price"
              stroke={lineColor}
              strokeWidth={2}
              fill="url(#priceGradient)"
              dot={{ fill: lineColor, strokeWidth: 2, r: 4 }}
              activeDot={{ r: 6, stroke: lineColor, strokeWidth: 2 }}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>

      {/* Chart Footer */}
      <div className="mt-3 flex justify-between items-center text-xs text-gray-500 dark:text-gray-500">
        <span>{chartData.length} price points recorded</span>
        <span>
          From {chartData[0]?.formattedTime} to {chartData[chartData.length - 1]?.formattedTime}
        </span>
      </div>
    </div>
  );
}
