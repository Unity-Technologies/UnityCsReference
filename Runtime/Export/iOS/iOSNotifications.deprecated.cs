// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;

namespace UnityEngine
{
    [Obsolete("CalendarIdentifier is deprecated. Please use iOS.CalendarIdentifier instead (UnityUpgradable) -> UnityEngine.iOS.CalendarIdentifier", true)]
    public enum CalendarIdentifier
    {
        GregorianCalendar,
        BuddhistCalendar,
        ChineseCalendar,
        HebrewCalendar,
        IslamicCalendar,
        IslamicCivilCalendar,
        JapaneseCalendar,
        RepublicOfChinaCalendar,
        PersianCalendar,
        IndianCalendar,
        ISO8601Calendar
    }

    [Obsolete("CalendarUnit is deprecated. Please use iOS.CalendarUnit instead (UnityUpgradable) -> UnityEngine.iOS.CalendarUnit", true)]
    public enum CalendarUnit
    {
        Era,
        Year,
        Month,
        Day,
        Hour,
        Minute,
        Second,
        Week,
        Weekday,
        WeekdayOrdinal,
        Quarter
    }

    [Obsolete("LocalNotification is deprecated. Please use iOS.LocalNotification instead (UnityUpgradable) -> UnityEngine.iOS.LocalNotification", true)]
    public sealed class LocalNotification
    {
        public DateTime fireDate { get { return default(DateTime); } set {} }
        public string timeZone { get { return default(string); } set {} }
        public CalendarUnit repeatInterval { get { return default(CalendarUnit); } set {} }
        public CalendarIdentifier repeatCalendar { get { return default(CalendarIdentifier); } set {} }
        public string alertBody { get { return default(string); } set {} }
        public string alertAction { get { return default(string); } set {} }
        public bool hasAction { get { return default(bool); } set {} }
        public string alertLaunchImage { get { return default(string); } set {} }
        public int applicationIconBadgeNumber { get { return default(int); } set {} }
        public string soundName { get { return default(string); } set {} }
        public static string defaultSoundName { get { return default(string); } }
        public IDictionary userInfo { get { return default(IDictionary); } set {} }

        public LocalNotification() {}
    }

    [Obsolete("RemoteNotification is deprecated. Please use iOS.RemoteNotification instead (UnityUpgradable) -> UnityEngine.iOS.RemoteNotification", true)]
    public sealed class RemoteNotification
    {
        public string alertBody { get { return default(string); } }
        public bool hasAction { get { return default(bool); } }
        public int applicationIconBadgeNumber { get { return default(int); } }
        public string soundName { get { return default(string); } }
        public IDictionary userInfo { get { return default(IDictionary); } }
    }

    [Obsolete("RemoteNotificationType is deprecated. Please use iOS.NotificationType instead (UnityUpgradable) -> UnityEngine.iOS.NotificationType", true)]
    public enum RemoteNotificationType
    {
        None,
        Badge,
        Sound,
        Alert
    }

    [Obsolete("NotificationServices is deprecated. Please use iOS.NotificationServices instead (UnityUpgradable) -> UnityEngine.iOS.NotificationServices", true)]
    public sealed class NotificationServices
    {
        public NotificationServices()
        {
        }

        [Obsolete("RegisterForRemoteNotificationTypes is deprecated. Please use RegisterForNotifications instead (UnityUpgradable) -> UnityEngine.iOS.NotificationServices.RegisterForNotifications(*)", true)]
        public static void RegisterForRemoteNotificationTypes(RemoteNotificationType notificationTypes) {}
    }
}

