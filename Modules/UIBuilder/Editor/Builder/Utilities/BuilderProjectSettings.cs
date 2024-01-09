// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.UI.Builder
{
    static class BuilderProjectSettings
    {
        const string k_EditorExtensionModeKey = "UIBuilder.EditorExtensionModeKey";
        const string k_DisableMouseWheelZooming = "UIBuilder.DisableMouseWheelZooming";
        const string k_EnableAbsolutePositionPlacement = "UIBuilder.EnableAbsolutePositionPlacement";
        const string k_BlockedNotifications = "UIBuilder.ProjectBlockedNotifications";

        public static bool enableEditorExtensionModeByDefault
        {
            get => GetBool(k_EditorExtensionModeKey);
            set => SetBool(k_EditorExtensionModeKey, value);
        }

        public static bool disableMouseWheelZooming
        {
            get => GetBool(k_DisableMouseWheelZooming);
            set => SetBool(k_DisableMouseWheelZooming, value);
        }

        public static bool enableAbsolutePositionPlacement
        {
            get => Unsupported.IsDeveloperMode() && GetBool(k_EnableAbsolutePositionPlacement);
            set => SetBool(k_EnableAbsolutePositionPlacement, value);
        }

        static bool GetBool(string name)
        {
            var value = EditorUserSettings.GetConfigValue(name);
            if (string.IsNullOrEmpty(value))
                return false;

            return Convert.ToBoolean(value);
        }

        static void SetBool(string name, bool value)
        {
            EditorUserSettings.SetConfigValue(name, value.ToString());
        }

        internal static void Reset()
        {
            EditorUserSettings.SetConfigValue(k_EditorExtensionModeKey, null);
            EditorUserSettings.SetConfigValue(k_EnableAbsolutePositionPlacement, null);
            ResetNotifications();
        }

        public static void BlockNotification(string notificationKey)
        {
            var blockedNotifications = EditorUserSettings.GetConfigValue(k_BlockedNotifications);

            var blockedNotificationsList = string.IsNullOrEmpty(blockedNotifications) ? new List<string>() : new List<string>(blockedNotifications.Split(","));

            if (!blockedNotificationsList.Contains(notificationKey))
            {
                blockedNotificationsList.Add(notificationKey);
            }

            var newStringArray = string.Join(",", blockedNotificationsList);
            EditorUserSettings.SetConfigValue(k_BlockedNotifications, newStringArray);
        }

        public static void ResetNotifications()
        {
            EditorUserSettings.SetConfigValue(k_BlockedNotifications, null);
        }

        public static bool HasBlockedNotifications()
        {
            var blockedNotifications = EditorUserSettings.GetConfigValue(k_BlockedNotifications);
            if (!string.IsNullOrEmpty(blockedNotifications))
            {
                var blockedNotificationsList = new List<string>(blockedNotifications.Split(","));
                return blockedNotificationsList.Count > 0;
            }

            return false;
        }

        public static bool IsNotificationBlocked(string notificationKey)
        {
            var blockedNotifications = EditorUserSettings.GetConfigValue(k_BlockedNotifications);

            if (!string.IsNullOrEmpty(blockedNotifications))
            {
                var blockedNotificationsList = new List<string>(blockedNotifications.Split(","));
                return blockedNotificationsList.Contains(notificationKey);
            }

            return false;
        }
    }
}
