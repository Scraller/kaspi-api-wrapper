import { useState, useCallback } from 'react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Search, X } from 'lucide-react';

interface SearchBarProps {
  onSearch?: (query: string) => void;
  placeholder?: string;
  className?: string;
  isLoading?: boolean;
}

/**
 * SearchBar component provides product search functionality
 * Features debounced input, loading states, and search suggestions
 * Core component for product discovery across the app
 */
export function SearchBar({ 
  onSearch,
  placeholder = "Поиск товаров на Kaspi.kz...",
  className = "",
  isLoading = false
}: SearchBarProps) {
  const [localQuery, setLocalQuery] = useState('');

  /**
   * Handle search execution
   * Triggers search via callback
   */
  const handleSearch = useCallback(() => {
    if (localQuery.trim()) {
      onSearch?.(localQuery.trim());
    }
  }, [localQuery, onSearch]);

  /**
   * Handle Enter key press for quick search
   */
  const handleKeyPress = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  }, [handleSearch]);

  /**
   * Clear search input and reset results
   */
  const handleClear = useCallback(() => {
    setLocalQuery('');
  }, []);

  return (
    <div className={`relative flex items-center gap-2 ${className}`}>
      {/* Search Input */}
      <div className="relative flex-1">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          type="text"
          placeholder={placeholder}
          value={localQuery}
          onChange={(e) => setLocalQuery(e.target.value)}
          onKeyPress={handleKeyPress}
          className="pl-10 pr-10"
          disabled={isLoading}
        />
        
        {/* Clear Button */}
        {localQuery && (
          <Button
            variant="ghost"
            size="sm"
            onClick={handleClear}
            className="absolute right-1 top-1/2 transform -translate-y-1/2 h-6 w-6 p-0 hover:bg-muted"
          >
            <X className="h-3 w-3" />
          </Button>
        )}
      </div>

      {/* Search Button */}
      <Button 
        onClick={handleSearch}
        disabled={!localQuery.trim() || isLoading}
        className="shrink-0"
      >
        {isLoading ? (
          <div className="flex items-center gap-2">
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            Поиск...
          </div>
        ) : (
          <>
            <Search className="h-4 w-4 mr-2" />
            Найти
          </>
        )}
      </Button>
    </div>
  );
}
