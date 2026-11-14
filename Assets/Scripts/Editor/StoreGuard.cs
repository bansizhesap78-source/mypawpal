using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PatiDostum.EditorTools
{
    /// <summary>
    /// Oyun internetsiz oynanır; reklam, analiz ve çökme raporu yoktur. Alpi oyunlarından farkı: Google Play
    /// satın alma (com.unity.purchasing, BILLING izni) serbesttir (plan §7b).
    /// Ayarları sahne kurucu uygular; derleme öncesi denetim de biri yasak bir şey eklerse derlemeyi durdurur.
    /// </summary>
    public static class StoreSettings
    {
        public static readonly string[] ForbiddenPackages =
        {
            "com.unity.ads", "com.unity.analytics", "com.unity.services.analytics",
            "com.unity.modules.unityanalytics", "com.google.ads.mobile", "com.google.firebase",
            "com.unity.collab-proxy", "com.unity.services.cloud-diagnostics"
        };

        public static readonly string[] ForbiddenPaths =
        {
            "Assets/GoogleMobileAds", "Assets/Firebase", "Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib",
            "Assets/Plugins/Android/googlemobileads-unity.aar"
        };

        public static void Apply()
        {
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.enableCrashReportAPI = false;

            // Assets/Plugins/Android/AndroidManifest.xml: internet ve reklam izinlerini çıkaran, yedeklemeyi kapatan ana manifest
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty customManifest = settings.FindProperty("useCustomMainManifest");
            if (customManifest != null) customManifest.boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public sealed class StoreBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            // İkinci kilit: Development build'li bir AAB (sahte satın alma içerir) hangi yoldan gelirse gelsin durur
            if (report.summary.platform == BuildTarget.Android
                && (report.summary.options & BuildOptions.Development) != 0
                && EditorUserBuildSettings.buildAppBundle)
                throw new BuildFailedException("Development build ile AAB üretilemez (sahte test mağazası içerir). Development Build seçeneğini kapat.");

            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string manifest = File.ReadAllText(Path.Combine(root, "Packages/manifest.json"));
            string lockPath = Path.Combine(root, "Packages/packages-lock.json");
            string packageLock = File.Exists(lockPath) ? File.ReadAllText(lockPath) : "";
            foreach (string package in StoreSettings.ForbiddenPackages)
            {
                if (manifest.Contains("\"" + package) || packageLock.Contains("\"" + package))
                    throw new BuildFailedException("Yasak paket var (reklam/analiz): " + package);
            }

            foreach (string path in StoreSettings.ForbiddenPaths)
            {
                string full = Path.Combine(root, path);
                if (Directory.Exists(full) || File.Exists(full))
                    throw new BuildFailedException("Yasak SDK var (reklam/analiz): " + path);
            }

            string connect = File.ReadAllText(Path.Combine(root, "ProjectSettings/UnityConnectSettings.asset"));
            if (connect.Contains("m_EnableCloudDiagnosticsReporting: 1") || connect.Contains("m_EngineDiagnosticsEnabled: 1"))
                throw new BuildFailedException("Unity çökme raporu ve analiz kapalı kalmalı: ProjectSettings/UnityConnectSettings.asset");

            StoreSettings.Apply();
            Debug.Log("[Pati Dostum] Mağaza denetimi geçti: reklam, analiz ve internet izni yok.");
        }
    }
}
