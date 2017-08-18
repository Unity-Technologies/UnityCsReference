// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
using System.Collections;

namespace UnityEngine.iOS
{
    // must be kept in sync with CalendarIdentifier in Notifications.h
    public enum CalendarIdentifier
    {
        GregorianCalendar   = 0,
        BuddhistCalendar    = 1,
        ChineseCalendar     = 2,
        HebrewCalendar      = 3,
        IslamicCalendar     = 4,
        IslamicCivilCalendar = 5,
        JapaneseCalendar    = 6,
        RepublicOfChinaCalendar = 7,
        PersianCalendar     = 8,
        IndianCalendar      = 9,
        ISO8601Calendar     = 10,
    }

    // values are taken from NSCalendarUnit/CFCalendarUnit
    public enum CalendarUnit
    {
        Era     = 2,
        Year    = 4,
        Month   = 8,
        Day     = 16,
        Hour    = 32,
        Minute  = 64,
        Second  = 128,
        Week    = 256,
        Weekday = 512,
        WeekdayOrdinal = 1024,
        Quarter = 2048,
    }

    // values are taken from UIUserNotificationType
    public enum NotificationType
    {
        None  = 0,
        Badge = 1,
        Sound = 2,
        Alert = 4,
    }

    //[RequiredByNativeCode]
    [NativeHeader("PlatformDependent/iPhonePlayer/Notifications.h")]
    internal sealed partial class NotificationHelper
    {
        [FreeFunction("NotificationScripting::CreateLocal")] extern internal static IntPtr CreateLocal();
        [NativeMethod(Name = "NotificationScripting::DestroyLocal", IsFreeFunction = true, IsThreadSafe = true)] extern internal static void DestroyLocal(IntPtr target);

        //[FreeFunction("NotificationScripting::CreateRemote")] extern internal static IntPtr CreateRemote();
        [NativeMethod(Name = "NotificationScripting::DestroyRemote", IsFreeFunction = true, IsThreadSafe = true)] extern internal static void DestroyRemote(IntPtr target);
    }

    // TODO: should we make it IDisposable?
    [RequiredByNativeCode]
    [NativeHeader("PlatformDependent/iPhonePlayer/Notifications.h")]
    [NativeConditional("PLATFORM_IPHONE")]
    public sealed partial class LocalNotification
    {
        #pragma warning disable 169, 649
        private IntPtr m_Ptr;

        public LocalNotification() { m_Ptr = NotificationHelper.CreateLocal(); }
        ~LocalNotification() { NotificationHelper.DestroyLocal(m_Ptr); }


        extern public string timeZone { get; set; }
        extern public CalendarIdentifier repeatCalendar
        {
            get;
            [NativeMethod(Name = "NotificationScripting::SetRepeatCalendar", IsFreeFunction = true, HasExplicitThis = true)] set;
        }

        extern public CalendarUnit repeatInterval { get; set; }

        [NativeProperty("FireDate")] extern private double fireDateImpl { get; set; }
        private static long m_NSReferenceDateTicks = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        public DateTime fireDate
        {
            get { return new DateTime((long)(fireDateImpl * 10000000) + m_NSReferenceDateTicks); }
            set { fireDateImpl = (value.ToUniversalTime().Ticks - m_NSReferenceDateTicks) / 10000000.0; }
        }

        extern public string alertBody                  { get; set; }
        extern public string alertAction                { get; set; }
        extern public string alertLaunchImage           { get; set; }
        extern public string soundName                  { get; set; }
        extern public int    applicationIconBadgeNumber { get; set; }

        extern public static string defaultSoundName    { get; }

        extern public IDictionary userInfo              { get; set; }

        public extern bool hasAction {[NativeName("HasAction")] get; [NativeName("HasAction")] set; }


        extern internal void Schedule();
        extern internal void PresentNow();
        extern internal void Cancel();
    }

    // TODO: should we make it IDisposable?
    [RequiredByNativeCode]
    [NativeHeader("PlatformDependent/iPhonePlayer/Notifications.h")]
    [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
    public sealed partial class RemoteNotification
    {
        #pragma warning disable 169, 649
        private IntPtr m_Ptr;

        private RemoteNotification()  {}
        ~RemoteNotification() { NotificationHelper.DestroyRemote(m_Ptr); }

        extern public string alertBody                  { get; }
        extern public string soundName                  { get; }
        extern public int    applicationIconBadgeNumber { get; }
        extern public IDictionary userInfo              { get; }

        public extern bool hasAction {[NativeName("HasAction")] get; }
    }

    [NativeHeader("PlatformDependent/iPhonePlayer/Notifications.h")]
    [NativeConditional("PLATFORM_IPHONE || PLATFORM_TVOS")]
    public sealed partial class NotificationServices
    {
        extern public static int localNotificationCount  {[FreeFunction("NotificationScripting::GetLocalCount")]  get; }
        extern public static int remoteNotificationCount {[FreeFunction("NotificationScripting::GetRemoteCount")] get; }

        [FreeFunction("NotificationScripting::ClearLocal")]  extern public static void ClearLocalNotifications();
        [FreeFunction("NotificationScripting::ClearRemote")] extern public static void ClearRemoteNotifications();

        // we search for references to RegisterForNotifications method to check if remote notifications are actually used in the project
        // at first we had 2 RegisterForNotifications overloads and one was calling other (as we dont yet use default arguments)
        // this will be registered as "method referenced".
        // thats why we implement both overloads in terms of differently-named custom method, but there is a catch
        // when searching "referenced methods" list we do check *Contains(name)*, so, for example,
        // if we call RegisterForNotificationsImpl and search for references for RegisterForNotifications it will be found
        // check AssemblyReferenceChecker.HasReferenceToMethod
        // i am not quite ready to change implementation now so i am doing this ugly hack
        [FreeFunction("RegisterForNotifications")] extern internal static void Internal_RegisterImpl(NotificationType notificationTypes, bool registerForRemote);

        public static void RegisterForNotifications(NotificationType notificationTypes)
        {
            Internal_RegisterImpl(notificationTypes, true);
        }

        public static void RegisterForNotifications(NotificationType notificationTypes, bool registerForRemote)
        {
            Internal_RegisterImpl(notificationTypes, registerForRemote);
        }

        extern public static NotificationType enabledNotificationTypes {[FreeFunction("GetEnabledNotificationTypes")] get; }


        public static void ScheduleLocalNotification(LocalNotification notification)    { notification.Schedule(); }
        public static void PresentLocalNotificationNow(LocalNotification notification)  { notification.PresentNow(); }
        public static void CancelLocalNotification(LocalNotification notification)      { notification.Cancel(); }

        [FreeFunction("iPhoneLocalNotification::CancelAll")] extern public static void CancelAllLocalNotifications();
        [FreeFunction("iPhoneRemoteNotification::Unregister")] extern public static void UnregisterForRemoteNotifications();

        extern public static string registrationError {[FreeFunction("iPhoneRemoteNotification::GetError")] get; }
        extern public static byte[] deviceToken {[FreeFunction("NotificationScripting::GetDeviceToken")] get; }

        [FreeFunction("NotificationScripting::GetLocal")] extern static internal LocalNotification GetLocalNotificationImpl(int index);
        public static LocalNotification GetLocalNotification(int index)
        {
            if (index < 0 || index >= localNotificationCount)
                throw new ArgumentOutOfRangeException("index", "Index out of bounds.");
            return GetLocalNotificationImpl(index);
        }

        public static LocalNotification[] localNotifications
        {
            get
            {
                int count = localNotificationCount;
                LocalNotification[] notifications = new LocalNotification[count];
                for (int i = 0; i < count; ++i)
                    notifications[i] = GetLocalNotificationImpl(i);
                return notifications;
            }
        }

        [FreeFunction("NotificationScripting::GetRemote")] extern static internal RemoteNotification GetRemoteNotificationImpl(int index);
        public static RemoteNotification GetRemoteNotification(int index)
        {
            if (index < 0 || index >= remoteNotificationCount)
                throw new ArgumentOutOfRangeException("index", "Index out of bounds.");
            return GetRemoteNotificationImpl(index);
        }

        public static RemoteNotification[] remoteNotifications
        {
            get
            {
                int count = remoteNotificationCount;
                RemoteNotification[] notifications = new RemoteNotification[count];
                for (int i = 0; i < count; ++i)
                    notifications[i] = GetRemoteNotificationImpl(i);
                return notifications;
            }
        }

        extern public static LocalNotification[] scheduledLocalNotifications {[FreeFunction("NotificationScripting::GetScheduledLocal")] get; }
    }
}
