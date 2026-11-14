using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

namespace PatiDostum
{
    /// <summary>
    /// Yerel bildirimler (Unity Mobile Notifications; internet gerekmez). Ne zaman ne gideceğini
    /// NotificationPlanner hesaplar; burası yalnızca Android ve iOS'a iletir. Editörde hiçbir şey yapmaz.
    /// </summary>
    public static class Notifications
    {
        private const string Channel = "pets";

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool ready;

        private static void Init()
        {
            if (ready) return;
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(
                Channel, Loc.Get("notify_channel"), Loc.Get("notify_channel_desc"), Importance.Default));
            ready = true;
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        // İzin penceresi sorulurken istek nesnesi canlı tutulur
        private static AuthorizationRequest authorization;
#endif

        /// <summary>Android 13+ bildirim izni (ilk sahiplenmede ve ayar açılınca istenir).</summary>
        public static void RequestPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Init();
                if (AndroidNotificationCenter.UserPermissionToPost != PermissionStatus.Allowed)
                    new PermissionRequest();
            }
            catch (Exception error)
            {
                Debug.LogWarning("Bildirim izni istenemedi: " + error.Message);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                // Yalnızca ilk kez sorulur; sonraki çağrılarda sistem zaten kararı biliyor
                if (authorization == null
                    && iOSNotificationCenter.GetNotificationSettings().AuthorizationStatus == AuthorizationStatus.NotDetermined)
                    authorization = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Sound, false);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Bildirim izni istenemedi: " + error.Message);
            }
#endif
        }

        public static void Schedule(List<PlannedNotification> plan)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Init();
                AndroidNotificationCenter.CancelAllNotifications();
                foreach (PlannedNotification n in plan)
                {
                    var notification = new AndroidNotification(Loc.Get("title"), Loc.Format(n.bodyKey, n.petName), n.at);
                    AndroidNotificationCenter.SendNotification(notification, Channel);
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Bildirim planlanamadı: " + error.Message);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                iOSNotificationCenter.RemoveAllScheduledNotifications();
                iOSNotificationCenter.RemoveAllDeliveredNotifications();
                // İzin verilmediyse sistem zaten gösterme; yine de planlamak zararsız
                DateTime now = DateTime.Now;
                foreach (PlannedNotification n in plan)
                {
                    TimeSpan wait = n.at - now;
                    if (wait.TotalSeconds < 2.0) continue;   // zaman aralığı tetikleyicisi en az 1 sn ister
                    iOSNotificationCenter.ScheduleNotification(new iOSNotification
                    {
                        Title = Loc.Get("title"),
                        Body = Loc.Format(n.bodyKey, n.petName),
                        ShowInForeground = false,
                        Trigger = new iOSNotificationTimeIntervalTrigger { TimeInterval = wait, Repeats = false }
                    });
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Bildirim planlanamadı: " + error.Message);
            }
#endif
        }

        public static void CancelAll()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidNotificationCenter.CancelAllNotifications();
            }
            catch (Exception error)
            {
                Debug.LogWarning("Bildirimler iptal edilemedi: " + error.Message);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                iOSNotificationCenter.RemoveAllScheduledNotifications();
                iOSNotificationCenter.RemoveAllDeliveredNotifications();
            }
            catch (Exception error)
            {
                Debug.LogWarning("Bildirimler iptal edilemedi: " + error.Message);
            }
#endif
        }
    }
}
