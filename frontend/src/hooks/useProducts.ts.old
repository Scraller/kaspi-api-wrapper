import { useEffect, useState } from 'react';
import { fetchProducts, subscribeToProduct, unsubscribeFromProduct } from '../lib/api';
import { Product } from '../types';

const useProducts = () => {
    const [products, setProducts] = useState<Product[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const loadProducts = async () => {
            try {
                const fetchedProducts = await fetchProducts();
                setProducts(fetchedProducts);
            } catch (err) {
                setError('Failed to fetch products');
            } finally {
                setLoading(false);
            }
        };

        loadProducts();
    }, []);

    const subscribe = async (productId: string) => {
        try {
            await subscribeToProduct(productId);
            // Optionally update local state or notify user
        } catch (err) {
            setError('Failed to subscribe to product');
        }
    };

    const unsubscribe = async (productId: string) => {
        try {
            await unsubscribeFromProduct(productId);
            // Optionally update local state or notify user
        } catch (err) {
            setError('Failed to unsubscribe from product');
        }
    };

    return {
        products,
        loading,
        error,
        subscribe,
        unsubscribe,
    };
};

export default useProducts;