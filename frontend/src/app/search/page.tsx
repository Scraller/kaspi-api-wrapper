'use client';

import { SearchBar } from '@/components/features/product-search/SearchBar';
import { SearchResults } from '@/components/features/product-search/SearchResults';
import { useSearch } from '@/hooks/useSearch';
import { useState, useEffect } from 'react';
import { useSearchParams } from 'next/navigation';

/**
 * Search page component - dedicated page for product search functionality
 * Combines SearchBar and SearchResults with state management
 * Provides clean URL structure for search operations
 */
export default function SearchPage() {
  const { searchResults, isSearching, executeSearch } = useSearch();
  const [currentPage, setCurrentPage] = useState(0);
  const searchParams = useSearchParams();

  // Execute search on page load if query parameter exists
  useEffect(() => {
    const query = searchParams.get('q');
    if (query) {
      executeSearch({ text: query });
    }
  }, [searchParams, executeSearch]);

  /**
   * Handle search execution from SearchBar
   * Could be used for analytics or additional processing
   */
  const handleSearch = (query: string) => {
    setCurrentPage(0);
    executeSearch({ text: query });
    console.log('Search executed:', query);
  };

  /**
   * Handle subscription to product from search results
   * This will be implemented in Iteration 3 with localStorage
   */
  const handleSubscribe = (productId: string) => {
    console.log('Subscribe to product:', productId);
    // TODO: Implement in Iteration 3
  };

  /**
   * Handle loading more results (pagination)
   * Loads next page while preserving current results
   */
  const handleLoadMore = () => {
    if (searchResults.data?.hasNextPage) {
      setCurrentPage(prev => prev + 1);
      // TODO: Implement pagination logic with current search params
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Page Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold mb-2">Поиск товаров</h1>
        <p className="text-muted-foreground">
          Найдите товары на Kaspi.kz и добавьте их в список отслеживания
        </p>
      </div>

      {/* Search Bar */}
      <div className="mb-8">
        <SearchBar 
          onSearch={handleSearch}
          className="max-w-2xl mx-auto"
          isLoading={isSearching}
        />
      </div>

      {/* Search Results */}
      <SearchResults
        searchResults={searchResults.data}
        isLoading={isSearching}
        error={searchResults.error}
        onSubscribe={handleSubscribe}
        onLoadMore={searchResults.data?.hasNextPage ? handleLoadMore : undefined}
        subscribedProductIds={[]} // TODO: Get from localStorage in Iteration 3
      />
    </div>
  );
}