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