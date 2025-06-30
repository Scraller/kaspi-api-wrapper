import React, { useState } from 'react';
import { useProducts } from '../hooks/useProducts';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Alert } from './ui/alert';

const PriceAlert = () => {
    const { subscribeToPriceAlert } = useProducts();
    const [productId, setProductId] = useState('');
    const [alertMessage, setAlertMessage] = useState('');
    const [errorMessage, setErrorMessage] = useState('');

    const handleSubscribe = async () => {
        try {
            await subscribeToPriceAlert(productId);
            setAlertMessage('Successfully subscribed to price alerts!');
            setErrorMessage('');
        } catch (error) {
            setErrorMessage('Failed to subscribe. Please try again.');
            setAlertMessage('');
        }
    };

    return (
        <div className="price-alert">
            <h2>Subscribe to Price Alerts</h2>
            <Input 
                type="text" 
                placeholder="Enter Product ID" 
                value={productId} 
                onChange={(e) => setProductId(e.target.value)} 
            />
            <Button onClick={handleSubscribe}>Subscribe</Button>
            {alertMessage && <Alert type="success">{alertMessage}</Alert>}
            {errorMessage && <Alert type="error">{errorMessage}</Alert>}
        </div>
    );
};

export default PriceAlert;