// AdInMoIapProxy.cs - AdInMo IAPBoost Unity Package
// Unity-style extension method for seamless AdInMo IAPBoost integration
// Usage: UnityPurchasing.InitializeWithAdInMo(this, builder);

// ---- Dependency guards ----
#if !UNITY_PURCHASING
#error AdInMo IAP Proxy requires Unity IAP (com.unity.purchasing). Install the package and ensure the UNITY_PURCHASING define is set.
#endif
#if !ADINMO_PRESENT
#error AdInMo IAP Proxy requires the AdInMo SDK. Import the SDK and define ADINMO_PRESENT in Project Settings → Player → Scripting Define Symbols.
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using Adinmo; // from AdInMo SDK

namespace AdInMo.IapProxy
{
    /// <summary>
    /// Extension methods for Unity Purchasing to enable AdInMo IAPBoost integration
    /// </summary>
    public static class UnityPurchasingAdInMoExtensions
    {
        /// <summary>
        /// Initialize Unity Purchasing with AdInMo IAPBoost support in a single call
        /// IMPORTANT: Call this only ONCE per application lifecycle, preferably from a persistent scene
        /// </summary>
        /// <param name="listener">Your IStoreListener implementation</param>
        /// <param name="builder">Unity IAP ConfigurationBuilder</param>
        public static void InitializeWithAdInMo(IStoreListener listener, ConfigurationBuilder builder)
        {
            if (listener == null)
            {
                LogError("[AdInMo] Listener cannot be null");
                return;
            }
            
            if (builder == null)
            {
                LogError("[AdInMo] Builder cannot be null");
                return;
            }

            lock (AdInMoIapProxy._initLock)
            {
                Log("[AdInMo] Initializing Unity Purchasing with AdInMo IAPBoost support...");
                
                // Check if Unity IAP is already initialized or in progress
                if (AdInMoIapProxy._iapInitialized)
                {
                    LogWarning("[AdInMo] Unity IAP already initialized, skipping duplicate initialization");
                    
                    // If already initialized, notify the listener immediately
                    if (AdInMoIapProxy._controller != null)
                    {
                        Log("[AdInMo] Notifying listener with existing controller");
                        listener.OnInitialized(AdInMoIapProxy._controller, null);
                    }
                    return;
                }
                
                if (AdInMoIapProxy._iapInitializing)
                {
                    LogWarning("[AdInMo] Unity IAP initialization already in progress, skipping duplicate call");
                    return;
                }

                // Mark as initializing to prevent concurrent calls
                AdInMoIapProxy._iapInitializing = true;
                
                // Register AdInMo callbacks (protected against re-registration)
                AdInMoIapProxy.Register();
                
                // Use newer IDetailedStoreListener API to avoid deprecation warnings
                var wrappedListener = AdInMoIapProxy.Wrap(listener);
                if (wrappedListener is IDetailedStoreListener detailedListener)
                {
                    UnityPurchasing.Initialize(detailedListener, builder);
                }
                else
                {
                    // Fallback for compatibility
                    #pragma warning disable CS0618
                    UnityPurchasing.Initialize(wrappedListener, builder);
                    #pragma warning restore CS0618
                }
            }
        }

        /// <summary>
        /// Checks if the AdInMo IAP system is ready for purchases
        /// </summary>
        /// <returns>True if the system is initialized and ready</returns>
        public static bool IsReady()
        {
            return AdInMoIapProxy.IsReady();
        }

        /// <summary>
        /// Validates the current AdInMo IAP initialization state (development/debugging only)
        /// </summary>
        public static void ValidateState()
        {
            AdInMoIapProxy.ValidateInitializationState();
        }

        /// <summary>
        /// Validates product configuration (development/debugging only)
        /// </summary>
        public static void ValidateProducts()
        {
            AdInMoIapProxy.ValidateProductConfiguration();
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Resets AdInMo IAP state for testing (EDITOR ONLY)
        /// WARNING: Only use this for testing/debugging scenarios
        /// </summary>
        public static void ResetForTesting()
        {
            AdInMoIapProxy.Reset();
        }
        #endif

        // ---- Logging helpers ----
        private static void Log(string message)
        {
            #if ADINMO_IAP_PROXY_LOGS || UNITY_EDITOR
            Debug.Log(message);
            #endif
        }

        private static void LogWarning(string message)
        {
            #if ADINMO_IAP_PROXY_LOGS || UNITY_EDITOR
            Debug.LogWarning(message);
            #endif
        }

        private static void LogError(string message)
        {
            Debug.LogError(message); // Always log errors
        }
    }

    /// <summary>
    /// Internal proxy that handles AdInMo IAPBoost callbacks and reporting
    /// </summary>
    internal static class AdInMoIapProxy
    {
        /// <summary>
        /// Wraps an IStoreListener with AdInMo reporting capabilities
        /// </summary>
        internal static IStoreListener Wrap(IStoreListener inner) => new Proxy(inner);

        /// <summary>
        /// Registers all AdInMo IAPBoost callbacks
        /// </summary>
        internal static void Register()
        {
            if (_registered) return;
            _registered = true;

            Log("[AdInMoIapProxy] Registering AdInMo IAPBoost callbacks...");

            // 1) When an AdInMo UI/ad requests a purchase, start Unity IAP for that SKU
            AdinmoManager.SetInAppPurchaseCallback(OnAdInMoPurchaseRequested);

            // 2) Let AdInMo show localized store prices in their magnifier CTA
            AdinmoManager.SetInAppPurchaseGetPriceCallback(GetLocalizedPriceString);

            // 3) Let AdInMo know if a non-consumable is already owned (for gating UI/serving)
            // Use InAppAlreadyPurchasedReply constructor for AdInMo SDK compatibility
            AdinmoManager.SetInAppPurchasedAlreadyCallback((string iapId) =>
            {
                var p = _controller?.products?.WithID(iapId);
                bool isPurchased = p != null && p.hasReceipt;
                Log($"[AdInMoIapProxy] Already purchased check for {iapId}: {isPurchased}");
                
                if (isPurchased && p != null)
                {
                    // Return with purchase details for owned products
                    return new InAppAlreadyPurchasedReply(true, (float)p.metadata.localizedPrice, p.metadata.isoCurrencyCode);
                }
                else
                {
                    // Return false for non-purchased products
                    return new InAppAlreadyPurchasedReply(false);
                }
            });
            Log("[AdInMoIapProxy] Using InAppAlreadyPurchasedReply constructor for AdInMo SDK compatibility");

            Log("[AdInMoIapProxy] All callbacks registered successfully!");
        }

        /// <summary>
        /// Validates that all requested product IDs are properly configured in Unity IAP
        /// Call this after IAP initialization for debugging purposes
        /// </summary>
        internal static void ValidateProductConfiguration()
        {
            if (_controller?.products?.all == null)
            {
                LogWarning("[AdInMoIapProxy] Cannot validate products - IAP not initialized");
                return;
            }

            var registeredProducts = _controller.products.all.Select(p => p.definition.id).ToArray();
            Log($"[AdInMoIapProxy] Registered Unity IAP Products: [{string.Join(", ", registeredProducts)}]");
            Log("[AdInMoIapProxy] Ensure all AdInMo campaign SKU IDs match these product IDs to avoid purchase failures");
            
            #if UNITY_EDITOR
            Log("[AdInMoIapProxy] 💡 TIP: Check your AdInMo dashboard campaigns and verify all SKU IDs are listed above");
            #endif
        }

        /// <summary>
        /// Validates the current initialization state and reports any issues
        /// </summary>
        internal static void ValidateInitializationState()
        {
            lock (_initLock)
            {
                Log($"[AdInMoIapProxy] Initialization State Report:");
                Log($"  - AdInMo callbacks registered: {_registered}");
                Log($"  - Unity IAP initializing: {_iapInitializing}");
                Log($"  - Unity IAP initialized: {_iapInitialized}");
                Log($"  - Controller available: {_controller != null}");
                
                if (_iapInitializing)
                {
                    LogWarning("[AdInMoIapProxy] Initialization in progress - wait for completion before making purchases");
                }
                
                if (_iapInitialized && _controller == null)
                {
                    LogError("[AdInMoIapProxy] State corruption detected: marked as initialized but controller is null");
                }
                
                if (!_iapInitialized && _controller != null)
                {
                    LogWarning("[AdInMoIapProxy] Unexpected state: controller exists but not marked as initialized");
                }
            }
        }

        /// <summary>
        /// Resets all initialization state - use for testing or hot-reload scenarios
        /// WARNING: This will clear all state and may cause issues if called during active IAP operations
        /// </summary>
        internal static void Reset()
        {
            lock (_initLock)
            {
                Log("[AdInMoIapProxy] Resetting all initialization state");
                
                _registered = false;
                _iapInitialized = false;
                _iapInitializing = false;
                _controller = null;
                _lastRequestedByAdInMo = null;
                
                Log("[AdInMoIapProxy] State reset complete - initialization can be performed again");
                
                // Note: We cannot unregister AdInMo callbacks as the SDK doesn't provide that functionality
                // The _registered flag will prevent re-registration on next initialization
            }
        }

        /// <summary>
        /// Checks if the IAP system is ready for purchases
        /// </summary>
        internal static bool IsReady()
        {
            lock (_initLock)
            {
                return _iapInitialized && _controller != null && !_iapInitializing;
            }
        }

        // ---- internal state ----
        static bool _registered;
        internal static bool _iapInitialized;        // Track Unity IAP initialization state
        internal static bool _iapInitializing;       // Track initialization in progress
        internal static IStoreController _controller;
        static string _lastRequestedByAdInMo; // track source for IAPSource flagging
        internal static readonly object _initLock = new object(); // Thread safety for initialization

        // ---- AdInMo callbacks ----

        static void OnAdInMoPurchaseRequested(string iapId)
        {
            Log($"[AdInMoIapProxy] Purchase requested from AdInMo for: {iapId}");
            _lastRequestedByAdInMo = iapId ?? string.Empty;
            
            if (_controller == null) 
            { 
                LogError("[AdInMoIapProxy] Cannot process purchase - IAP not initialized yet");
                ReportPurchaseUnavailable(iapId, "IAP system not initialized");
                return; 
            }

            var product = _controller.products?.WithID(iapId);
            if (product == null)
            {
                LogError($"[AdInMoIapProxy] Product '{iapId}' not registered in Unity IAP configuration");
                ReportPurchaseUnavailable(iapId, "Product not configured in Unity IAP");
                return;
            }

            if (!product.availableToPurchase)
            {
                LogWarning($"[AdInMoIapProxy] Product '{iapId}' is not available for purchase");
                ReportPurchaseUnavailable(iapId, "Product not available for purchase", product);
                return;
            }

            Log($"[AdInMoIapProxy] Starting purchase for: {iapId}");
            _controller.InitiatePurchase(product);
        }

        /// <summary>
        /// Reports to AdInMo that a purchase cannot be completed due to configuration issues
        /// </summary>
        static void ReportPurchaseUnavailable(string iapId, string reason, Product product = null)
        {
            // Extract product metadata if available
            string currency = product?.metadata?.isoCurrencyCode ?? "USD";
            float price = product != null ? (float)(product.metadata?.localizedPrice ?? 0m) : 0f;
            
            LogError($"[AdInMoIapProxy] Reporting purchase failure to AdInMo: {iapId} - {reason}");
            
            try 
            {
                // Report the configuration failure to AdInMo
                AdinmoManager.InAppPurchaseFailed(iapId, currency, price, reason, IAPSource.AdInMo);
            }
            catch (Exception ex)
            {
                LogError($"[AdInMoIapProxy] Failed to report purchase unavailability to AdInMo: {ex.Message}");
            }
            
            // Clear the tracking since we've handled this request
            _lastRequestedByAdInMo = null;
        }

        static string GetLocalizedPriceString(string itemId)
        {
            var p = _controller?.products?.WithID(itemId);
            var price = p?.metadata?.localizedPriceString;
            Log($"[AdInMoIapProxy] Price requested for {itemId}: {price}");
            return price;
        }

        // ---- Proxy wrapper for Unity IAP ----

        class Proxy : IDetailedStoreListener
        {
            readonly IStoreListener _inner;
            public Proxy(IStoreListener inner) { _inner = inner; }

            public void OnInitialized(IStoreController c, IExtensionProvider e)
            {
                lock (_initLock)
                {
                    // Defensive check for multiple initialization
                    if (_controller != null)
                    {
                        LogWarning("[AdInMoIapProxy] Controller already set - possible multiple initialization detected");
                    }
                    
                    _controller = c; // allow callbacks to query products & initiate purchases
                    _iapInitialized = true;     // Mark as completed
                    _iapInitializing = false;   // Clear in-progress flag
                    
                    Log("[AdInMoIapProxy] Unity IAP initialized successfully, store controller ready");

                    #if UNITY_EDITOR
                    // Validate products in editor for debugging
                    ValidateProductConfiguration();
                    #endif
                }
                
                _inner.OnInitialized(c, e);
            }

            public void OnInitializeFailed(InitializationFailureReason error)
            {
                lock (_initLock)
                {
                    _iapInitializing = false;  // Clear in-progress flag to allow retry
                    _iapInitialized = false;   // Ensure not marked as initialized
                    
                    LogError($"[AdInMoIapProxy] Unity IAP initialization failed: {error} - retry is now possible");
                }
                
                #pragma warning disable CS0612
                _inner.OnInitializeFailed(error);
                #pragma warning restore CS0612
            }

            public void OnInitializeFailed(InitializationFailureReason error, string message)
            {
                lock (_initLock)
                {
                    _iapInitializing = false;  // Clear in-progress flag to allow retry
                    _iapInitialized = false;   // Ensure not marked as initialized
                    
                    LogError($"[AdInMoIapProxy] Unity IAP initialization failed: {error} - {message} - retry is now possible");
                }
                
                _inner.OnInitializeFailed(error, message);
            }

            public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
            {
                // Let game code decide success (Complete) vs Pending
                var result = _inner.ProcessPurchase(args);

                if (result == PurchaseProcessingResult.Complete)
                {
                    ReportSuccess(args.purchasedProduct);
                }
                return result;
            }

            public void OnPurchaseFailed(Product product, PurchaseFailureDescription failure)
            {
                // Try calling the newer method first, fallback to older method
                try
                {
                    ((IDetailedStoreListener)_inner)?.OnPurchaseFailed(product, failure);
                }
                catch
                {
                    // Fallback to legacy method if newer interface not implemented
                    #pragma warning disable CS0618
                    _inner.OnPurchaseFailed(product, failure?.reason ?? PurchaseFailureReason.Unknown);
                    #pragma warning restore CS0618
                }
                ReportFailure(product, failure?.reason.ToString());
            }

            public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
            {
                #pragma warning disable CS0618
                _inner.OnPurchaseFailed(product, failureReason);
                #pragma warning restore CS0618
                ReportFailure(product, failureReason.ToString());
            }

            // ---- Reporting helpers ----

            static void ReportSuccess(Product p)
            {
                if (p == null) return;
                var id   = p.definition?.id ?? "";
                var curr = p.metadata?.isoCurrencyCode ?? "";
                var amt  = (float)(p.metadata?.localizedPrice ?? 0m);
                var tx   = p.transactionID;

                var source = (_lastRequestedByAdInMo == id) ? IAPSource.AdInMo : IAPSource.Other;
                Log($"[AdInMoIapProxy] Reporting success to AdInMo: {id}, {curr}, {amt}, {tx}, source: {source}");
                
                AdinmoManager.InAppPurchaseSuccess(id, curr, amt, tx, source);
                _lastRequestedByAdInMo = null;
            }

            static void ReportFailure(Product p, string reason)
            {
                if (p == null) return;
                var id   = p.definition?.id ?? "";
                var curr = p.metadata?.isoCurrencyCode ?? "";
                var amt  = (float)(p.metadata?.localizedPrice ?? 0m);

                var source = (_lastRequestedByAdInMo == id) ? IAPSource.AdInMo : IAPSource.Other;
                Log($"[AdInMoIapProxy] Reporting failure to AdInMo: {id}, reason: {reason}, source: {source}");
                
                // Newer SDKs accept a failure reason parameter.
                try {
                    AdinmoManager.InAppPurchaseFailed(id, curr, amt, reason, source);
                } catch {
                    // Older SDKs without reason/source overload:
                    AdinmoManager.InAppPurchaseFailed(id, curr, amt, null);
                }
                _lastRequestedByAdInMo = null;
            }
        }

        // ---- Logging helpers ----
        private static void Log(string message)
        {
            #if ADINMO_IAP_PROXY_LOGS || UNITY_EDITOR
            Debug.Log(message);
            #endif
        }

        private static void LogWarning(string message)
        {
            #if ADINMO_IAP_PROXY_LOGS || UNITY_EDITOR
            Debug.LogWarning(message);
            #endif
        }

        private static void LogError(string message)
        {
            Debug.LogError(message); // Always log errors
        }
    }
}
