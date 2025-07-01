'use client';

import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Bell, BellOff, Loader2, Settings } from 'lucide-react';
import { useSubscriptionStore } from '@/stores/subscriptionStore';
import { ProductSummaryResponse } from '@/types';
import { getProductDetails, getMerchantDetails } from '@/services/api';
import { PriceThresholdInput } from './PriceThresholdInput';
import { toast } from 'sonner';

interface SubscribeButtonProps {
  product: ProductSummaryResponse;
  priceThreshold?: number;
  className?: string;
  size?: 'sm' | 'default' | 'lg';
  variant?: 'default' | 'outline' | 'secondary';
  showText?: boolean;
  showThresholdDialog?: boolean; // New prop to enable threshold setting
}

/**
 * Subscribe button component for adding/removing products from watchlist
 * Handles anonymous localStorage-based subscriptions with 24h expiry
 */
export function SubscribeButton({
  product,
  priceThreshold,
  className,
  size = 'default',
  variant = 'default',
  showText = true,
  showThresholdDialog = true, // Enable by default
}: SubscribeButtonProps) {
  const {
    addSubscription,
    removeSubscription,
    isSubscribed,
    isLoading,
    stats,
  } = useSubscriptionStore();

  const [isProcessing, setIsProcessing] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [tempThreshold, setTempThreshold] = useState<number | undefined>(priceThreshold);
  const subscribed = isSubscribed(product.id);

  /**
   * Handle subscription with threshold setting
   * Fetches complete product and merchant details before subscribing
   */
  const handleSubscriptionWithThreshold = async (finalThreshold?: number) => {
    setIsProcessing(true);

    try {
      // Check if we have remaining slots
      if (stats.remaining <= 0) {
        toast.error(`Maximum ${stats.limit} subscriptions allowed for anonymous users`);
        return;
      }

      console.log(`🔍 Fetching complete product details for subscription: ${product.id}`);
      
      // Fetch complete product details to get the best offer and merchant info
      const productDetails = await getProductDetails(product.id);
      
      // Find the offer with the lowest price (best deal)
      if (!productDetails.offers || productDetails.offers.length === 0) {
        toast.error('No offers available for this product');
        return;
      }
      
      const bestOffer = productDetails.offers.reduce((best, current) => 
        current.price < best.price ? current : best
      );

      // Fetch merchant details for phone and contact info
      let merchantInfo = {
        name: bestOffer.merchantName,
        id: bestOffer.id, // Use id instead of merchantId
        phone: undefined as string | undefined,
        url: undefined as string | undefined
      };

      try {
        console.log(`📞 Fetching merchant details for: ${bestOffer.id}`);
        const merchantDetails = await getMerchantDetails(bestOffer.id);
        
        merchantInfo = {
          name: merchantDetails.name,
          id: merchantDetails.id,
          phone: merchantDetails.contactInfo?.phone,
          url: `https://kaspi.kz/shop/info/merchant/${merchantDetails.id}/address-tab/`
        };
        
        console.log(`✅ Merchant info for subscription: ${merchantInfo.name} - Phone: ${merchantInfo.phone || 'N/A'}`);
      } catch (merchantError) {
        console.warn(`⚠️ Failed to fetch merchant details for ${bestOffer.id}:`, merchantError);
        // Continue with basic merchant info from product offer
      }

      // Add subscription with complete merchant information
      const result = await addSubscription(
        product.id,
        product.name,
        bestOffer.price, // Use the actual best offer price, not search result price
        {
          productImage: product.imageUrl,
          priceThreshold: finalThreshold,
          availability: bestOffer.availability,
          merchantName: merchantInfo.name,
          merchantId: merchantInfo.id,
          merchantPhone: merchantInfo.phone,
          merchantUrl: merchantInfo.url,
          kaspiUrl: productDetails.kaspiUrl,
        }
      );

      if (result.success) {
        toast.success("Added to watchlist! You'll get notified of price changes.");
        setDialogOpen(false);
      } else {
        toast.error(result.message);
      }
    } catch (error) {
      console.error('Error adding subscription:', error);
      toast.error('Failed to add to watchlist. Please try again.');
    } finally {
      setIsProcessing(false);
    }
  };

  /**
   * Handle subscription toggle - add or remove from watchlist
   */
  const handleSubscriptionToggle = async () => {
    if (subscribed) {
      // Remove subscription
      setIsProcessing(true);
      try {
        const success = await removeSubscription(product.id);
        if (success) {
          toast.success('Removed from watchlist');
        } else {
          toast.error('Failed to remove from watchlist');
        }
      } catch (error) {
        console.error('Error removing subscription:', error);
        toast.error('Something went wrong. Please try again.');
      } finally {
        setIsProcessing(false);
      }
    } else {
      // For new subscriptions, show threshold dialog if enabled
      if (showThresholdDialog) {
        setTempThreshold(priceThreshold);
        setDialogOpen(true);
      } else {
        // Direct subscription without threshold dialog
        await handleSubscriptionWithThreshold(priceThreshold);
      }
    }
  };

  const isButtonLoading = isLoading || isProcessing;

  // For subscribed items, just show the toggle button
  if (subscribed) {
    return (
      <Button
        onClick={handleSubscriptionToggle}
        disabled={isButtonLoading}
        size={size}
        variant="outline"
        className={className}
      >
        {isButtonLoading ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <>
            <BellOff className="h-4 w-4" />
            {showText && <span className="ml-2">Unsubscribe</span>}
          </>
        )}
      </Button>
    );
  }

  // For non-subscribed items, show the dialog version
  return (
    <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
      <DialogTrigger asChild>
        <Button
          onClick={!showThresholdDialog ? handleSubscriptionToggle : undefined}
          disabled={isButtonLoading}
          size={size}
          variant={variant}
          className={className}
        >
          {isButtonLoading ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <>
              <Bell className="h-4 w-4" />
              {showText && <span className="ml-2">Watch Price</span>}
            </>
          )}
        </Button>
      </DialogTrigger>

      {showThresholdDialog && (
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Set Price Alert</DialogTitle>
          </DialogHeader>
          
          <div className="space-y-4">
            <div>
              <h4 className="font-medium text-sm mb-2">{product.name}</h4>
              <p className="text-sm text-gray-600">
                Current price: {product.price.toLocaleString()} KZT
              </p>
            </div>

            <PriceThresholdInput
              currentPrice={product.price}
              currency="KZT"
              defaultThreshold={tempThreshold}
              onThresholdSet={setTempThreshold}
              className="w-full"
            />

            <div className="flex gap-2 justify-end">
              <Button 
                variant="outline" 
                onClick={() => setDialogOpen(false)}
                disabled={isProcessing}
              >
                Cancel
              </Button>
              <Button 
                onClick={() => handleSubscriptionWithThreshold(tempThreshold)}
                disabled={isProcessing}
              >
                {isProcessing ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    Adding...
                  </>
                ) : (
                  'Add to Watchlist'
                )}
              </Button>
            </div>
          </div>
        </DialogContent>
      )}
    </Dialog>
  );
}
