import { useState, useEffect } from 'react';
import { getProductDetails } from '@/services/api';

interface BestPriceInfo {
  price: number;
  currency: string;
  merchantName: string;
  loading: boolean;
  error: boolean;
}

/**
 * Hook to fetch the real-time best price for a product
 * This addresses the issue where search results show outdated/non-optimal prices
 */
export const useBestPrice = (productId: string, enabled: boolean = true) => {
  const [bestPrice, setBestPrice] = useState<BestPriceInfo | null>(null);

  useEffect(() => {
    if (!enabled || !productId) {
      return;
    }

    let isCancelled = false;

    const fetchBestPrice = async () => {
      setBestPrice(prev => prev ? { ...prev, loading: true } : null);

      try {
        const productDetails = await getProductDetails(productId);
        
        if (isCancelled) return;

        // Filter only available offers
        const availableOffers = productDetails.offers?.filter(
          offer => offer.availability === 'in_stock'
        ) || [];

        if (availableOffers.length === 0) {
          setBestPrice({
            price: 0,
            currency: 'KZT',
            merchantName: 'N/A',
            loading: false,
            error: true
          });
          return;
        }

        // Find the offer with minimum price
        const bestOffer = availableOffers.reduce((min, offer) => 
          offer.price < min.price ? offer : min
        );

        setBestPrice({
          price: bestOffer.price,
          currency: bestOffer.currency,
          merchantName: bestOffer.merchantName,
          loading: false,
          error: false
        });

      } catch (error) {
        if (!isCancelled) {
          console.error('Failed to fetch best price for product:', productId, error);
          setBestPrice({
            price: 0,
            currency: 'KZT',
            merchantName: 'N/A',
            loading: false,
            error: true
          });
        }
      }
    };

    fetchBestPrice();

    return () => {
      isCancelled = true;
    };
  }, [productId, enabled]);

  return bestPrice;
};
