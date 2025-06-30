'use client';

import { Button } from '@/components/ui/button'
import { SearchBar } from '@/components/features/product-search/SearchBar'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import Link from 'next/link'
import { useRouter } from 'next/navigation'

export default function HomePage() {
  const router = useRouter();

  /**
   * Handle search from homepage - redirect to search page
   * This provides a seamless transition from landing to search results
   */
  const handleSearch = (query: string) => {
    // Navigate to search page with query parameter
    router.push(`/search?q=${encodeURIComponent(query)}`);
  };

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Hero Section */}
      <div className="text-center space-y-6 mb-12">
        <h1 className="text-4xl font-bold tracking-tight text-gray-900 sm:text-6xl">
          Track Kaspi.kz Prices
        </h1>
        <p className="text-lg text-gray-600 max-w-3xl mx-auto">
          Get notified when your favorite products drop in price. 
          Never miss a deal on Kaspi.kz again.
        </p>
      </div>

      {/* Search Section */}
      <div className="max-w-2xl mx-auto mb-12">
        <SearchBar 
          onSearch={handleSearch}
          placeholder="Search for products on Kaspi.kz..."
        />
        <p className="text-sm text-gray-500 mt-2 text-center">
          Try searching for &ldquo;iPhone&rdquo;, &ldquo;Samsung Galaxy&rdquo;, or &ldquo;MacBook&rdquo;
        </p>
      </div>

      {/* Features Section */}
      <div className="grid md:grid-cols-3 gap-6 mb-12">
        <Card>
          <CardHeader>
            <CardTitle>Price Tracking</CardTitle>
            <CardDescription>
              Track prices for up to 10 products for 24 hours
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-gray-600">
              Add products to your watchlist and get real-time price updates every 15 minutes.
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Instant Notifications</CardTitle>
            <CardDescription>
              Get browser notifications when prices drop
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-gray-600">
              Set price thresholds and receive instant notifications when products hit your target price.
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>No Sign-up Required</CardTitle>
            <CardDescription>
              Start tracking immediately without registration
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-gray-600">
              Anonymous tracking with data stored locally in your browser for privacy.
            </p>
          </CardContent>
        </Card>
      </div>

      {/* CTA Section */}
      <div className="text-center">
        <Link href="/search">
          <Button size="lg">
            Start Tracking Prices
          </Button>
        </Link>
      </div>
    </div>
  )
}