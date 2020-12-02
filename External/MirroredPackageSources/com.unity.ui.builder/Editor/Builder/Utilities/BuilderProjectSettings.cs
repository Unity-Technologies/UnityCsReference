using System;
using UnityEditor;

namespace Unity.UI.Builder
{
    static class BuilderProjectSettings
    {
        const string k_EditorExtensionModeKey = "UIBuilder.EditorExtensionModeKey";
        const string k_HideNotificationAboutMissingUITKPackage = "UIBuilder.HideNotificationAboutMissingUITKPackage";
        const string k_DisableMouseWheelZooming = "UIBuilder.DisableMouseWheelZooming";
        const string k_EnableAbsolutePositionPlacement = "UIBuilder.EnableAbsolutePositionPlacement";

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

        public static bool hideNotificationAboutMissingUITKPackage
        {
            get => GetBool(k_HideNotificationAboutMissingUITKPackage);
            set => SetBool(k_HideNotificationAboutMissingUITKPackage, value);
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
            EditorUserSettings.SetConfigValue(k_HideNotificationAboutMissingUITKPackage, null);
            EditorUserSettings.SetConfigValue(k_EnableAbsolutePositionPlacement, null);
        }
    }
}
