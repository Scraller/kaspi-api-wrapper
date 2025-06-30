import { ProductCard } from './ProductCard';
import { ProductSearchResponse } from '@/types';
import Alert from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { AlertCircle, RefreshCw } from 'lucide-react';

interface SearchResultsProps {
  searchResults?: ProductSearchResponse;
  isLoading?: boolean;
  error?: Error | null;
  onLoadMore?: () => void;
}

/**
 * SearchResults component displays product search results in a grid layout
 * Handles loading states, errors, pagination, and empty states
 * Core component for displaying search results and enabling subscriptions
 */
export function SearchResults({ 
  searchResults, 
  isLoading, 
  error,
  onLoadMore
}: SearchResultsProps) {
  // Loading state
  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-center py-8">
          <div className="flex items-center gap-2">
            <RefreshCw className="h-5 w-5 animate-spin" />
            <span>Поиск товаров...</span>
          </div>
        </div>
        
        {/* Loading skeleton */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, index) => (
            <div key={index} className="border rounded-lg p-4 animate-pulse">
              <div className="aspect-square bg-muted rounded-md mb-4"></div>
              <div className="space-y-2">
                <div className="h-4 bg-muted rounded w-3/4"></div>
                <div className="h-4 bg-muted rounded w-1/2"></div>
                <div className="h-6 bg-muted rounded w-1/3"></div>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <Alert 
        type="error"
        message={`Произошла ошибка при поиске товаров: ${error.message}`}
      />
    );
  }

  // No results state
  if (!searchResults || !searchResults.products || searchResults.products.length === 0) {
    return (
      <div className="text-center py-12">
        <div className="text-muted-foreground text-lg mb-2">
          Товары не найдены
        </div>
        <div className="text-sm text-muted-foreground">
          Попробуйте изменить поисковый запрос или использовать другие ключевые слова
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Results header with count and query */}
      <div className="flex items-center justify-between">
        <div className="text-sm text-muted-foreground">
          Найдено {searchResults?.totalCount || searchResults?.products?.length || 0} товаров
          {searchResults?.searchQuery && (
            <span> по запросу &ldquo;{searchResults.searchQuery}&rdquo;</span>
          )}
        </div>
        
        {searchResults?.category && (
          <div className="text-sm text-muted-foreground">
            Категория: {searchResults.category}
          </div>
        )}
      </div>

      {/* Products grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        {searchResults?.products?.map((product, index) => (
          <ProductCard
            key={product.id}
            product={product}
            showRealTimePrice={index < 4} // Show real-time price for first 4 results to avoid too many API calls
            showImageCarousel={index < 6} // Show image carousel for first 6 results
          />
        ))}
      </div>

      {/* Load More button */}
      {searchResults?.hasNextPage && onLoadMore && (
        <div className="flex justify-center pt-6">
          <Button 
            variant="outline" 
            onClick={onLoadMore}
            className="px-8"
          >
            Загрузить еще
          </Button>
        </div>
      )}

      {/* Pagination info */}
      <div className="text-center text-sm text-muted-foreground">
        Страница {(searchResults?.page ?? 0) + 1}
        {searchResults?.hasNextPage && searchResults?.totalCount && searchResults?.pageSize && (
          <span> из {Math.ceil(searchResults.totalCount / searchResults.pageSize)}</span>
        )}
      </div>
    </div>
  );
}
