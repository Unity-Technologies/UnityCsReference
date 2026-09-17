// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;

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
    internal static partial class UserAccessiblitySettings
    {
        [OnCodeLoaded]
        static void Initialize()
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
        // Editor preference mirror (value-type enum); re-seeded from EditorPrefs in Initialize and safe to persist across reload.
        [NoAutoStaticsCleanup]
        private static ColorBlindCondition s_ColorBlindCondition;

        [AutoStaticsCleanupOnCodeReload]
        // Subscribers attach through their own lifecycle and re-subscribe after a code reload, so the
        // cleared invocation list refills itself.
        [IgnoreForUAL0015("Event whose subscribers re-register through their own lifecycle after a code reload")]
        public static Action colorBlindConditionChanged;
    }
}
