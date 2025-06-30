import React from 'react';
import ProductSearch from '../components/ProductSearch';
import Dashboard from '../components/Dashboard';

const Page = () => {
    return (
        <div className="container mx-auto p-4">
            <h1 className="text-2xl font-bold mb-4">Kaspi Price Tracker</h1>
            <ProductSearch />
            <Dashboard />
        </div>
    );
};

export default Page;