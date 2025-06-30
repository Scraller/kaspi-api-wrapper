# Product Requirements Document (PRD)
## Kaspi Price Tracker Frontend

---

## 1. Executive Summary

### 1.1 Product Vision
The Kaspi Price Tracker Frontend is a modern web application that empowers users to monitor product prices from kaspi.kz marketplace, receive intelligent price alerts, and make informed purchasing decisions. The platform provides price tracking, subscription management, and notification services in an intuitive, user-friendly interface.

### 1.2 Product Goals
- **Primary Goal**: Enable users to track product prices from kaspi.kz and receive timely alerts for price changes
- **Secondary Goals**: 
  - Reduce time spent manually checking prices
  - Help users save money by catching the best deals
  - Create a reliable, scalable platform for price monitoring
  - Provide simple analytics on price trends (Phase 2)

---

## 2. Product Overview

### 2.1 Target Audience
- **Primary**: Kazakhstan consumers who regularly shop on kaspi.kz
- **Secondary**: Price-conscious shoppers, deal hunters, and bargain seekers
- **Tertiary**: Small business owners tracking competitor pricing

### 2.2 User Personas

#### Persona 1: "Deal Hunter Aida" 
- **Demographics**: 25-35, urban professional, tech-savvy
- **Goals**: Find best deals, save money, stay updated on price drops
- **Pain Points**: Manual price checking is time-consuming, missing good deals
- **Behavior**: Checks prices frequently, uses multiple shopping platforms

#### Persona 2: "Busy Professional Arman"
- **Demographics**: 30-45, working parent, limited time for shopping
- **Goals**: Efficient shopping, automated deal finding
- **Pain Points**: No time for price comparison, wants automation
- **Behavior**: Prefers mobile, wants instant notifications

#### Persona 3: "Budget-Conscious Student Dana"
- **Demographics**: 18-25, student, price-sensitive
- **Goals**: Find cheapest options, track price history
- **Pain Points**: Limited budget, needs to find best value
- **Behavior**: Compares prices extensively, waits for discounts

---

## 3. Core Features & Requirements

### 3.1 MVP Features (Phase 1) - Anonymous Users

#### 3.1.1 Product Search & Discovery
**User Story**: "As a user, I want to search for products on kaspi.kz so that I can add them to my watchlist"

**Requirements**:
- Search bar with autocomplete functionality
- Product search results with basic information (name, image, current price)
- Integration with backend API endpoints
- Category filtering

**Acceptance Criteria**:
- Users can search for products by name or category
- Search results load within 3 seconds
- Results show product image, name, current price, and merchant

#### 3.1.2 Anonymous Subscription System
**User Story**: "As a user, I want to subscribe to product price alerts so that I'm notified when prices drop"

**Requirements**:
- "Add to Watchlist" button on product cards
- Browser localStorage for anonymous subscriptions
- Price threshold setting (alert when price drops below X)
- Browser notification options only (MVP)
- Session-based subscription management (24h expiry)

**Acceptance Criteria**:
- Users can subscribe to up to 10 products (anonymous limit)
- Users can set custom price thresholds
- Subscriptions persist for 24 hours in localStorage
- Users can easily remove products from watchlist

#### 3.1.3 Price Monitoring Dashboard
**User Story**: "As a user, I want to see all my subscribed products in one place so that I can monitor their prices"

**Requirements**:
- Personal dashboard with subscribed products (localStorage-based)
- Current price display with price change indicators
- Basic price change percentage
- "Quick Buy" links to kaspi.kz
- Sort options (by price change, date added)

**Acceptance Criteria**:
- Dashboard loads within 2 seconds
- Price data updates every 15 minutes (backend polling)
- Visual indicators for price increases/decreases
- Mobile-responsive design
- Works offline with cached data

#### 3.1.4 Browser Notification System
**User Story**: "As a user, I want to receive notifications when product prices change so that I don't miss deals"

**Requirements**:
- Browser push notifications only (MVP)
- Notification permission request
- Simple notification with product name and price change

**Acceptance Criteria**:
- Notifications delivered when user visits site (15min polling)
- Notifications include product name, old price, new price
- Users can enable/disable notifications
- Notifications work on desktop and mobile browsers

### 3.2 Phase 2 Features - Authenticated Users

#### 3.2.1 User Authentication
- Google OAuth via NextAuth.js
- Persistent user accounts and subscriptions
- Unlimited product subscriptions

#### 3.2.2 Advanced Notifications
- Telegram bot integration
- Email notifications
- Real-time WebSocket updates

#### 3.2.3 Price Analytics & Insights
- Price trend charts and historical data (30+ days)
- Price prediction algorithms
- Best time to buy recommendations

---

## 4. Technical Requirements

### 4.1 Frontend Architecture

#### 4.1.1 Technology Stack
- **Framework**: Next.js 14+ (React 18+)
- **Language**: TypeScript for type safety
- **Styling**: Tailwind CSS + shadcn/ui components
- **State Management**: Zustand (lightweight for MVP)
- **Data Fetching**: React Query (TanStack Query)
- **Storage**: Browser localStorage (MVP), Redis (Phase 2)
- **Authentication**: None (MVP), NextAuth.js (Phase 2)

#### 4.1.2 Project Structure (Inspired by Techinterview-space)
```
src/
├── app/                      # Next.js 13+ app directory
│   ├── dashboard/           # Dashboard routes
│   ├── search/              # Search pages
│   ├── layout.tsx           # Root layout
│   ├── page.tsx             # Home page
│   └── globals.css          # Global styles
├── components/              # Reusable UI components
│   ├── ui/                  # Base UI components (shadcn/ui)
│   ├── features/            # Feature-specific components
│   ├── layout/              # Layout components
│   └── common/              # Common shared components
├── hooks/                   # Custom React hooks
├── lib/                     # Utility functions and configurations
├── services/                # API services and data layer
├── stores/                  # Zustand stores
├── types/                   # TypeScript type definitions
└── utils/                   # Helper functions
```

#### 4.1.3 Component Architecture
```
components/
├── ui/                      # Base components
│   ├── button.tsx
│   ├── input.tsx
│   ├── card.tsx
│   ├── dialog.tsx
│   └── alert.tsx
├── features/                # Feature components
│   ├── product-search/
│   │   ├── SearchBar.tsx
│   │   ├── SearchResults.tsx
│   │   └── ProductCard.tsx
│   ├── dashboard/
│   │   ├── WatchlistGrid.tsx
│   │   ├── PriceIndicator.tsx
│   │   └── StatsCards.tsx
│   ├── notifications/
│   │   ├── NotificationPermission.tsx
│   │   └── NotificationToast.tsx
│   └── subscription/
│       ├── SubscribeButton.tsx
│       ├── PriceThresholdInput.tsx
│       └── WatchlistManager.tsx
├── layout/                  # Layout components
│   ├── Header.tsx
│   ├── Footer.tsx
│   └── Navigation.tsx
└── common/                  # Shared components
    ├── LoadingSpinner.tsx
    ├── ErrorBoundary.tsx
    ├── PriceDisplay.tsx
    └── ProductImage.tsx
```

### 4.2 Backend Integration

#### 4.2.1 API Integration
- **Base URL**: Configurable via environment variables
- **Authentication**: None for MVP (anonymous users)
- **Rate Limiting**: Client-side request throttling
- **Error Handling**: Comprehensive error handling and user feedback
- **Caching**: React Query for client-side caching

#### 4.2.2 Required API Endpoints (MVP)
```typescript
// Product Search
GET /api/products/search?q={query}&category={category}&page={page}

// Product Details
GET /api/products/{productId}

// Categories
GET /api/categories

// Phase 2 - User-specific endpoints
POST /api/subscriptions
GET /api/subscriptions
DELETE /api/subscriptions/{subscriptionId}
PUT /api/subscriptions/{subscriptionId}
```

### 4.3 Data Storage & Caching

#### 4.3.1 MVP Storage Strategy
- **Anonymous Subscriptions**: Browser localStorage (24h expiry)
- **API Cache**: React Query client-side caching
- **Notifications**: Browser Notification API
- **Session**: No server-side sessions needed

#### 4.3.2 Phase 2 - Redis Integration
- **User Sessions**: Authenticated user sessions
- **Persistent Subscriptions**: User subscription data
- **Real-time Cache**: WebSocket connection state management

#### 4.3.3 Data Models (MVP)
```typescript
interface Product {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  currentPrice: number;
  currency: string;
  merchantName: string;
  kaspiUrl: string;
  categoryId: string;
  lastUpdated: Date;
}

interface AnonymousSubscription {
  productId: string;
  priceThreshold?: number;
  subscribedAt: Date;
  expiresAt: Date; // 24h from creation
}

// Phase 2 Models
interface UserSubscription {
  id: string;
  productId: string;
  userId: string;
  priceThreshold?: number;
  alertTypes: AlertType[];
  isActive: boolean;
  createdAt: Date;
}
```

### 4.4 Performance Requirements

#### 4.4.1 Realistic Loading Performance
- **Initial Page Load**: < 2 seconds
- **Search Results**: < 3 seconds
- **Dashboard Load**: < 2 seconds
- **Price Updates**: 15-minute polling interval

#### 4.4.2 Optimization Strategies
- Code splitting and lazy loading
- Image optimization with Next.js Image component
- Service Worker for offline functionality
- Bundle size optimization (< 1MB initial bundle - realistic for feature set)
- Critical CSS inlining

### 4.5 Security Requirements

#### 4.5.1 Data Protection
- Input sanitization and validation
- XSS protection via React's built-in sanitization
- Content Security Policy (CSP)
- No sensitive data stored in localStorage

#### 4.5.2 Privacy Compliance
- User consent for browser notifications
- Clear data retention policies (24h for anonymous data)
- No cookies for MVP (localStorage only)

---

## 5. User Experience (UX) Requirements

### 5.1 Design Principles
- **Clean & Minimalist**: Focus on essential features without clutter
- **Mobile-First**: Responsive design optimized for mobile devices
- **Accessibility**: WCAG 2.1 AA compliance
- **Performance**: Fast, smooth interactions
- **Intuitive**: Clear navigation and user flow

### 5.2 User Interface (UI) Requirements

#### 5.2.1 Design System
- **Color Palette**: Primary (blue), secondary (green for price drops, red for increases)
- **Typography**: Inter font family for readability
- **Icons**: Lucide React icon library
- **Components**: shadcn/ui component library
- **Spacing**: 8px grid system

#### 5.2.2 Key UI Components
- **Search Bar**: Prominent, always visible, with autocomplete
- **Product Cards**: Image, name, price, merchant, subscribe button
- **Price Indicators**: Clear visual indication of price changes
- **Dashboard Grid**: Responsive grid layout for subscribed products
- **Notification Toast**: Non-intrusive browser notifications

### 5.3 User Flow

#### 5.3.1 MVP User Flow (Anonymous)
1. **Landing Page**: Value proposition, search bar, example products
2. **Search Results**: Product grid with subscription options
3. **Product Selection**: Click to subscribe, set price threshold
4. **Dashboard**: View subscribed products (localStorage), price updates
5. **Notifications**: Browser notifications for price changes

#### 5.3.2 Phase 2 User Flow (Authenticated)
1. **Registration/Login**: Google OAuth authentication
2. **Persistent Dashboard**: Unlimited subscriptions, cross-device sync
3. **Advanced Notifications**: Telegram/email integration
4. **Analytics**: Price history, trends, predictions

---

## 6. Infrastructure & DevOps

### 6.1 Development Environment

#### 6.1.1 Docker Configuration (MVP)
```yaml
# docker-compose.yml
version: '3.8'
services:
  frontend:
    build: .
    ports:
      - "3000:3000"
    environment:
      - NEXT_PUBLIC_API_URL=${API_URL}
      - NODE_ENV=development

  # Redis only needed for Phase 2
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    command: redis-server --appendonly yes
    profiles: ["phase2"]

volumes:
  redis_data:
```

#### 6.1.2 Environment Configuration
```bash
# .env.local
NEXT_PUBLIC_API_URL=http://localhost:5137
NODE_ENV=development

# Phase 2 variables
# NEXT_PUBLIC_WS_URL=ws://localhost:8080
# REDIS_URL=redis://localhost:6379
```

### 6.2 Deployment Strategy

#### 6.2.1 MVP Production Environment
- **Hosting**: Vercel (static deployment)
- **CDN**: Built-in Vercel CDN
- **Monitoring**: Vercel Analytics
- **No database needed** (localStorage only)

#### 6.2.2 Phase 2 Production Environment
- **Backend**: Docker containers
- **Database**: Redis Cloud
- **Monitoring**: Sentry for error tracking
- **Analytics**: PostHog or Google Analytics

#### 6.2.3 CI/CD Pipeline
1. **Code Push**: GitHub repository
2. **Testing**: Automated tests (unit, e2e)
3. **Build**: Next.js static export (MVP)
4. **Deploy**: Automatic deployment to Vercel
5. **Monitoring**: Performance and error tracking

---

## 7. Testing Strategy

### 7.1 Testing Levels

#### 7.1.1 Unit Testing
- **Framework**: Jest + React Testing Library
- **Components**: All UI components with localStorage interactions
- **Utilities**: Helper functions and custom hooks

#### 7.1.2 End-to-End Testing
- **Framework**: Playwright
- **User Flows**: Critical user journeys
- **Cross-browser**: Chrome, Firefox, Safari
- **Mobile Testing**: Responsive design validation

### 7.2 Testing Scenarios

#### 7.2.1 Critical Test Cases (MVP)
1. **Search Functionality**: Search for products, view results
2. **Anonymous Subscription**: Subscribe to product, localStorage persistence
3. **Dashboard Navigation**: View subscribed products, price updates
4. **Browser Notifications**: Permission request, notification display
5. **Responsive Design**: Mobile and desktop compatibility
6. **Offline Functionality**: App works with cached data

### 7.3 MVP Limitations & Phase 2 Migration

#### 7.3.1 MVP Constraints
- **Anonymous Users**: No persistent accounts
- **24h Expiry**: Subscriptions auto-expire
- **10 Product Limit**: Prevent localStorage abuse
- **Browser Notifications Only**: No external integrations
- **Basic Price Display**: No historical charts

#### 7.3.2 Phase 2 Migration Path
- Seamless upgrade from localStorage to authenticated accounts
- Data migration tools for existing anonymous subscriptions
- Progressive feature rollout (notifications → analytics → predictions)

---

**Document Version**: 2.0  
**Last Updated**: June 30, 2025  
**Next Review**: July 15, 2025  
**Document Owner**: Maxim Stryuk  
**Changes**: Resolved contradictions, clarified MVP vs Phase 2, realistic performance targets
