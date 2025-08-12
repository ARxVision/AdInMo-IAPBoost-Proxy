using UnityEngine;
using UnityEngine.Purchasing;
using AdInMo.IapProxy;

namespace AdInMo.IapProxy.Samples
{
    /// <summary>
    /// Minimal setup example for AdInMo IAPBoost integration
    /// This script demonstrates the one-call initialization and basic IAP handling
    /// </summary>
    public class QuickstartBootstrap : MonoBehaviour, IStoreListener
    {
        [Header("Test Products")]
        [SerializeField] private string[] productIds = {
            "coins_100",
            "coins_500", 
            "premium_remove_ads",
            "premium_unlock_all"
        };

        [Header("Debug Options")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool validateOnStart = true;

        private void Start()
        {
            // Validate setup if requested
            if (validateOnStart)
            {
                ValidateSetup();
            }

            InitializeIAP();
        }

        private void ValidateSetup()
        {
            Debug.Log("[QuickstartBootstrap] Validating AdInMo IAP setup...");
            
            // Check if the proxy is available and ready
            if (enableDebugLogs)
            {
                UnityPurchasingAdInMoExtensions.ValidateState();
            }
        }

        private void InitializeIAP()
        {
            Debug.Log("[QuickstartBootstrap] Initializing Unity IAP with AdInMo IAPBoost...");

            // Create the standard purchasing module
            var module = StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);

            // Add products - IMPORTANT: These IDs must match your AdInMo campaign SKUs
            foreach (var productId in productIds)
            {
                builder.AddProduct(productId, ProductType.Consumable);
                Debug.Log($"[QuickstartBootstrap] Added product: {productId}");
            }

            // ✨ ONE CALL TO INITIALIZE UNITY IAP + ADINMO IAPBOOST ✨
            UnityPurchasingAdInMoExtensions.InitializeWithAdInMo(this, builder);
        }

        // ---- Unity IAP Store Listener Implementation ----

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("[QuickstartBootstrap] ✅ Unity IAP initialized successfully!");
            
            if (enableDebugLogs)
            {
                Debug.Log($"[QuickstartBootstrap] Available products: {controller.products.all.Length}");
                foreach (var product in controller.products.all)
                {
                    Debug.Log($"[QuickstartBootstrap] - {product.definition.id}: {product.metadata.localizedPriceString}");
                }

                // Validate product configuration
                UnityPurchasingAdInMoExtensions.ValidateProducts();
            }
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[QuickstartBootstrap] ❌ Unity IAP initialization failed: {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[QuickstartBootstrap] ❌ Unity IAP initialization failed: {error} - {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;
            Debug.Log($"[QuickstartBootstrap] ✅ Purchase successful: {product.definition.id}");

            // Handle the purchase based on product ID
            switch (product.definition.id)
            {
                case "coins_100":
                    GrantCoins(100);
                    break;
                case "coins_500":
                    GrantCoins(500);
                    break;
                case "premium_remove_ads":
                    RemoveAds();
                    break;
                case "premium_unlock_all":
                    UnlockAllContent();
                    break;
                default:
                    Debug.LogWarning($"[QuickstartBootstrap] Unknown product purchased: {product.definition.id}");
                    break;
            }

            // Return Complete to finalize the purchase
            // Use Pending if you need to validate with your server first
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failure)
        {
            Debug.LogError($"[QuickstartBootstrap] ❌ Purchase failed: {product.definition.id} - {failure.reason}: {failure.message}");
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Debug.LogError($"[QuickstartBootstrap] ❌ Purchase failed: {product.definition.id} - {reason}");
        }

        // ---- Game Logic Examples ----

        private void GrantCoins(int amount)
        {
            Debug.Log($"[QuickstartBootstrap] 🪙 Granting {amount} coins to player");
            // TODO: Implement your coin granting logic here
            // PlayerData.AddCoins(amount);
        }

        private void RemoveAds()
        {
            Debug.Log($"[QuickstartBootstrap] 🚫 Removing ads for player");
            // TODO: Implement your ad removal logic here
            // AdManager.DisableAds();
            // PlayerPrefs.SetInt("AdsRemoved", 1);
        }

        private void UnlockAllContent()
        {
            Debug.Log($"[QuickstartBootstrap] 🔓 Unlocking all premium content");
            // TODO: Implement your content unlocking logic here
            // PlayerData.UnlockAllLevels();
            // PlayerData.UnlockAllCharacters();
        }

        // ---- Debug UI Methods (for testing) ----

        private void OnGUI()
        {
            if (!enableDebugLogs) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("AdInMo IAP Debug Panel", GUI.skin.box);

            if (GUILayout.Button("Check IAP Ready"))
            {
                bool isReady = UnityPurchasingAdInMoExtensions.IsReady();
                Debug.Log($"[QuickstartBootstrap] IAP Ready: {isReady}");
            }

            if (GUILayout.Button("Validate State"))
            {
                UnityPurchasingAdInMoExtensions.ValidateState();
            }

            if (GUILayout.Button("Validate Products"))
            {
                UnityPurchasingAdInMoExtensions.ValidateProducts();
            }

            #if UNITY_EDITOR
            if (GUILayout.Button("Reset for Testing (Editor Only)"))
            {
                UnityPurchasingAdInMoExtensions.ResetForTesting();
                Debug.Log("[QuickstartBootstrap] Reset complete - you can reinitialize now");
            }
            #endif

            GUILayout.EndArea();
        }
    }
}
