'use client';

import dynamic from 'next/dynamic';

/**
 * Dynamic import wrapper for PricePollingManager to avoid SSR issues
 * This ensures the component only runs on the client side
 */
const PricePollingManager = dynamic(
  () => import('@/components/features/notifications/PricePollingManager'),
  {
    ssr: false,
    loading: () => null, // No loading component needed since this is invisible
  }
);

export default PricePollingManager;
