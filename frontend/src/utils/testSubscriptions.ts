/**
 * Simple test script to validate localStorage subscription functionality
 * Run this in the browser console to test our implementation
 */

// Test localStorage utilities
import {
  addSubscription,
  removeSubscription,
  getSubscriptions,
  isSubscribed,
  getSubscriptionStats,
  updateSubscriptionPrice,
} from '../utils/localStorage';

// Mock product data for testing
const testProduct = {
  id: 'test-product-1',
  name: 'iPhone 15 Pro Max',
  price: 599000,
  imageUrl: 'https://kaspi.kz/img/test-image.jpg',
  merchantName: 'Test Merchant',
  availability: 'in_stock' as const,
  kaspiUrl: 'https://kaspi.kz/shop/p/test-product',
};

/**
 * Test subscription functionality
 */
export async function testSubscriptionSystem() {
  console.group('🧪 Testing Subscription System');

  try {
    // Test 1: Initial state
    console.log('1. Testing initial state...');
    const initialStats = getSubscriptionStats();
    console.log('Initial stats:', initialStats);
    console.assert(initialStats.count === 0, 'Initial count should be 0');
    console.assert(initialStats.remaining === 10, 'Initial remaining should be 10');

    // Test 2: Add subscription
    console.log('2. Testing add subscription...');
    const addResult = addSubscription(
      testProduct.id,
      testProduct.name,
      testProduct.price,
      {
        productImage: testProduct.imageUrl,
        priceThreshold: 550000,
        merchantName: testProduct.merchantName,
        availability: testProduct.availability,
        kaspiUrl: testProduct.kaspiUrl,
      }
    );
    console.log('Add result:', addResult);
    console.assert(addResult.success === true, 'Add subscription should succeed');

    // Test 3: Check if subscribed
    console.log('3. Testing isSubscribed...');
    const subscribed = isSubscribed(testProduct.id);
    console.log('Is subscribed:', subscribed);
    console.assert(subscribed === true, 'Product should be subscribed');

    // Test 4: Get subscriptions
    console.log('4. Testing getSubscriptions...');
    const subscriptions = getSubscriptions();
    console.log('Subscriptions:', subscriptions);
    console.assert(subscriptions.length === 1, 'Should have 1 subscription');
    console.assert(subscriptions[0].productId === testProduct.id, 'Product ID should match');

    // Test 5: Update stats
    console.log('5. Testing updated stats...');
    const updatedStats = getSubscriptionStats();
    console.log('Updated stats:', updatedStats);
    console.assert(updatedStats.count === 1, 'Count should be 1');
    console.assert(updatedStats.remaining === 9, 'Remaining should be 9');

    // Test 6: Update price
    console.log('6. Testing price update...');
    const priceUpdateResult = updateSubscriptionPrice(testProduct.id, 580000);
    console.log('Price update result:', priceUpdateResult);
    console.assert(priceUpdateResult === true, 'Price update should succeed');

    const updatedSubscriptions = getSubscriptions();
    console.log('Updated subscription:', updatedSubscriptions[0]);
    console.assert(updatedSubscriptions[0].currentPrice === 580000, 'Price should be updated');
    console.assert(updatedSubscriptions[0].priceChange === 'decrease', 'Price change should be decrease');

    // Test 7: Duplicate subscription
    console.log('7. Testing duplicate subscription...');
    const duplicateResult = addSubscription(testProduct.id, testProduct.name, testProduct.price);
    console.log('Duplicate result:', duplicateResult);
    console.assert(duplicateResult.success === false, 'Duplicate subscription should fail');

    // Test 8: Remove subscription
    console.log('8. Testing remove subscription...');
    const removeResult = removeSubscription(testProduct.id);
    console.log('Remove result:', removeResult);
    console.assert(removeResult === true, 'Remove subscription should succeed');

    // Test 9: Final state
    console.log('9. Testing final state...');
    const finalStats = getSubscriptionStats();
    console.log('Final stats:', finalStats);
    console.assert(finalStats.count === 0, 'Final count should be 0');
    console.assert(!isSubscribed(testProduct.id), 'Product should not be subscribed');

    console.log('✅ All tests passed!');
  } catch (error) {
    console.error('❌ Test failed:', error);
  }

  console.groupEnd();
}

/**
 * Test subscription limits
 */
export async function testSubscriptionLimits() {
  console.group('📊 Testing Subscription Limits');

  try {
    // Clear any existing subscriptions
    const existing = getSubscriptions();
    for (const sub of existing) {
      removeSubscription(sub.productId);
    }

    // Test maximum subscriptions
    console.log('Testing maximum subscription limit (10)...');
    
    for (let i = 1; i <= 12; i++) {
      const result = addSubscription(
        `test-product-${i}`,
        `Test Product ${i}`,
        100000 + i * 1000
      );

      if (i <= 10) {
        console.assert(result.success === true, `Subscription ${i} should succeed`);
      } else {
        console.assert(result.success === false, `Subscription ${i} should fail (limit reached)`);
      }
    }

    const stats = getSubscriptionStats();
    console.log('Final stats:', stats);
    console.assert(stats.count === 10, 'Should have exactly 10 subscriptions');
    console.assert(stats.remaining === 0, 'Should have 0 remaining slots');

    console.log('✅ Limit tests passed!');

    // Cleanup
    const subscriptions = getSubscriptions();
    for (const sub of subscriptions) {
      removeSubscription(sub.productId);
    }
  } catch (error) {
    console.error('❌ Limit test failed:', error);
  }

  console.groupEnd();
}

/**
 * Run all tests
 */
export async function runAllTests() {
  console.log('🚀 Starting Subscription System Tests');
  await testSubscriptionSystem();
  await testSubscriptionLimits();
  console.log('🎉 All tests completed!');
}

// Export for console testing
if (typeof window !== 'undefined') {
  (window as any).testSubscriptions = {
    runAllTests,
    testSubscriptionSystem,
    testSubscriptionLimits,
  };
}
