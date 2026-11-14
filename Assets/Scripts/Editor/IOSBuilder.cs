using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#endif

namespace PatiDostum.EditorTools
{
    /// <summary>
    /// iOS derlemesi: yalnızca Xcode projesini üretir (GitHub Actions, .github/workflows/ios.yml). Arşiv, imza ve
    /// App Store Connect yüklemesi macOS işinde xcodebuild ile yapılır; bu makinede hiçbir şey derlenmez.
    /// Komut satırı: -buildTarget iOS -executeMethod PatiDostum.EditorTools.IOSBuilder.Build
    /// Girdiler (önce komut satırı bağımsız değişkeni, sonra ortam değişkeni):
    ///   -customBuildPath / PATI_IOS_BUILD_DIR   Xcode projesinin çıkacağı klasör (game-ci -customBuildPath verir)
    ///   -iosBuildNumber  / IOS_BUILD_NUMBER      CFBundleVersion (varsayılan 1)
    ///   -iosVersion      / IOS_BUNDLE_VERSION    CFBundleShortVersionString (yoksa projedeki bundleVersion)
    ///   -appleTeamId     / APPLE_TEAM_ID         isteğe bağlı; yoksa xcodebuild DEVELOPMENT_TEAM ile verilir
    /// </summary>
    public static class IOSBuilder
    {
        public const string BundleId = SceneBuilder.PackageName;
        public const string DisplayNameEn = "My Paw Pal";
        public const string DisplayNameTr = "Pati Dostum";
        // Xcode ürün adı boşluk ve kesme işareti taşımasın; telefonda görünen ad Info.plist'ten gelir
        private const string BuildProductName = "MyPawPal";
        private const string TargetIos = "15.0";
        private const string IconSource = "Art/Icon/app_icon.png";

        private const string UsageEn = "My Paw Pal saves the photos you take in the photo studio to your Photos library.";
        private const string UsageTr = "Pati Dostum, foto stüdyosunda çektiğin fotoğrafları Fotoğraflar'a kaydeder.";

        public static void Build()
        {
            string outDir = Argument("-customBuildPath") ?? Environment.GetEnvironmentVariable("PATI_IOS_BUILD_DIR");
            if (string.IsNullOrEmpty(outDir))
                outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "iOS");
            Directory.CreateDirectory(outDir);

            string buildNumber = Argument("-iosBuildNumber") ?? Environment.GetEnvironmentVariable("IOS_BUILD_NUMBER");
            if (string.IsNullOrEmpty(buildNumber)) buildNumber = "1";
            if (!Regex.IsMatch(buildNumber, @"^\d+(\.\d+){0,2}$"))
            {
                Fail($"Geçersiz build numarası: '{buildNumber}' (yalnızca rakam ve nokta, ör. 42).", 2);
                return;
            }

            string version = Argument("-iosVersion") ?? Environment.GetEnvironmentVariable("IOS_BUNDLE_VERSION");
            if (!string.IsNullOrEmpty(version))
            {
                if (!Regex.IsMatch(version, @"^\d+(\.\d+){0,2}$"))
                {
                    Fail($"Geçersiz sürüm: '{version}' (ör. 1.0.0).", 2);
                    return;
                }
                PlayerSettings.bundleVersion = version;
            }

            // Sahte test mağazası (FakeStore) yalnızca development build'de derlenir; App Store'a böylesi gitmesin
            if (EditorUserBuildSettings.development || EditorUserBuildSettings.allowDebugging)
            {
                Fail("iOS derlemesi durdu: Development Build açık (sahte test mağazası içerir).", 3);
                return;
            }

            ApplySettings(buildNumber);

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = outDir,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };
            if (options.scenes.Length == 0)
            {
                Fail("Derlenecek sahne yok (Build Settings boş). Önce Pati Dostum > Sahneleri Kur çalışmalı.", 4);
                return;
            }

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded;
            if (ok)
                Debug.Log($"[Pati Dostum] iOS Xcode projesi hazır: {outDir} (sürüm {PlayerSettings.bundleVersion}, build {buildNumber}, {summary.totalTime.TotalMinutes:0.0} dk)");
            else
                Debug.LogError($"[Pati Dostum] iOS derlenemedi: {summary.result}, {summary.totalErrors} hata");

            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        private static void ApplySettings(string buildNumber)
        {
            PlayerSettings.productName = BuildProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.iOS.buildNumber = buildNumber;
            PlayerSettings.iOS.targetOSVersionString = TargetIos;
            // iPad ek ekran görüntüsü ve yön kuralları ister; dikey telefon oyunu yalnızca iPhone olarak yayınlanır
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;

            // Android ile aynı: dikey, tek elle oynanır
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);   // iOS yalnızca ARM64

            // Otomatik imza: takım kimliğini xcodebuild DEVELOPMENT_TEAM ile de verir; burada yalnızca bilinirse yazılır
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            string team = Argument("-appleTeamId") ?? Environment.GetEnvironmentVariable("APPLE_TEAM_ID");
            if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;

            // Bildirim izni oyun içinde (ilk sahiplenmede) istenir; paket açılışta sormasın, push yeteneği eklemesin
            Unity.Notifications.NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = false;
            Unity.Notifications.NotificationSettings.iOSSettings.AddRemoteNotificationCapability = false;

            StoreSettings.Apply();
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        private static void Fail(string message, int exitCode)
        {
            Debug.LogError("[Pati Dostum] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
        }

#if UNITY_IOS
        /// <summary>Xcode projesi yazıldıktan sonra: Info.plist, yerelleştirme, çerçeveler, Swift ve ikon.</summary>
        [PostProcessBuild(1000)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            PatchInfoPlist(path);
            WriteLocalization(path);
            PatchProject(path);
            ReplaceAppIcon(path);
            Debug.Log("[Pati Dostum] iOS son işlemleri tamam (Info.plist, tr.lproj, StoreKit/Photos, Swift, ikon).");
        }

        private static void PatchInfoPlist(string path)
        {
            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict root = plist.root;

            // Yalnızca Apple'ın muaf şifrelemesi (HTTPS) var; her yüklemede dışa aktarım sorusu çıkmasın
            root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            root.SetString("CFBundleDevelopmentRegion", "en");
            root.SetString("CFBundleDisplayName", DisplayNameEn);
            root.SetString("NSPhotoLibraryAddUsageDescription", UsageEn);

            PlistElementArray localizations = root.CreateArray("CFBundleLocalizations");
            localizations.AddString("en");
            localizations.AddString("tr");

            // Arka plan modu kullanılmaz (bildirimler yereldir, push yok)
            if (root.values.ContainsKey("UIBackgroundModes"))
            {
                root.values.Remove("UIBackgroundModes");
                Debug.Log("[Pati Dostum] UIBackgroundModes kaldırıldı.");
            }

            plist.WriteToFile(plistPath);
        }

        /// <summary>tr.lproj/InfoPlist.strings: Türkçe telefonda ad "Pati Dostum" ve Türkçe fotoğraf izni metni.</summary>
        private static void WriteLocalization(string path)
        {
            string dir = Path.Combine(path, "tr.lproj");
            Directory.CreateDirectory(dir);
            string content =
                $"\"CFBundleDisplayName\" = \"{DisplayNameTr}\";\n" +
                $"\"NSPhotoLibraryAddUsageDescription\" = \"{UsageTr}\";\n";
            File.WriteAllText(Path.Combine(dir, "InfoPlist.strings"), content, new System.Text.UTF8Encoding(false));
        }

        private static void PatchProject(string path)
        {
            string projectPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            string main = project.GetUnityMainTargetGuid();
            string framework = project.GetUnityFrameworkTargetGuid();

            // Swift eklentileri (StoreKit 2 ve Fotoğraflar köprüsü) UnityFramework hedefinde derlenir
            project.AddFrameworkToProject(framework, "StoreKit.framework", false);
            project.AddFrameworkToProject(framework, "Photos.framework", false);
            project.SetBuildProperty(framework, "SWIFT_VERSION", "5.0");
            project.SetBuildProperty(framework, "CLANG_ENABLE_MODULES", "YES");
            project.SetBuildProperty(main, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            project.SetBuildProperty(main, "SWIFT_VERSION", "5.0");

            // tr.lproj klasörü pakete olduğu gibi kopyalanır
            string reference = project.AddFolderReference(Path.Combine(path, "tr.lproj"), "tr.lproj");
            project.AddFileToBuild(main, reference);

            project.WriteToFile(projectPath);
        }

        /// <summary>
        /// AppIcon.appiconset tek 1024x1024 ikonla yeniden yazılır (Xcode 14+ tüm boyutları üretir). App Store alfa kanalı
        /// olan ikonu reddeder; kaynak alfalıysa beyaz zemine yassıltılır.
        /// </summary>
        private static void ReplaceAppIcon(string path)
        {
            string[] sets = Directory.GetDirectories(path, "AppIcon.appiconset", SearchOption.AllDirectories);
            if (sets.Length == 0)
            {
                Debug.LogWarning("[Pati Dostum] AppIcon.appiconset bulunamadı; ikon değiştirilmedi.");
                return;
            }

            string source = Path.Combine(Application.dataPath, IconSource);
            if (!File.Exists(source))
                throw new BuildFailedException("iOS ikonu yok: Assets/" + IconSource);
            byte[] png = File.ReadAllBytes(source);
            if (HasAlpha(png)) png = Flatten(png);
            if (HasAlpha(png))
                throw new BuildFailedException("iOS ikonu alfa kanalından arındırılamadı: Assets/" + IconSource);

            foreach (string set in sets)
            {
                foreach (string file in Directory.GetFiles(set)) File.Delete(file);
                File.WriteAllBytes(Path.Combine(set, "icon-1024.png"), png);
                File.WriteAllText(Path.Combine(set, "Contents.json"),
                    "{\n  \"images\" : [\n    {\n      \"filename\" : \"icon-1024.png\",\n      \"idiom\" : \"universal\",\n" +
                    "      \"platform\" : \"ios\",\n      \"size\" : \"1024x1024\"\n    }\n  ],\n" +
                    "  \"info\" : {\n    \"author\" : \"xcode\",\n    \"version\" : 1\n  }\n}\n");
            }
        }

        /// <summary>PNG IHDR renk türü: 4 (gri+alfa) ve 6 (RGBA) alfalıdır; tRNS parçası da saydamlık demektir.</summary>
        private static bool HasAlpha(byte[] png)
        {
            if (png.Length < 33) return true;
            byte colorType = png[25];
            if (colorType == 4 || colorType == 6) return true;
            return System.Text.Encoding.ASCII.GetString(png).Contains("tRNS");
        }

        private static byte[] Flatten(byte[] png)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(png);
            Color32[] pixels = source.GetPixels32();
            var flat = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                float a = pixels[i].a / 255f;
                flat[i] = new Color32(
                    (byte)Mathf.Round(pixels[i].r * a + 255f * (1f - a)),
                    (byte)Mathf.Round(pixels[i].g * a + 255f * (1f - a)),
                    (byte)Mathf.Round(pixels[i].b * a + 255f * (1f - a)), 255);
            }
            var rgb = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            rgb.SetPixels32(flat);
            rgb.Apply();
            byte[] result = rgb.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(rgb);
            Debug.Log("[Pati Dostum] iOS ikonunun alfa kanalı kaldırıldı.");
            return result;
        }
#endif
    }
}
