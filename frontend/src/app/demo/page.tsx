'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ProductCard } from '@/components/features/product-search/ProductCard';
import { PriceThresholdInput } from '@/components/features/subscription/PriceThresholdInput';
import { WatchlistManager } from '@/components/features/subscription/WatchlistManager';
import { StatsCards } from '@/components/features/dashboard/StatsCards';
import { WatchlistGrid } from '@/components/features/dashboard/WatchlistGrid';
import { PriceIndicator } from '@/components/features/dashboard/PriceIndicator';
import { PriceRecommendation } from '@/components/features/dashboard/PriceRecommendation';
import { useSubscriptionStore } from '@/stores/subscriptionStore';
import { useWatchlist } from '@/hooks/useWatchlist';
import { ProductSummaryResponse } from '@/types';
import { runAllTests } from '@/utils/testSubscriptions';

/**
 * Demo page to showcase subscription functionality with mock data
 * This demonstrates all features of Iteration 3 & 4: Subscription System + Enhanced Dashboard
 */
export default function SubscriptionDemoPage() {
  const { loadSubscriptions } = useSubscriptionStore();
  const { subscriptions, dashboardStats, handleRemoveSubscription } = useWatchlist();
  const [priceThreshold, setPriceThreshold] = useState<number | undefined>(undefined);

  // Mock product data for demonstration
  const mockProducts: ProductSummaryResponse[] = [
    {
      id: 'iphone-15-pro-1',
      name: 'Apple iPhone 15 Pro 128GB Titanium Natural',
      slug: 'apple-iphone-15-pro-128gb-titanium-natural',
      price: 599000,
      currency: 'KZT',
      imageUrl: '/placeholder-product.svg',
      rating: 4.8,
      reviewCount: 245,
      availability: 'in_stock',
      category: 'Smartphones',
    },
    {
      id: 'samsung-galaxy-s24-1',
      name: 'Samsung Galaxy S24 Ultra 256GB Titanium Black',
      slug: 'samsung-galaxy-s24-ultra-256gb-titanium-black',
      price: 720000,
      currency: 'KZT',
      imageUrl: '/placeholder-product.svg',
      rating: 4.7,
      reviewCount: 189,
      availability: 'in_stock',
      category: 'Smartphones',
    },
    {
      id: 'macbook-air-m3-1',
      name: 'Apple MacBook Air 13" M3 8GB/256GB Space Gray',
      slug: 'apple-macbook-air-13-m3-8gb-256gb-space-gray',
      price: 850000,
      currency: 'KZT',
      imageUrl: '/placeholder-product.svg',
      rating: 4.9,
      reviewCount: 156,
      availability: 'in_stock',
      category: 'Laptops',
    },
    {
      id: 'airpods-pro-2-1',
      name: 'Apple AirPods Pro 2nd Generation USB-C',
      slug: 'apple-airpods-pro-2nd-generation-usb-c',
      price: 189000,
      currency: 'KZT',
      imageUrl: '/placeholder-product.svg',
      rating: 4.6,
      reviewCount: 412,
      availability: 'out_of_stock',
      category: 'Audio',
    },
  ];

  /**
   * Run localStorage tests in browser console
   */
  const handleRunTests = async () => {
    console.log('Running subscription system tests...');
    await runAllTests();
    console.log('Tests completed! Check the console for results.');
    
    // Refresh the store after tests
    loadSubscriptions();
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="max-w-6xl mx-auto space-y-8">
        {/* Page Header */}
        <div className="text-center space-y-4">
          <h1 className="text-4xl font-bold text-gray-900">
            Subscription & Dashboard Demo
          </h1>
          <p className="text-lg text-gray-600 max-w-3xl mx-auto">
            Iteration 3 & 4: Local Storage Subscription System + Enhanced Dashboard with Statistics, Sorting & Advanced UI
          </p>
          <Button onClick={handleRunTests} variant="outline">
            Run Tests (Check Console)
          </Button>
        </div>

        {/* Feature Overview */}
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Anonymous Subscriptions</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-gray-600">
                No sign-up required. Data stored locally with 24h expiry.
              </p>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Price Thresholds</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-gray-600">
                Set custom price alerts. Get notified when prices drop.
              </p>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">10 Product Limit</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-gray-600">
                Track up to 10 products for anonymous users.
              </p>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Auto Expiry</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-gray-600">
                Subscriptions automatically expire after 24 hours.
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Price Threshold Demo */}
        <Card>
          <CardHeader>
            <CardTitle>Price Threshold Demo</CardTitle>
            <CardDescription>
              Test the price threshold input component
            </CardDescription>
          </CardHeader>
          <CardContent>
            <PriceThresholdInput
              currentPrice={599000}
              currency="KZT"
              defaultThreshold={priceThreshold}
              onThresholdSet={setPriceThreshold}
              className="max-w-md"
            />
            {priceThreshold && (
              <div className="mt-4 p-3 bg-green-50 border border-green-200 rounded-lg">
                <div className="text-green-800 text-sm">
                  Price threshold set: {priceThreshold.toLocaleString()} KZT
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Mock Product Grid */}
        <div>
          <h2 className="text-2xl font-bold mb-6">Demo Products</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            {mockProducts.map((product) => (
              <ProductCard
                key={product.id}
                product={product}
                priceThreshold={priceThreshold}
                showRealTimePrice={false}
                showImageCarousel={false}
              />
            ))}
          </div>
        </div>

        {/* Dashboard Components Demo */}
        <div>
          <h2 className="text-2xl font-bold mb-6">Dashboard Components (Iteration 4)</h2>
          <div className="space-y-6">
            {/* Statistics Cards */}
            <div>
              <h3 className="text-lg font-semibold mb-3">Statistics Cards</h3>
              <StatsCards subscriptions={subscriptions} />
            </div>

            {/* Price Indicators Demo */}
            <div>
              <h3 className="text-lg font-semibold mb-3">Price Indicators</h3>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {subscriptions.slice(0, 3).map((sub) => (
                  <Card key={sub.productId}>
                    <CardHeader>
                      <CardTitle className="text-sm">{sub.productName}</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-2">
                      <PriceIndicator subscription={sub} showChange={true} showPercentage={true} />
                      <PriceRecommendation subscription={sub} size="sm" />
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>

            {/* Enhanced Grid */}
            {subscriptions.length > 0 && (
              <div>
                <h3 className="text-lg font-semibold mb-3">Enhanced Watchlist Grid</h3>
                <WatchlistGrid
                  subscriptions={subscriptions}
                  onRemoveSubscription={handleRemoveSubscription}
                  isLoading={false}
                />
              </div>
            )}
          </div>
        </div>

        {/* Watchlist Manager */}
        <div>
          <h2 className="text-2xl font-bold mb-6">Your Watchlist</h2>
          <WatchlistManager />
        </div>

        {/* Implementation Notes */}
        <Card>
          <CardHeader>
            <CardTitle>Implementation Notes</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <h3 className="font-semibold mb-2">Iteration 3 Features Completed:</h3>
              <ul className="list-disc list-inside space-y-1 text-sm text-gray-600">
                <li>✅ localStorage utilities for subscription management</li>
                <li>✅ Zustand store for reactive state management</li>
                <li>✅ SubscribeButton component with toast notifications</li>
                <li>✅ PriceThresholdInput with validation and quick options</li>
                <li>✅ WatchlistManager for CRUD operations</li>
                <li>✅ 24-hour expiry mechanism</li>
                <li>✅ 10 product subscription limit</li>
                <li>✅ Price change tracking</li>
                <li>✅ Integration with existing ProductCard</li>
                <li>✅ Dashboard page for managing subscriptions</li>
              </ul>
            </div>
            
            <div>
              <h3 className="font-semibold mb-2">Next Steps (Iteration 4):</h3>
              <ul className="list-disc list-inside space-y-1 text-sm text-gray-600">
                <li>🔄 Dashboard & Watchlist Display enhancement</li>
                <li>🔄 Price change percentage calculations</li>
                <li>🔄 Sort/filter options for watchlist</li>
                <li>🔄 Enhanced UI polish and animations</li>
              </ul>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
