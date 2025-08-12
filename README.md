# AdInMo IAPBoost Proxy

**One-call Unity IAP + AdInMo IAPBoost integration** with multiple initialization protection and enhanced debugging.

🚀 **Quick Start**: Replace your Unity IAP initialization with a single call:
```csharp
UnityPurchasingAdInMoExtensions.InitializeWithAdInMo(this, builder);
```

## ✨ Features

- **🔒 Multiple initialization protection** - Safe to call from multiple scenes
- **🧵 Thread-safe initialization** - Concurrent calls handled properly  
- **🔍 Enhanced debugging** - Validation tools and detailed logging
- **⚡ Zero configuration** - Works with existing Unity IAP setup
- **🛡️ Dependency validation** - Automatic checks for missing packages
- **📊 Purchase attribution** - Tracks AdInMo vs. direct purchases

## 📋 Requirements

- **Unity 2021.3+**
- **Unity IAP** (`com.unity.purchasing` 4.11.0+)
- **AdInMo SDK** (imported and configured)
- **ADINMO_PRESENT** define symbol (added automatically by editor tools)

## 📦 Installation

### Option 1: Package Manager (Recommended)
1. Open **Package Manager** (`Window → Package Manager`)
2. Click **`+`** → **Add package from git URL**
3. Enter: `https://github.com/adinmo/adinmo-iap-proxy.git`

### Option 2: Git Submodule
```bash
git submodule add https://github.com/adinmo/adinmo-iap-proxy.git Packages/adinmo-iap-proxy
```

### Option 3: Manual Download
1. Download the latest release
2. Extract to `Packages/adinmo-iap-proxy/`

## 🛠️ Setup

### 1. Install Dependencies
- Install **Unity IAP** via Package Manager
- Import the **AdInMo SDK** into your project

### 2. Configure Scripting Defines
The package automatically adds `ADINMO_PRESENT` define symbol. If needed, add manually:
- Go to **Project Settings → Player → Scripting Define Symbols**
- Add `ADINMO_PRESENT`

### 3. Validate Setup
Use the editor menu: **AdInMo → Validate IAP Setup** to check your configuration.

## 🚀 Usage

### Basic Integration

Replace your existing Unity IAP initialization:

```csharp
// OLD: Standard Unity IAP initialization
// UnityPurchasing.Initialize(this, builder);

// NEW: AdInMo IAPBoost integration
UnityPurchasingAdInMoExtensions.InitializeWithAdInMo(this, builder);
```

### Complete Example

```csharp
using UnityEngine;
using UnityEngine.Purchasing;
using AdInMo.IapProxy;

public class IAPManager : MonoBehaviour, IStoreListener
{
    void Start()
    {
        var module = StandardPurchasingModule.Instance();
        var builder = ConfigurationBuilder.Instance(module);

        // Add products - IDs must match your AdInMo campaign SKUs
        builder.AddProduct("coins_100", ProductType.Consumable);
        builder.AddProduct("premium_unlock", ProductType.NonConsumable);

        // ✨ One call to initialize Unity IAP + AdInMo IAPBoost
        UnityPurchasingAdInMoExtensions.InitializeWithAdInMo(this, builder);
    }

    // Implement IStoreListener methods...
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions) 
    {
        Debug.Log("IAP Ready!");
    }
    
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) 
    {
        // Handle purchase - AdInMo reporting is automatic
        return PurchaseProcessingResult.Complete;
    }

    // ... other IStoreListener methods
}
```

## 🔧 Advanced Features

### Check Initialization Status
```csharp
if (UnityPurchasingAdInMoExtensions.IsReady())
{
    // Safe to make purchases
}
```

### Debugging & Validation
```csharp
// Validate current state
UnityPurchasingAdInMoExtensions.ValidateState();

// Check product configuration
UnityPurchasingAdInMoExtensions.ValidateProducts();
```

### Editor-Only Testing
```csharp
#if UNITY_EDITOR
// Reset state for testing (Editor only)
UnityPurchasingAdInMoExtensions.ResetForTesting();
#endif
```

### Control Logging
Add `ADINMO_IAP_PROXY_LOGS` to Scripting Define Symbols to enable detailed logging in builds.

## 🎯 Important Notes

### Product ID Matching
**Critical**: Your Unity IAP product IDs must exactly match your AdInMo campaign SKU IDs:

```csharp
// Unity IAP
builder.AddProduct("coins_100", ProductType.Consumable);

// AdInMo Dashboard Campaign
// SKU ID: coins_100  ← Must match exactly
```

### Multiple Initialization Protection
The proxy automatically handles multiple initialization attempts:
- ✅ **First call**: Initializes Unity IAP + AdInMo
- ✅ **Subsequent calls**: Safely ignored with immediate listener notification
- ✅ **Concurrent calls**: Thread-safe with proper locking

### Purchase Attribution
The proxy automatically tracks purchase sources:
- **AdInMo purchases**: When triggered by AdInMo UI/ads
- **Direct purchases**: When triggered by your game UI

## 🐛 Troubleshooting

### Common Issues

**"AdInMo IAP Proxy requires Unity IAP"**
- Install Unity IAP via Package Manager
- Ensure `UNITY_PURCHASING` define is present

**"AdInMo IAP Proxy requires the AdInMo SDK"**
- Import the AdInMo SDK
- Add `ADINMO_PRESENT` to Scripting Define Symbols

**"Product not configured in Unity IAP"**
- Ensure your AdInMo campaign SKU IDs match Unity IAP product IDs exactly
- Use `ValidateProducts()` to see registered products

**"IAP system not initialized yet"**
- Call `InitializeWithAdInMo()` before AdInMo tries to trigger purchases
- Use `IsReady()` to check initialization status

### Debug Tools

1. **Menu**: `AdInMo → Validate IAP Setup`
2. **Runtime**: `UnityPurchasingAdInMoExtensions.ValidateState()`
3. **Logging**: Add `ADINMO_IAP_PROXY_LOGS` define for detailed logs

## 📚 Samples

Import the **IAPBoost Quickstart** sample via Package Manager:
1. Select the AdInMo IAPBoost Proxy package
2. Expand **Samples**
3. Click **Import** next to "IAPBoost Quickstart"

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: [AdInMo Developer Docs](https://docs.adinmo.com)
- **Issues**: [GitHub Issues](https://github.com/adinmo/adinmo-iap-proxy/issues)
- **Contact**: support@adinmo.com
