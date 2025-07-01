'use client';

import React from 'react';
import { ExternalLink, MessageCircle, Phone } from 'lucide-react';
import { Button } from '@/components/ui/button';
import Image from 'next/image';

interface MerchantInfoProps {
  merchantName?: string;
  merchantId?: string;
  merchantPhone?: string;
  merchantUrl?: string;
  size?: 'sm' | 'md' | 'lg';
  showPhoneIcon?: boolean;
  showLinkIcon?: boolean;
  className?: string;
}

/**
 * MerchantInfo component displays merchant details with clickable links
 * Includes merchant name with Kaspi link and WhatsApp phone contact
 */
export function MerchantInfo({
  merchantName,
  merchantId,
  merchantPhone,
  merchantUrl,
  size = 'md',
  showPhoneIcon = true,
  showLinkIcon = true,
  className = ""
}: MerchantInfoProps) {
  
  // Format phone number for WhatsApp (remove non-digits and ensure proper format)
  const formatPhoneForWhatsApp = (phone: string): string => {
    // Remove all non-digit characters
    const cleaned = phone.replace(/\D/g, '');
    
    // If it starts with 8, replace with 7 (Russia/Kazakhstan format)
    if (cleaned.startsWith('8')) {
      return '7' + cleaned.substring(1);
    }
    
    // If it starts with +7, remove the +
    if (cleaned.startsWith('7')) {
      return cleaned;
    }
    
    // If no country code, assume Kazakhstan (+7)
    return '7' + cleaned;
  };

  const getMerchantKaspiUrl = (): string => {
    // If we have merchantId, always construct the proper Kaspi merchant URL with address tab
    if (merchantId) {
      return `https://kaspi.kz/shop/info/merchant/${merchantId}/address-tab/`;
    }
    
    // If we have a merchant URL from backend, ensure it has address-tab
    if (merchantUrl) {
      // Check if URL already has address-tab, if not add it
      if (merchantUrl.includes('/shop/info/merchant/') && !merchantUrl.includes('/address-tab')) {
        // Add address-tab to the URL
        const urlWithoutTrailing = merchantUrl.replace(/\/$/, ''); // Remove trailing slash
        return `${urlWithoutTrailing}/address-tab/`;
      }
      return merchantUrl;
    }
    
    // Last resort: search for merchant by name (not ideal)
    if (merchantName) {
      return `https://kaspi.kz/shop/search/?text=${encodeURIComponent(merchantName)}`;
    }
    
    return '#';
  };

  const getWhatsAppUrl = (): string => {
    if (!merchantPhone) return '';
    const formattedPhone = formatPhoneForWhatsApp(merchantPhone);
    return `https://wa.me/${formattedPhone}`;
  };

  const sizeClasses = {
    sm: 'text-xs',
    md: 'text-sm',
    lg: 'text-base'
  };

  const iconSizes = {
    sm: 'h-3 w-3',
    md: 'h-4 w-4',
    lg: 'h-5 w-5'
  };

  if (!merchantName) {
    return (
      <span className={`text-gray-500 dark:text-gray-500 ${sizeClasses[size]} ${className}`}>
        Unknown merchant
      </span>
    );
  }

  return (
    <div className={`flex items-center gap-2 flex-wrap ${className}`}>
      {/* Merchant Name with Kaspi Link */}
      <Button
        variant="link"
        size="sm"
        onClick={() => window.open(getMerchantKaspiUrl(), '_blank', 'noopener,noreferrer')}
        className={`p-0 h-auto font-medium text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 ${sizeClasses[size]}`}
      >
        <span className="truncate max-w-32">{merchantName}</span>
        {showLinkIcon && (
          <ExternalLink className={`${iconSizes[size]} ml-1 flex-shrink-0`} />
        )}
      </Button>

      {/* WhatsApp Phone Contact - Links to WhatsApp */}
      {merchantPhone && (
        <div className="flex items-center gap-1">
          <span className={`text-gray-400 dark:text-gray-500 ${sizeClasses[size]}`}>•</span>
          <Button
            variant="link"
            size="sm"
            onClick={() => window.open(getWhatsAppUrl(), '_blank', 'noopener,noreferrer')}
            className={`p-0 h-auto font-medium hover:opacity-80 flex items-center gap-1`}
            title={`Contact via WhatsApp: ${merchantPhone}`}
          >
            <Image
              src="/whatsapp-icon.svg"
              alt="WhatsApp"
              width={size === 'sm' ? 14 : size === 'md' ? 16 : 20}
              height={size === 'sm' ? 14 : size === 'md' ? 16 : 20}
              className="flex-shrink-0"
            />
          </Button>
        </div>
      )}
    </div>
  );
}

/**
 * Simplified version for inline display
 */
export function MerchantInfoInline({
  merchantName,
  merchantId,
  merchantPhone,
  merchantUrl,
  className = ""
}: Pick<MerchantInfoProps, 'merchantName' | 'merchantId' | 'merchantPhone' | 'merchantUrl' | 'className'>) {
  return (
    <MerchantInfo
      merchantName={merchantName}
      merchantId={merchantId}
      merchantPhone={merchantPhone}
      merchantUrl={merchantUrl}
      size="sm"
      showPhoneIcon={false}
      showLinkIcon={false}
      className={className}
    />
  );
}
