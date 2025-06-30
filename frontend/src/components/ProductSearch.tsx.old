import React, { useState } from 'react';
import { Input } from './ui/input';
import { Button } from './ui/button';
import { PriceAlert } from './PriceAlert';
import { useProducts } from '../hooks/useProducts';

const ProductSearch: React.FC = () => {
    const [productName, setProductName] = useState('');
    const { subscribeToProduct } = useProducts();

    const handleSearch = () => {
        if (productName) {
            subscribeToProduct(productName);
            setProductName('');
        }
    };

    return (
        <div className="flex flex-col items-center">
            <h2 className="text-xl font-bold mb-4">Subscribe to Product Price Alerts</h2>
            <Input
                value={productName}
                onChange={(e) => setProductName(e.target.value)}
                placeholder="Enter product name"
            />
            <Button onClick={handleSearch} className="mt-2">
                Subscribe
            </Button>
            <PriceAlert />
        </div>
    );
};

export default ProductSearch;