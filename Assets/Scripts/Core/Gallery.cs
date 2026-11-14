using System;
using System.IO;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
using AOT;
#endif
using UnityEngine;

namespace PatiDostum
{
    /// <summary>
    /// Foto stüdyosunun çektiği PNG'yi kaydeder: Android'de galeriye (Resimler/Pati Dostum,
    /// Assets/Plugins/Android/PatiGallery.androidlib), iOS'ta Fotoğraflar'a (Assets/Plugins/iOS/PatiGallery.swift), editörde persistentDataPath/photos klasörüne.
    /// Sonuç: ekranda gösterilecek metin anahtarı.
    /// </summary>
    public static class Gallery
    {
        /// <summary>
        /// iOS izin penceresi ya da kayıt bittikten sonra geç gelen sonuç (metin anahtarı). Android ve editörde hiç tetiklenmez;
        /// Foto stüdyosu dinleyip ekranda gösterir.
        /// </summary>
        public static event Action<string> LateResult;

#if UNITY_IOS && !UNITY_EDITOR
        // Assets/Plugins/iOS/PatiGallery.swift (PHPhotoLibrary, yalnızca ekleme izni)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NativeCallback(IntPtr result);

        [DllImport("__Internal")] private static extern void PatiGallery_SetCallback(NativeCallback callback);
        [DllImport("__Internal")] private static extern int PatiGallery_Save(byte[] bytes, int length);

        // Çöp toplayıcı temsilciyi toplamasın diye statik alanda tutulur
        private static readonly NativeCallback nativeCallback = OnNative;
        private static bool callbackSet;
        private static bool waitingForPermission;

        [MonoPInvokeCallback(typeof(NativeCallback))]
        private static void OnNative(IntPtr result)
        {
            string text = Marshal.PtrToStringUTF8(result);
            // İzin zaten varsa "kaydedildi" ekranda hemen gösterilmişti; yinelenmesin
            if (text == "ok" && !waitingForPermission) return;
            waitingForPermission = false;
            LateResult?.Invoke(text == "ok" ? "photo_saved" : text == "denied" ? "photo_denied" : "photo_error");
        }
#endif

        public static string Save(byte[] png, string name)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var gallery = new AndroidJavaClass("com.alpigame.mypawpal.gallery.PatiGallery"))
                {
                    AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    string result = gallery.CallStatic<string>("save", activity, png, name);
                    return result == "ok" ? "photo_saved" : result == "old" ? "photo_old" : "photo_error";
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Fotoğraf kaydedilemedi: " + error.Message);
                return "photo_error";
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                if (!callbackSet)
                {
                    PatiGallery_SetCallback(nativeCallback);
                    callbackSet = true;
                }
                // 0: kaydediliyor (izin var), 1: izin soruluyor (sonuç LateResult ile gelir), 2: izin reddedilmiş
                int status = PatiGallery_Save(png, png.Length);
                waitingForPermission = status == 1;
                return status == 0 ? "photo_saved" : status == 1 ? "photo_saving" : "photo_denied";
            }
            catch (Exception error)
            {
                Debug.LogWarning("Fotoğraf kaydedilemedi: " + error.Message);
                return "photo_error";
            }
#else
            string dir = Path.Combine(Application.persistentDataPath, "photos");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), png);
            Debug.Log("[Pati Dostum] Fotoğraf: " + dir);
            return "photo_saved";
#endif
        }
    }
}
