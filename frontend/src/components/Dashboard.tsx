import React from 'react';
import { useProducts } from '../hooks/useProducts';
import ProductCard from './ProductCard';
import PriceAlert from './PriceAlert';

const Dashboard: React.FC = () => {
    const { products, loading, error } = useProducts();

    if (loading) {
        return <div>Loading...</div>;
    }

    if (error) {
        return <div>Error loading products: {error.message}</div>;
    }

    return (
        <div className="dashboard">
            <h1>Your Subscribed Products</h1>
            <div className="product-list">
                {products.length === 0 ? (
                    <p>No subscribed products found.</p>
                ) : (
                    products.map(product => (
                        <ProductCard key={product.id} product={product} />
                    ))
                )}
            </div>
            <PriceAlert />
        </div>
    );
};

export default Dashboard;