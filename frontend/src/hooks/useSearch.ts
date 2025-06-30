import { useQuery, UseQueryResult } from '@tanstack/react-query';
import { useState, useCallback } from 'react';
import { ProductSearchResponse, SearchRequest } from '@/types';
import { searchProducts } from '@/services/api';

interface UseSearchOptions {
  enabled?: boolean;
  staleTime?: number;
}

interface UseSearchReturn {
  searchQuery: string;
  setSearchQuery: (query: string) => void;
  searchResults: UseQueryResult<ProductSearchResponse, Error>;
  executeSearch: (params?: Partial<SearchRequest>) => void;
  isSearching: boolean;
}

/**
 * Custom hook for managing product search functionality
 * Provides debounced search, caching, and state management
 */
export const useSearch = (options: UseSearchOptions = {}): UseSearchReturn => {
  const [searchQuery, setSearchQuery] = useState('');
  const [searchParams, setSearchParams] = useState<SearchRequest | null>(null);

  // React Query for search results with caching
  const searchResults = useQuery({
    queryKey: ['products', 'search', searchParams],
    queryFn: () => searchProducts(searchParams!),
    enabled: !!searchParams && options.enabled !== false,
    staleTime: options.staleTime || 5 * 60 * 1000, // 5 minutes cache
    retry: 2,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
  });

  /**
   * Execute search with provided parameters
   * Merges with current search state and triggers API call
   */
  const executeSearch = useCallback((params: Partial<SearchRequest> = {}) => {
    const textToSearch = params.text || searchQuery.trim();
    if (!textToSearch) return;

    const searchRequest: SearchRequest = {
      text: textToSearch,
      page: 0,
      pageSize: 20,
      sortOptions: ['relevance'],
      ...params,
    };

    setSearchParams(searchRequest);
  }, [searchQuery]);

  return {
    searchQuery,
    setSearchQuery,
    searchResults,
    executeSearch,
    isSearching: searchResults.isFetching,
  };
};
