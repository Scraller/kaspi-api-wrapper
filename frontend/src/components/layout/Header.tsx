'use client';

import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { useSubscriptionStore } from '@/stores/subscriptionStore'
import { useEffect } from 'react'
import { Bell } from 'lucide-react'

export default function Header() {
  const { stats, loadSubscriptions } = useSubscriptionStore();

  // Load subscription stats on mount
  useEffect(() => {
    loadSubscriptions();
  }, [loadSubscriptions]);

  return (
    <header className="border-b bg-white">
      <div className="container mx-auto px-4 py-4 flex items-center justify-between">
        <Link href="/" className="text-2xl font-bold text-blue-600">
          Kaspi Price Tracker
        </Link>
        
        <nav className="flex items-center space-x-6">
          <Link href="/" className="text-gray-600 hover:text-gray-900">
            Search
          </Link>
          <Link href="/dashboard" className="text-gray-600 hover:text-gray-900 flex items-center space-x-1">
            <Bell className="h-4 w-4" />
            <span>Dashboard</span>
            {stats.count > 0 && (
              <Badge variant="secondary" className="ml-1 text-xs">
                {stats.count}
              </Badge>
            )}
          </Link>
          <Link href="/demo" className="text-gray-600 hover:text-gray-900">
            Demo
          </Link>
          <Button variant="outline" size="sm">
            Sign In
          </Button>
        </nav>
      </div>
    </header>
  )
}
