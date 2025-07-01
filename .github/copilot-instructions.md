# Copilot Instructions: Kaspi Price Tracker Frontend MVP Development

## Main Rules:
- Refer to context7 MCP for dependencies and libraries
- Write comments in functions, which help you (agent) to understand the importance of the function/feature.

## Context Overview
You are developing a **Next.js 14 + TypeScript** frontend for a Kaspi.kz price tracking application. The MVP focuses on **anonymous users** using **localStorage** for 24-hour product subscriptions with **browser notifications**.

## Backend API Schema (Available Endpoints)

### Core MVP Endpoints
```typescript
// Product Search (Primary)
GET /api/search/advanced
POST /api/search/advanced
Content-Type: application/json
{
  "text": "iPhone 15 Pro",
  "category": "smartphones", 
  "page": 0,
  "pageSize": 20,
  "sortOptions": ["price_asc"]
}

// Product Details
GET /api/products/{productId}?cityCode=750000000

// Categories
GET /api/categories/kaspi?includeSubcategories=true

// Health Check
GET /api/test/health
```

### Response Models
```typescript
// Search Response
interface ProductSearchResponse {
  products: ProductSummaryResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  searchQuery: string;
  category?: string;
}

interface ProductSummaryResponse {
  id: string;
  name: string;
  slug: string;
  price: number;
  currency: string; // "KZT"
  imageUrl?: string;
  rating?: number;
  reviewCount?: number;
  availability: "in_stock" | "out_of_stock";
  category: string;
}

// Product Detail Response
interface ProductDetailResponse {
  id: string;
  name: string;
  slug: string;
  title?: string;
  description?: string;
  galleryImages: string[];
  kaspiUrl: string;
  brand?: string;
  offers: ProductOffer[];
  specifications: SpecificationGroup[];
}

interface ProductOffer {
  id: string;
  price: number;
  currency: string;
  merchantName: string;
  merchantId: string;
  availability: "in_stock" | "out_of_stock";
  deliveryOptions: string[];
}

// Categories Response
interface HierarchicalCategoryInfo {
  id: string;
  name: string;
  slug: string;
  parentCategoryId?: string;
  subcategories: HierarchicalCategoryInfo[];
  productCount: number;
}
```

## Development Flow: 8 Short Iterations

### Iteration 1: Project Setup & Basic Structure
**Duration**: 30-45 minutes
**Goal**: Create Next.js project with TypeScript, Tailwind, and shadcn/ui

**Tasks**:
1. Initialize Next.js 14 project with TypeScript
2. Install and configure Tailwind CSS
3. Set up shadcn/ui components library
4. Create basic folder structure following PRD
5. Set up environment variables for API_URL
6. Create basic layout with Header and Navigation

**Key Files to Create**:
```bash
src/
├── app/
│   ├── layout.tsx           # Root layout with Inter font
│   ├── page.tsx             # Landing page with search bar
│   └── globals.css          # Tailwind imports
├── components/
│   ├── ui/                  # shadcn/ui components (button, input, card)
│   └── layout/
│       ├── Header.tsx       # Navigation header
│       └── Navigation.tsx   # Main navigation
├── lib/
│   ├── utils.ts            # shadcn/ui utils
│   └── api.ts              # API client setup
└── types/
    └── index.ts            # TypeScript interfaces
```

**Environment Setup**:
```bash
# .env.local
NEXT_PUBLIC_API_URL=http://localhost:5147
```

**Acceptance Criteria**:
- [x] Next.js app runs on localhost:3000
- [x] Tailwind CSS styling works
- [x] shadcn/ui Button component displays correctly
- [x] TypeScript compilation without errors
- [x] Basic header with site title and navigation

---

### Iteration 2: API Integration & Search Functionality
**Duration**: 45-60 minutes
**Goal**: Implement product search with API integration

**Tasks**:
1. Create API service for backend integration
2. Set up React Query for data fetching and caching
3. Implement search functionality (SearchBar component)
4. Create ProductCard component for search results
5. Add loading states and error handling

**Key Files to Create/Update**:
```bash
src/
├── services/
│   └── api.ts              # API client with React Query
├── components/
│   └── features/
│       └── product-search/
│           ├── SearchBar.tsx      # Search input (manual search only)
│           ├── SearchResults.tsx  # Grid of search results
│           └── ProductCard.tsx    # Individual product display
├── hooks/
│   └── useSearch.ts        # Custom hook for search logic
└── app/
    └── search/
        └── page.tsx        # Search results page
```

**API Integration**:
```typescript
// services/api.ts
import { QueryClient } from '@tanstack/react-query';

export const searchProducts = async (query: string, page = 0) => {
  const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/search/advanced`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      text: query,
      page,
      pageSize: 20,
      sortOptions: ['relevance']
    })
  });
  return response.json();
};
```

**Search Implementation Notes**:
- **Manual Search Only**: Users must click search button or press Enter
- **No Autocomplete**: Too heavy for API performance and causes timeouts
- **No Real-time Search**: API response time makes live search impractical
- **No Popular/Trending**: Feature scope limited to core price tracking functionality
- **Debouncing**: Only applied to prevent double-clicks, not for live search

**Acceptance Criteria**:
- [x] Search bar accepts user input
- [x] API requests to backend work correctly
- [x] Search results display in card format
- [x] Loading spinner during search
- [x] Error handling for failed requests
- [x] Product cards show image, name, price, merchant
- [x] Manual search trigger (button/Enter key)

---

### Iteration 3: Local Storage Subscription System
**Duration**: 45-60 minutes  
**Goal**: Implement anonymous subscription system with localStorage

**Tasks**:
1. Create localStorage utilities for subscription management
2. Implement SubscribeButton component
3. Add price threshold input functionality
4. Create WatchlistManager for subscription CRUD
5. Add 24-hour expiry mechanism

**Key Files to Create/Update**:
```bash
src/
├── utils/
│   └── localStorage.ts     # localStorage utilities
├── stores/
│   └── subscriptionStore.ts # Zustand store for subscriptions
├── components/
│   └── features/
│       └── subscription/
│           ├── SubscribeButton.tsx      # Add to watchlist
│           ├── PriceThresholdInput.tsx  # Set price alert
│           └── WatchlistManager.tsx     # Manage subscriptions
└── types/
    └── subscription.ts     # Subscription interfaces
```

**Local Storage Schema**:
```typescript
interface AnonymousSubscription {
  productId: string;
  productName: string;
  productImage?: string;
  currentPrice: number;
  priceThreshold?: number;
  subscribedAt: Date;
  expiresAt: Date; // 24h from creation
  lastCheckedPrice?: number;
  priceChange?: 'increase' | 'decrease' | 'no_change';
}

// localStorage key: 'kaspi_subscriptions'
// Max 10 subscriptions for anonymous users
```

**Acceptance Criteria**:
- [x] Users can subscribe to products from search results
- [x] Subscriptions saved to localStorage
- [x] Price threshold setting works
- [x] 10 product limit enforced
- [x] 24-hour expiry implemented
- [x] Subscribe/unsubscribe toggles correctly

---

### Iteration 4: Dashboard & Watchlist Display  
**Duration**: 45-60 minutes
**Goal**: Create dashboard to display subscribed products

**Tasks**:
1. Create dashboard layout and routing
2. Implement WatchlistGrid component
3. Add PriceIndicator for price changes
4. Create price change percentage calculations
5. Add sort/filter options for watchlist

**Key Files to Create/Update**:
```bash
src/
├── app/
│   └── dashboard/
│       └── page.tsx        # Main dashboard page
├── components/
│   └── features/
│       └── dashboard/
│           ├── WatchlistGrid.tsx    # Grid layout for products
│           ├── PriceIndicator.tsx   # Price change visuals
│           ├── StatsCards.tsx       # Summary statistics
│           └── EmptyState.tsx       # No subscriptions state
└── hooks/
    └── useWatchlist.ts     # Dashboard data logic
```

**Dashboard Features**:
```typescript
// Price change calculations
const getPriceChange = (current: number, previous: number) => {
  const change = current - previous;
  const percentage = (change / previous) * 100;
  return {
    amount: change,
    percentage: Math.round(percentage * 100) / 100,
    direction: change > 0 ? 'increase' : change < 0 ? 'decrease' : 'no_change'
  };
};

// Sort options: price_change, date_added, alphabetical
// Filter options: price_drops_only, in_stock_only
```

**Acceptance Criteria**:
- [x] Dashboard shows all subscribed products
- [x] Price changes displayed with visual indicators (red/green)
- [x] "Quick Buy" links to kaspi.kz work
- [x] Sort options function correctly  
- [x] Empty state when no subscriptions
- [x] Responsive grid layout

---

### Iteration 5: Price Update Polling System
**Duration**: 60 minutes
**Goal**: Implement 15-minute price checking with notifications

**Tasks**:
1. Create price polling service with intervals
2. Implement price comparison logic
3. Set up browser notification permissions
4. Create notification display system
5. Add notification preferences

**Key Files to Create/Update**:
```bash
src/
├── services/
│   ├── pricePolling.ts     # Price update polling
│   └── notifications.ts   # Browser notification API
├── components/
│   └── features/
│       └── notifications/
│           ├── NotificationPermission.tsx  # Request permissions
│           ├── NotificationToast.tsx       # In-app notifications
│           └── NotificationSettings.tsx    # User preferences
└── hooks/
    ├── useNotifications.ts # Notification logic
    └── usePricePolling.ts  # Price polling hook
```

**Polling Implementation**:
```typescript
// Price polling every 15 minutes when tab is active
const POLLING_INTERVAL = 15 * 60 * 1000; // 15 minutes

const checkPriceUpdates = async (subscriptions: AnonymousSubscription[]) => {
  for (const subscription of subscriptions) {
    try {
      const productData = await getProductDetails(subscription.productId);
      const currentPrice = productData.offers[0]?.price;
      
      if (currentPrice && currentPrice !== subscription.lastCheckedPrice) {
        // Update price and check threshold
        updateSubscriptionPrice(subscription.productId, currentPrice);
        
        if (subscription.priceThreshold && currentPrice <= subscription.priceThreshold) {
          showPriceAlert(subscription, currentPrice);
        }
      }
    } catch (error) {
      console.error('Price check failed:', error);
    }
  }
};
```

**Acceptance Criteria**:
- [x] Price polling runs every 15 minutes when tab active
- [x] Browser notification permission requested
- [x] Notifications show for price drops below threshold
- [x] Price changes update in localStorage
- [x] Visual indicators update in dashboard
- [x] Notifications include product name and price change

---

### Iteration 6: Product Detail Page & Enhanced UI
**Duration**: 45-60 minutes
**Goal**: Create product detail page and improve UI polish

**Tasks**:
1. Create product detail page with routing
2. Implement enhanced ProductCard design
3. Add product image gallery
4. Create detailed price and offer display
5. Improve overall UI/UX polish

**Key Files to Create/Update**:
```bash
src/
├── app/
│   └── products/
│       └── [productId]/
│           └── page.tsx    # Product detail page
├── components/
│   ├── common/
│   │   ├── ProductImage.tsx     # Optimized image component
│   │   ├── PriceDisplay.tsx     # Price formatting
│   │   └── LoadingSpinner.tsx   # Loading states
│   └── features/
│       └── product-detail/
│           ├── ProductGallery.tsx     # Image gallery
│           ├── OffersList.tsx         # Merchant offers
│           └── ProductSpecs.tsx       # Specifications
```

**Product Detail Features**:
- Product image gallery with zoom
- Multiple merchant offers comparison
- Detailed specifications display
- Enhanced subscribe functionality
- Breadcrumb navigation
- Share functionality

**Acceptance Criteria**:
- [x] Product detail page accessible via routing
- [x] Product gallery displays multiple images
- [x] Multiple merchant offers shown
- [x] Subscribe functionality works from detail page
- [x] Responsive design on mobile
- [x] Loading states during data fetch

---

### Iteration 7: Offline Support & PWA Features
**Duration**: 60 minutes
**Goal**: Add offline functionality and PWA capabilities

**Tasks**:
1. Implement service worker for caching
2. Add offline detection and handling
3. Cache critical assets and API responses
4. Create PWA manifest file
5. Add installation prompts

**Key Files to Create/Update**:
```bash
public/
├── manifest.json           # PWA manifest
└── sw.js                  # Service worker
src/
├── components/
│   └── common/
│       ├── OfflineIndicator.tsx  # Offline status
│       └── InstallPrompt.tsx     # PWA install
└── hooks/
    ├── useOffline.ts       # Offline detection
    └── usePWA.ts          # PWA functionality
```

**Service Worker Strategy**:
```typescript
// Cache Strategy:
// - Cache First: Static assets (images, fonts, icons)
// - Network First: API calls with offline fallback
// - Stale While Revalidate: Product data and search results

// Offline Features:
// - Cached search results
// - Saved subscriptions work offline
// - Notification queue for when online
// - Cached product images
```

**Acceptance Criteria**:
- [x] App works offline with cached data
- [x] Offline indicator shows connection status
- [x] PWA can be installed on mobile/desktop
- [x] Service worker caches important assets
- [x] Cached API responses available offline
- [x] Search history works offline

---

### Iteration 8: Testing, Error Handling & Performance
**Duration**: 60-90 minutes
**Goal**: Add comprehensive testing and optimize performance

**Tasks**:
1. Set up Jest and React Testing Library
2. Write unit tests for core components
3. Add comprehensive error handling
4. Implement performance optimizations
5. Add error boundaries and fallbacks
6. Final bug fixes and polish

**Key Files to Create/Update**:
```bash
__tests__/
├── components/
│   ├── ProductCard.test.tsx
│   ├── SearchBar.test.tsx
│   └── Dashboard.test.tsx
├── services/
│   └── api.test.ts
└── utils/
    └── localStorage.test.ts
src/
├── components/
│   └── common/
│       └── ErrorBoundary.tsx   # Error boundaries
└── lib/
    └── errorHandling.ts        # Global error handling
```

**Testing Coverage**:
- Component rendering tests
- User interaction tests  
- localStorage functionality tests
- API integration tests
- Error scenario tests
- Accessibility tests

**Performance Optimizations**:
- Image optimization with Next.js Image
- Code splitting for routes
- Lazy loading for non-critical components
- Debounced search input
- Memoized expensive calculations
- Bundle analysis and optimization

**Acceptance Criteria**:
- [x] Test suite runs successfully
- [x] Core functionality covered by tests
- [x] Error boundaries catch and display errors gracefully
- [x] Performance meets targets (< 2s page load)
- [x] Accessibility compliance (basic WCAG)
- [x] Mobile responsiveness verified

---

## Code Style & Best Practices

### TypeScript Guidelines
```typescript
// Use strict typing
interface Product {
  id: string;
  name: string;
  price: number;
}

// Use const assertions for readonly data
const PRICE_THRESHOLDS = [100, 500, 1000] as const;

// Use proper error types
type ApiError = {
  message: string;
  code: number;
};
```

### Component Patterns
```typescript
// Use interface for props
interface ProductCardProps {
  product: ProductSummaryResponse;
  onSubscribe: (productId: string) => void;
}

// Use proper event handlers
const handleSubscribe = useCallback((productId: string) => {
  // Subscribe logic
}, []);

// Use proper loading states
if (isLoading) return <LoadingSpinner />;
if (error) return <ErrorMessage error={error} />;
```

### State Management
```typescript
// Use Zustand for simple state
import { create } from 'zustand';

interface SubscriptionStore {
  subscriptions: AnonymousSubscription[];
  addSubscription: (subscription: AnonymousSubscription) => void;
  removeSubscription: (productId: string) => void;
}

export const useSubscriptionStore = create<SubscriptionStore>((set) => ({
  subscriptions: [],
  addSubscription: (subscription) => 
    set((state) => ({ 
      subscriptions: [...state.subscriptions, subscription] 
    })),
  removeSubscription: (productId) =>
    set((state) => ({
      subscriptions: state.subscriptions.filter(s => s.productId !== productId)
    }))
}));
```

## Critical Implementation Notes

### 1. LocalStorage Management
- Always validate data before saving
- Implement expiry checks on read
- Handle storage quota exceeded
- Provide fallbacks for disabled localStorage

### 2. API Error Handling  
- Handle network failures gracefully
- Implement retry logic for failed requests
- Show user-friendly error messages
- Log errors for debugging

### 3. Performance Considerations
- **Manual search only** - No debounced search-as-you-type due to API performance
- **No autocomplete** - API response times make live suggestions impractical
- Implement virtual scrolling for large lists
- Use React.memo for expensive components
- Optimize images with Next.js Image component

### 4. Mobile-First Design
- Start with mobile layout
- Use responsive breakpoints (sm, md, lg, xl)
- Touch-friendly button sizes (min 44px)
- Test on actual devices

### 5. Accessibility
- Use semantic HTML elements
- Provide proper ARIA labels
- Ensure keyboard navigation works
- Maintain color contrast ratios

## Environment Variables
```bash
# .env.local (development)
NEXT_PUBLIC_API_URL=http://localhost:5147
NODE_ENV=development

# .env.production  
NEXT_PUBLIC_API_URL=https://api.kaspi-tracker.com
NODE_ENV=production
```

## Success Metrics for Each Iteration
- **Iteration 1**: Basic app structure loads without errors
- **Iteration 2**: Search functionality returns real data
- **Iteration 3**: Subscriptions persist in localStorage  
- **Iteration 4**: Dashboard displays subscription data
- **Iteration 5**: Price polling and notifications work
- **Iteration 6**: Product detail page fully functional
- **Iteration 7**: App works offline and installable as PWA
- **Iteration 8**: All tests pass, performance targets met

## Common Issues & Solutions

### API Integration Issues
- **CORS errors**: Backend needs proper CORS configuration
- **Rate limiting**: Implement client-side request throttling
- **Large responses**: Add pagination and response size limits

### LocalStorage Issues  
- **Quota exceeded**: Implement cleanup of expired subscriptions
- **Data corruption**: Add validation and error recovery
- **Cross-tab sync**: Use storage event listeners

### Performance Issues
- **API limitations**: Backend search API has ~3-4 second response times, making real-time features impractical
- **No live search**: Manual search only due to API performance constraints
- **Large bundle**: Use dynamic imports and code splitting
- **Memory leaks**: Clean up intervals and event listeners

Remember: Focus on **MVP features first**, keep iterations **short and focused**, and **test frequently** during development. Each iteration should result in working, deployable functionality.
