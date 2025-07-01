'use client';

import React from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { PriceHistoryChart } from './PriceHistoryChart';
import { AnonymousSubscription } from '@/types/subscription';
import { X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { MerchantInfo } from '../merchant/MerchantInfo';
import Image from 'next/image';

interface PriceHistoryModalProps {
  isOpen: boolean;
  onClose: () => void;
  subscription: AnonymousSubscription | null;
}

/**
 * Modal component for displaying price history chart
 * Opens when user clicks on a price offer to see detailed price trends
 */
export function PriceHistoryModal({ isOpen, onClose, subscription }: PriceHistoryModalProps) {
  if (!subscription) return null;

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-hidden">
        <DialogHeader>
          <div className="flex items-start justify-between">
            <div className="flex-1 min-w-0">
              <DialogTitle className="text-xl font-semibold text-gray-900 dark:text-gray-100 pr-4">
                Price History: {subscription.productName}
              </DialogTitle>
              <DialogDescription className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                View price changes and trends over time for this product
              </DialogDescription>
            </div>
            <Button 
              variant="ghost" 
              size="sm" 
              onClick={onClose}
              className="h-6 w-6 p-0 hover:bg-gray-100 dark:hover:bg-gray-800"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        </DialogHeader>

        <div className="mt-4 overflow-y-auto">
          {/* Product Summary */}
          <div className="bg-gray-50 dark:bg-gray-900 rounded-lg p-4 mb-6">
            <div className="flex items-start gap-4">
              {subscription.productImage && (
                <div className="relative w-16 h-16 flex-shrink-0">
                  <Image
                    src={subscription.productImage}
                    alt={subscription.productName}
                    fill
                    className="object-cover rounded-lg border border-gray-200 dark:border-gray-700"
                    sizes="64px"
                  />
                </div>
              )}
              <div className="flex-1 min-w-0">
                <h3 className="font-medium text-gray-900 dark:text-gray-100 mb-1">
                  {subscription.productName}
                </h3>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-gray-500 dark:text-gray-500">Current Price:</span>
                    <div className="font-semibold text-lg text-gray-900 dark:text-gray-100">
                      {new Intl.NumberFormat('kk-KZ', {
                        style: 'currency',
                        currency: 'KZT',
                        minimumFractionDigits: 0,
                      }).format(subscription.currentPrice)}
                    </div>
                  </div>
                  <div>
                    <span className="text-gray-500 dark:text-gray-500">Price Alert:</span>
                    <div className="font-medium text-gray-900 dark:text-gray-100">
                      {subscription.priceThreshold ? 
                        new Intl.NumberFormat('kk-KZ', {
                          style: 'currency',
                          currency: 'KZT',
                          minimumFractionDigits: 0,
                        }).format(subscription.priceThreshold) :
                        'Not set'
                      }
                    </div>
                  </div>
                </div>
                {subscription.merchantName && (
                  <div className="mt-2">
                    <span className="text-gray-500 dark:text-gray-500 text-sm">Merchant: </span>
                    <MerchantInfo
                      merchantName={subscription.merchantName}
                      merchantId={subscription.merchantId}
                      merchantPhone={subscription.merchantPhone}
                      merchantUrl={subscription.merchantUrl}
                      size="sm"
                    />
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Price History Chart */}
          <PriceHistoryChart 
            priceHistory={subscription.priceHistory || []}
            productName={subscription.productName}
            currentPrice={subscription.currentPrice}
            className="mb-4"
          />

          {/* Price Statistics */}
          {subscription.priceHistory && subscription.priceHistory.length > 1 && (
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4">
              <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-3">Price Statistics</h4>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                {(() => {
                  const prices = subscription.priceHistory!.map(h => h.price);
                  const minPrice = Math.min(...prices);
                  const maxPrice = Math.max(...prices);
                  const firstPrice = prices[0];
                  const lastPrice = prices[prices.length - 1];
                  const changeAmount = lastPrice - firstPrice;
                  const changePercent = ((changeAmount / firstPrice) * 100);

                  return (
                    <>
                      <div>
                        <div className="text-gray-500 dark:text-gray-500 mb-1">Lowest Price</div>
                        <div className="font-semibold text-green-600 dark:text-green-400">
                          {new Intl.NumberFormat('kk-KZ', {
                            style: 'currency',
                            currency: 'KZT',
                            minimumFractionDigits: 0,
                          }).format(minPrice)}
                        </div>
                      </div>
                      <div>
                        <div className="text-gray-500 dark:text-gray-500 mb-1">Highest Price</div>
                        <div className="font-semibold text-red-600 dark:text-red-400">
                          {new Intl.NumberFormat('kk-KZ', {
                            style: 'currency',
                            currency: 'KZT',
                            minimumFractionDigits: 0,
                          }).format(maxPrice)}
                        </div>
                      </div>
                      <div>
                        <div className="text-gray-500 dark:text-gray-500 mb-1">Total Change</div>
                        <div className={`font-semibold ${
                          changeAmount < 0 ? 'text-green-600 dark:text-green-400' : 
                          changeAmount > 0 ? 'text-red-600 dark:text-red-400' : 
                          'text-gray-600 dark:text-gray-400'
                        }`}>
                          {changeAmount < 0 ? '-' : changeAmount > 0 ? '+' : ''}
                          {new Intl.NumberFormat('kk-KZ', {
                            style: 'currency',
                            currency: 'KZT',
                            minimumFractionDigits: 0,
                          }).format(Math.abs(changeAmount))}
                        </div>
                      </div>
                      <div>
                        <div className="text-gray-500 dark:text-gray-500 mb-1">% Change</div>
                        <div className={`font-semibold ${
                          changePercent < 0 ? 'text-green-600 dark:text-green-400' : 
                          changePercent > 0 ? 'text-red-600 dark:text-red-400' : 
                          'text-gray-600 dark:text-gray-400'
                        }`}>
                          {changePercent > 0 ? '+' : ''}{changePercent.toFixed(1)}%
                        </div>
                      </div>
                    </>
                  );
                })()}
              </div>
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex justify-between items-center pt-4 border-t border-gray-200 dark:border-gray-700 mt-6">
            <div className="text-sm text-gray-500 dark:text-gray-500">
              {subscription.priceHistory?.length || 0} price points recorded
            </div>
            <div className="flex gap-3">
              <Button variant="outline" onClick={onClose}>
                Close
              </Button>
              {subscription.kaspiUrl && (
                <Button 
                  onClick={() => window.open(subscription.kaspiUrl, '_blank')}
                  className="bg-orange-600 hover:bg-orange-700 text-white"
                >
                  View on Kaspi.kz
                </Button>
              )}
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
