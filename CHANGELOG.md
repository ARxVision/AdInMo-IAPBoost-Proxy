# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Planned
- Sample scene with visual testing interface
- Editor window to simulate AdInMo purchase requests
- ScriptableObject-based product ID mapping configuration
- Advanced logging configuration options

---

## [1.1.0] - 2024-12-19
### Added
- **Multiple initialization protection**: Prevents Unity IAP double-initialization issues
- **Thread-safe initialization**: Concurrent calls handled with proper locking mechanisms
- **Enhanced debugging tools**: `ValidateInitializationState()` and `ValidateProductConfiguration()`
- **Dependency guards**: Compile-time `#error` if Unity IAP or AdInMo SDK is missing
- **Editor dependency check**: Automatic detection of missing packages with helpful dialogs
- **Editor menu tools**: "AdInMo → Validate IAP Setup" and "AdInMo → Add ADINMO_PRESENT Define"
- **Namespace organization**: All code properly namespaced under `AdInMo.IapProxy`
- **Conditional logging**: Use `ADINMO_IAP_PROXY_LOGS` define to control logging in builds
- **Assembly definitions**: Proper UPM package structure with Runtime/Editor separation
- **Comprehensive sample**: QuickstartBootstrap with debug UI and testing tools

### Enhanced
- **Error handling**: Better failure reporting when products are misconfigured
- **Purchase attribution**: More accurate tracking of AdInMo vs. direct purchases
- **State management**: Robust initialization state tracking with recovery mechanisms
- **Documentation**: Comprehensive README with troubleshooting and setup guides

### Fixed
- **Race conditions**: Thread-safe access to shared state variables
- **Controller override**: Defensive checks prevent state corruption from multiple initializations
- **Initialization failure recovery**: Proper cleanup allows retry after failed initialization
- **Access level consistency**: All internal methods properly marked as `internal`

### Technical Improvements
- **Proper UPM structure**: Runtime/, Editor/, Samples~/, Documentation~ organization
- **Version defines**: Automatic `UNITY_PURCHASING` define when Unity IAP is available
- **Editor-only features**: Testing and validation tools only available in Editor
- **Dependency validation**: Startup checks ensure all required components are available

---

## [1.0.0] - 2024-12-10
### Added
- **One-call initialization**: `UnityPurchasing.InitializeWithAdInMo(this, builder)`
- **AdInMo callback integration**: Purchase request, localized price, and ownership check handlers
- **Purchase result reporting**: Automatic success/failure reporting to AdInMo with source attribution
- **Compatibility layer**: Support for both `IStoreListener` and `IDetailedStoreListener` interfaces
- **Basic error handling**: Purchase unavailability reporting for configuration issues
- **Logging**: Debug output for troubleshooting and monitoring

### Core Features
- Unity IAP initialization wrapper with AdInMo callback registration
- Product availability validation before purchase attempts
- Automatic purchase source tracking (AdInMo vs. Other)
- Fallback support for legacy Unity IAP API versions
- Basic product configuration validation

[Keep a Changelog]: https://keepachangelog.com/en/1.1.0/
[Semantic Versioning]: https://semver.org/spec/v2.0.0.html
