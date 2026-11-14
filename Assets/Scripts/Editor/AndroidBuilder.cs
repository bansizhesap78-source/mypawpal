using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PatiDostum.EditorTools
{
    /// <summary>
    /// Android derlemeleri.
    ///  - BuildApk: test APK'sı (Unity'nin debug anahtarıyla imzalı, yalnızca ARM64; mağazaya yüklenemez).
    ///  - BuildAab: Play Store için imzalı AAB (ARM64 + ARMv7). Parolalar ortam değişkeninden okunur, dosyaya yazılmaz.
    /// Çıktı klasörü PATI_BUILD_DIR ile değiştirilebilir (varsayılan: proje/Builds).
    /// Komut satırı: -buildTarget Android -executeMethod PatiDostum.EditorTools.AndroidBuilder.BuildAab
    /// </summary>
    public static class AndroidBuilder
    {
        private const string BaseName = "MyPawPal";
        private const int PlayTargetSdk = 36;

        // AAB imzası için ortam değişkenleri (Alpi oyunlarıyla aynı anahtar)
        private const string KeystorePathVar = "ALPI_KEYSTORE_PATH";
        private const string KeystorePassVar = "ALPI_KEYSTORE_PASS";
        private const string KeyAliasVar = "ALPI_KEY_ALIAS";
        private const string KeyPassVar = "ALPI_KEY_PASS";
        private const string VersionCodeVar = "PATI_VERSION_CODE";
        private const string VersionNameVar = "PATI_VERSION_NAME";

        [MenuItem("Pati Dostum/APK Derle (test)")]
        public static void BuildApk()
        {
            Build(appBundle: false);
        }

        /// <summary>Yalnızca komut satırından (build-aab.ps1) çağrılır; parolayı betik sorar.</summary>
        public static void BuildAab()
        {
            string keystore = Environment.GetEnvironmentVariable(KeystorePathVar);
            string keystorePass = Environment.GetEnvironmentVariable(KeystorePassVar);
            string alias = Environment.GetEnvironmentVariable(KeyAliasVar);
            string keyPass = Environment.GetEnvironmentVariable(KeyPassVar);

            if (string.IsNullOrEmpty(keystore) || !File.Exists(keystore) || string.IsNullOrEmpty(keystorePass) || string.IsNullOrEmpty(alias))
            {
                Debug.LogError($"[Pati Dostum] AAB için {KeystorePathVar}, {KeystorePassVar} ve {KeyAliasVar} gerekli (keystore dosyası bulunmalı).");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(keyPass) ? keystorePass : keyPass;

            // Play Store her yüklemede daha büyük bir sürüm kodu ister
            if (int.TryParse(Environment.GetEnvironmentVariable(VersionCodeVar), out int versionCode) && versionCode > 0)
                PlayerSettings.Android.bundleVersionCode = versionCode;
            string versionName = Environment.GetEnvironmentVariable(VersionNameVar);
            if (!string.IsNullOrEmpty(versionName)) PlayerSettings.bundleVersion = versionName;

            Build(appBundle: true);
        }

        private static void Build(bool appBundle)
        {
            string outDir = Environment.GetEnvironmentVariable("PATI_BUILD_DIR");
            if (string.IsNullOrEmpty(outDir))
                outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
            Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, BaseName + (appBundle ? ".aab" : ".apk"));

            // Sahne kurulumu batch modda -buildTarget Android olmadan çalışınca ikonlar boş kalabiliyor
            SceneBuilder.SetAndroidIcons();
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)PlayTargetSdk;

            // Play Store 64 bit ister; eski telefonlar için AAB'ye ARMv7 de eklenir. Test APK'sı tek mimari (daha hızlı)
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = appBundle
                ? AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7
                : AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = appBundle;

            // Sahte satın alma (FakeStore) yalnızca development build'de derlenir; böyle bir AAB asla üretilmesin
            if (appBundle && (EditorUserBuildSettings.development || EditorUserBuildSettings.allowDebugging))
            {
                Debug.LogError("[Pati Dostum] AAB derlenemez: Development Build açık (File > Build Profiles > Development Build). Development build sahte test mağazası içerir; Play'e yalnızca yayın sürümü yüklenir.");
                if (Application.isBatchMode) EditorApplication.Exit(3);
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = outPath,
                target = BuildTarget.Android,
                // PATI_DEV=1 (build-apk.ps1 -Dev): sahte test mağazalı development build; AAB hiçbir zaman
                options = !appBundle && Environment.GetEnvironmentVariable("PATI_DEV") == "1"
                    ? BuildOptions.Development
                    : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded;
            string kind = appBundle ? "AAB" : "APK";

            if (ok)
                Debug.Log($"[Pati Dostum] {kind} hazır: {outPath} (sürüm {PlayerSettings.bundleVersion}, kod {PlayerSettings.Android.bundleVersionCode}, {summary.totalTime.TotalMinutes:0.0} dk)");
            else
                Debug.LogError($"[Pati Dostum] {kind} derlenemedi: {summary.result}, {summary.totalErrors} hata");

            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
            else if (ok) EditorUtility.RevealInFinder(outPath);
        }
    }
}
