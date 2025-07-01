import { ProductSearchResponse, ProductDetailResponse, SearchRequest, HierarchicalCategoryInfo, MerchantDetailResponse } from '@/types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5147';

/**
 * Search products using the advanced search endpoint
 * This is the primary search function that supports text, category, and pagination
 * 
 * Note: Currently returns representative prices from search, not always the best available price.
 * For accurate minimum pricing, use getProductDetails() to fetch all merchant offers.
 * This is a known limitation that should be addressed in the backend search logic.
 */
export const searchProducts = async (searchParams: SearchRequest): Promise<ProductSearchResponse> => {
  const requestBody = {
    text: searchParams.text,
    category: searchParams.category,
    page: searchParams.page || 0,
    pageSize: searchParams.pageSize || 20,
    sortOptions: searchParams.sortOptions || ['relevance']
  };

  const response = await fetch(`${API_BASE_URL}/api/search/advanced`, {
    method: 'POST',
    headers: { 
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(requestBody)
  });

  if (!response.ok) {
    throw new Error(`Search failed: ${response.status} ${response.statusText}`);
  }

  const result = await response.json();
  
  // Backend returns wrapped response with data field
  if (result.success && result.data) {
    return result.data;
  } else {
    throw new Error(result.message || 'Search failed');
  }
};

/**
 * Get detailed product information by product ID
 * Used for product detail pages and subscription management
 */
export const getProductDetails = async (productId: string, cityCode?: string): Promise<ProductDetailResponse> => {
  const params = new URLSearchParams();
  if (cityCode) params.append('cityCode', cityCode);
  
  const url = `${API_BASE_URL}/api/products/${productId}${params.toString() ? `?${params.toString()}` : ''}`;
  
  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`Failed to fetch product details: ${response.status} ${response.statusText}`);
  }

  const result = await response.json();
  
  // Backend returns wrapped response with data field
  if (result.success && result.data) {
    return result.data;
  } else {
    throw new Error(result.message || 'Failed to fetch product details');
  }
};

/**
 * Get categories for filtering and navigation
 * Includes subcategories for better user experience
 */
export const getCategories = async (): Promise<HierarchicalCategoryInfo[]> => {
  const response = await fetch(`${API_BASE_URL}/api/categories/kaspi?includeSubcategories=true`);

  if (!response.ok) {
    throw new Error(`Failed to fetch categories: ${response.status} ${response.statusText}`);
  }

  const result = await response.json();
  
  // Backend returns wrapped response with data field
  if (result.success && result.data) {
    return result.data;
  } else {
    throw new Error(result.message || 'Failed to fetch categories');
  }
};

/**
 * Health check endpoint to verify API connectivity
 * Used for monitoring and debugging
 */
export const healthCheck = async (): Promise<{ status: string }> => {
  const response = await fetch(`${API_BASE_URL}/api/test/health`);

  if (!response.ok) {
    throw new Error(`Health check failed: ${response.status} ${response.statusText}`);
  }

  const result = await response.json();
  
  // Health check might not be wrapped, check both patterns
  if (result.success && result.data) {
    return result.data;
  } else if (result.status) {
    return result;
  } else {
    throw new Error(result.message || 'Health check failed');
  }
};

/**
 * Get detailed merchant information by merchant ID
 * Used for fetching phone numbers and contact details
 */
export const getMerchantDetails = async (merchantId: string): Promise<MerchantDetailResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/merchants/${encodeURIComponent(merchantId)}`);

  if (!response.ok) {
    throw new Error(`Failed to fetch merchant details: ${response.status} ${response.statusText}`);
  }

  const result = await response.json();
  
  // Backend returns wrapped response with data field
  if (result.success && result.data) {
    return result.data;
  } else {
    throw new Error(result.message || 'Failed to fetch merchant details');
  }
};
