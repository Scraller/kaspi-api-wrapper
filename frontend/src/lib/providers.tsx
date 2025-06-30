'use client';

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useState } from 'react';
import { Toaster } from 'sonner';

/**
 * React Query providers component
 * Provides query client configuration for data fetching and caching
 * Essential for API integration and state management
 */
export function Providers({ children }: { children: React.ReactNode }) {
  // Create a stable query client instance
  const [queryClient] = useState(
    () => new QueryClient({
      defaultOptions: {
        queries: {
          // Stale time - how long data is considered fresh
          staleTime: 5 * 60 * 1000, // 5 minutes
          // Cache time - how long inactive data stays in memory
          gcTime: 10 * 60 * 1000, // 10 minutes (was cacheTime in v4)
          // Retry failed requests
          retry: 2,
          // Retry delay with exponential backoff
          retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
          // Don't refetch on window focus in development
          refetchOnWindowFocus: process.env.NODE_ENV === 'production',
        },
        mutations: {
          // Retry failed mutations once
          retry: 1,
        },
      },
    })
  );

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      <Toaster 
        position="top-right"
        toastOptions={{
          duration: 4000,
          style: {
            background: 'white',
            border: '1px solid #e5e7eb',
            borderRadius: '8px',
          },
        }}
      />
    </QueryClientProvider>
  );
}
