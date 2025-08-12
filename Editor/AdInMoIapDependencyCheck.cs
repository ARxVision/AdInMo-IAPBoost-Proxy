#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace AdInMo.IapProxy.Editor
{
    [InitializeOnLoad]
    internal static class AdInMoIapDependencyCheck
    {
        static AdInMoIapDependencyCheck()
        {
            bool hasIap = TypeExists("UnityEngine.Purchasing.ConfigurationBuilder") && 
                         TypeExists("UnityEngine.Purchasing.UnityPurchasing");
            bool hasAdInMo = TypeExists("Adinmo.AdinmoManager") || 
                           TypeExists("AdinmoManager");

            if (!hasIap)
            {
                ShowOnce("AdInMo IAP Proxy: Unity IAP missing",
                    "Unity IAP (com.unity.purchasing) is required for AdInMo IAPBoost integration.\n\n" +
                    "Please install Unity IAP:\n" +
                    "1. Open Package Manager (Window → Package Manager)\n" +
                    "2. Select 'Unity Registry' from the dropdown\n" +
                    "3. Search for 'In App Purchasing'\n" +
                    "4. Click 'Install'");
            }

            if (!hasAdInMo)
            {
                ShowOnce("AdInMo IAP Proxy: AdInMo SDK missing",
                    "AdInMo SDK is required for IAPBoost integration.\n\n" +
                    "Please complete the setup:\n" +
                    "1. Import the AdInMo SDK into your project\n" +
                    "2. Add 'ADINMO_PRESENT' to Project Settings → Player → Scripting Define Symbols\n" +
                    "3. Restart Unity after making these changes");
            }

            // Check for common configuration issues
            if (hasIap && hasAdInMo)
            {
                CheckScriptingDefines();
            }
        }

        static bool TypeExists(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try 
                { 
                    if (asm.GetType(fullName, false) != null) 
                        return true; 
                }
                catch 
                { 
                    // Ignore exceptions when checking for types
                }
            }
            return false;
        }

        static void CheckScriptingDefines()
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            
            bool hasUnityPurchasing = defines.Contains("UNITY_PURCHASING");
            bool hasAdinmoPresent = defines.Contains("ADINMO_PRESENT");

            if (!hasUnityPurchasing)
            {
                ShowOnce("AdInMo IAP Proxy: Missing UNITY_PURCHASING define",
                    "UNITY_PURCHASING scripting define symbol is missing.\n\n" +
                    "This should be automatically added when Unity IAP is installed. " +
                    "If you're seeing this message, try reimporting Unity IAP or restart Unity.");
            }

            if (!hasAdinmoPresent)
            {
                ShowOnce("AdInMo IAP Proxy: Missing ADINMO_PRESENT define",
                    "ADINMO_PRESENT scripting define symbol is missing.\n\n" +
                    "Please add 'ADINMO_PRESENT' to:\n" +
                    "Project Settings → Player → Scripting Define Symbols\n\n" +
                    "This tells the AdInMo IAP Proxy that the AdInMo SDK is available.");
            }
        }

        static void ShowOnce(string title, string msg)
        {
            string key = "AdInMoIapProxyDepCheck_" + title.GetHashCode();
            if (SessionState.GetBool(key, false)) return;
            
            SessionState.SetBool(key, true);
            
            // Show as a dialog with option to suppress
            if (EditorUtility.DisplayDialog(title, msg + "\n\nShow this warning again in this session?", "Yes", "No"))
            {
                SessionState.SetBool(key, false); // Show again this session
            }
        }
    }

    /// <summary>
    /// Editor utilities for AdInMo IAP Proxy
    /// </summary>
    public static class AdInMoIapProxyUtils
    {
        [MenuItem("AdInMo/Validate IAP Setup")]
        public static void ValidateSetup()
        {
            var hasIap = TypeExists("UnityEngine.Purchasing.ConfigurationBuilder");
            var hasAdInMo = TypeExists("Adinmo.AdinmoManager") || TypeExists("AdinmoManager");
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            string report = "AdInMo IAP Proxy Setup Validation:\n\n";
            report += $"✓ Unity IAP Available: {(hasIap ? "YES" : "NO")}\n";
            report += $"✓ AdInMo SDK Available: {(hasAdInMo ? "YES" : "NO")}\n";
            report += $"✓ UNITY_PURCHASING Define: {(defines.Contains("UNITY_PURCHASING") ? "YES" : "NO")}\n";
            report += $"✓ ADINMO_PRESENT Define: {(defines.Contains("ADINMO_PRESENT") ? "YES" : "NO")}\n\n";

            if (hasIap && hasAdInMo && defines.Contains("UNITY_PURCHASING") && defines.Contains("ADINMO_PRESENT"))
            {
                report += "🎉 Setup is complete! You can now use:\nUnityPurchasing.InitializeWithAdInMo(this, builder);";
            }
            else
            {
                report += "❌ Setup incomplete. Please address the missing items above.";
            }

            EditorUtility.DisplayDialog("AdInMo IAP Proxy Setup", report, "OK");
        }

        [MenuItem("AdInMo/Add ADINMO_PRESENT Define")]
        public static void AddAdinmoDefine()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            
            if (!defines.Contains("ADINMO_PRESENT"))
            {
                if (!string.IsNullOrEmpty(defines))
                    defines += ";";
                defines += "ADINMO_PRESENT";
                
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
                Debug.Log("Added ADINMO_PRESENT to scripting define symbols");
            }
            else
            {
                Debug.Log("ADINMO_PRESENT already present in scripting define symbols");
            }
        }

        private static bool TypeExists(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { if (asm.GetType(fullName, false) != null) return true; }
                catch { }
            }
            return false;
        }
    }
}
#endif
