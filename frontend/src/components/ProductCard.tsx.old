import React from 'react';

interface ProductCardProps {
    productName: string;
    lowestPrice: number;
    merchant: string;
    onSubscribe: () => void;
}

const ProductCard: React.FC<ProductCardProps> = ({ productName, lowestPrice, merchant, onSubscribe }) => {
    return (
        <div className="border rounded-lg p-4 shadow-md">
            <h2 className="text-xl font-bold">{productName}</h2>
            <p className="text-lg text-green-600">Lowest Price: ${lowestPrice}</p>
            <p className="text-sm text-gray-500">Merchant: {merchant}</p>
            <button 
                className="mt-2 bg-blue-500 text-white py-1 px-4 rounded" 
                onClick={onSubscribe}
            >
                Subscribe for Alerts
            </button>
        </div>
    );
};

export default ProductCard;