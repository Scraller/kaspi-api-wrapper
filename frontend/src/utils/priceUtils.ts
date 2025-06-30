/**
 * Price utility functions for handling product pricing
 * Provides functions to get best prices, format prices, and handle price comparisons
 */

import { ProductSummaryResponse, ProductDetailResponse } from '@/types';
import { getProductDetails } from '@/services/api';

/**
 * Format price with proper currency formatting
 */
export const formatPrice = (price: number, currency: string = 'KZT'): string => {
  return new Intl.NumberFormat('kk-KZ', {
    style: 'currency',
    currency: currency,
    minimumFractionDigits: 0,
  }).format(price);
};

/**
 * Get the best (minimum) price from product offers
 * This addresses the issue where search returns non-optimal prices
 */
export const getBestPrice = async (productId: string): Promise<{ price: number; currency: string; merchantName?: string } | null> => {
  try {
    const productDetails = await getProductDetails(productId);
    
    if (!productDetails.offers || productDetails.offers.length === 0) {
      return null;
    }

    // Filter only available offers
    const availableOffers = productDetails.offers.filter(offer => offer.availability === 'in_stock');
    
    if (availableOffers.length === 0) {
      return null;
    }

    // Find the offer with minimum price
    const bestOffer = availableOffers.reduce((min, offer) => 
      offer.price < min.price ? offer : min
    );

    return {
      price: bestOffer.price,
      currency: bestOffer.currency,
      merchantName: bestOffer.merchantName
    };
  } catch (error) {
    console.error('Failed to get best price for product:', productId, error);
    return null;
  }
};

/**
 * Calculate price difference and percentage
 */
export const calculatePriceDifference = (currentPrice: number, previousPrice: number) => {
  const difference = currentPrice - previousPrice;
  const percentage = (difference / previousPrice) * 100;
  
  return {
    amount: difference,
    percentage: Math.round(percentage * 100) / 100,
    direction: difference > 0 ? 'increase' : difference < 0 ? 'decrease' : 'no_change'
  };
};

/**
 * Check if a price is significantly different (more than 1% change)
 */
export const isSignificantPriceChange = (currentPrice: number, previousPrice: number, threshold: number = 1): boolean => {
  const change = calculatePriceDifference(currentPrice, previousPrice);
  return Math.abs(change.percentage) >= threshold;
};
