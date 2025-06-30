// API Response Models
export interface ProductSearchResponse {
  products: ProductSummaryResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  searchQuery: string;
  category?: string;
}

export interface ProductSummaryResponse {
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

export interface ProductDetailResponse {
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

export interface ProductOffer {
  id: string;
  price: number;
  currency: string;
  merchantName: string;
  merchantId: string;
  availability: "in_stock" | "out_of_stock";
  deliveryOptions: string[];
}

export interface SpecificationGroup {
  name: string;
  specifications: Specification[];
}

export interface Specification {
  name: string;
  value: string;
}

export interface HierarchicalCategoryInfo {
  id: string;
  name: string;
  slug: string;
  parentCategoryId?: string;
  subcategories: HierarchicalCategoryInfo[];
  productCount: number;
}

// Search Request Models
export interface SearchRequest {
  text: string;
  category?: string;
  page?: number;
  pageSize?: number;
  sortOptions?: string[];
}

// Legacy interface for compatibility
export interface Product {
    id: string;
    name: string;
    currentPrice: number;
    lowestPrice: number;
    merchant: string;
    imageUrl: string;
    url: string;
}

export interface PriceAlert {
    id: string;
    productId: string;
    userId: string;
    thresholdPrice: number;
    createdAt: Date;
    updatedAt: Date;
}

export interface User {
    id: string;
    email: string;
    subscribedProducts: string[];
}

export interface ApiResponse<T> {
    success: boolean;
    data: T;
    message?: string;
}