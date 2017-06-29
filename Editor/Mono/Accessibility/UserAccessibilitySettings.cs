// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Accessibility
{
    internal enum ColorBlindCondition
    {
        Default,
        Deuteranopia,
        Protanopia,
        Tritanopia,
    }

    // NOTE: The preferences in this class are currently only exposed via a context menu in the ProfilerWindow
    // these toggles need to instead be moved to e.g., the Preferences menu before they are used elsewhere
    internal static class UserAccessiblitySettings
    {
        static UserAccessiblitySettings()
        {
            s_ColorBlindCondition = (ColorBlindCondition)EditorPrefs.GetInt(k_ColorBlindConditionPrefKey, (int)ColorBlindCondition.Default);
        }

        private const string k_ColorBlindConditionPrefKey = "AccessibilityColorBlindCondition";

        public static ColorBlindCondition colorBlindCondition
        {
            get { return s_ColorBlindCondition; }
            set
            {
                if (s_ColorBlindCondition != value)
                {
                    s_ColorBlindCondition = value;
                    EditorPrefs.SetInt(k_ColorBlindConditionPrefKey, (int)value);
                    if (colorBlindConditionChanged != null)
                        colorBlindConditionChanged();
                }
            }
        }
        private static ColorBlindCondition s_ColorBlindCondition;

        public static Action colorBlindConditionChanged;
    }
}
