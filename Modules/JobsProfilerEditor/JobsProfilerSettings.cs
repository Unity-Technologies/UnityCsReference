// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.JobsProfiling;

/// <summary>
/// Theme colors and color-blind mode support for the Jobs Profiler.
/// This class uses reflection and should NOT be accessed from Burst-compiled code.
/// </summary>
[InitializeOnLoad]
internal static class JobsProfilerSettings
{
    static JobsProfilerSettings()
    {
        InitializeColorBlindReflection();
    }

    #region Color Blind Mode Support (via reflection - APIs are internal)

    [NoAutoStaticsCleanup] static Type s_ProfilerColorsType;
    [NoAutoStaticsCleanup] static Color[] s_DefaultColors;
    [NoAutoStaticsCleanup] static Color[] s_ColorBlindSafeColors;
    [NoAutoStaticsCleanup] static bool s_ColorBlindReflectionInitialized;
    [NoAutoStaticsCleanup] static bool s_IsColorBlindMode;
    [NoAutoStaticsCleanup] static bool s_ColorBlindModeSubscribed;
    [NoAutoStaticsCleanup] static PropertyInfo s_ColorBlindConditionProperty;

    static void InitializeColorBlindReflection()
    {
        if (s_ColorBlindReflectionInitialized)
            return;
        s_ColorBlindReflectionInitialized = true;

        var editorAssembly = typeof(Editor).Assembly;
        const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        // Access UserAccessiblitySettings for color blind mode detection
        var userAccessibilitySettingsType = editorAssembly.GetType("UnityEditor.Accessibility.UserAccessiblitySettings");
        if (userAccessibilitySettingsType != null)
        {
            s_ColorBlindConditionProperty = userAccessibilitySettingsType.GetProperty("colorBlindCondition", staticFlags);
            if (s_ColorBlindConditionProperty != null)
            {
                // Cache initial value (ColorBlindCondition.Default = 0)
                s_IsColorBlindMode = (int)s_ColorBlindConditionProperty.GetValue(null) != 0;
            }

            // Subscribe to changes to update cached value
            var changedEvent = userAccessibilitySettingsType.GetEvent("colorBlindConditionChanged", staticFlags);
            if (changedEvent != null && !s_ColorBlindModeSubscribed)
            {
                s_ColorBlindModeSubscribed = true;
                changedEvent.AddEventHandler(null, (Action)OnColorBlindConditionChanged);
            }
        }

        // Access ProfilerColors for color-blind safe color mapping
        s_ProfilerColorsType = typeof(UnityEditorInternal.ProfilerDriver).Assembly.GetType("UnityEditorInternal.ProfilerColors");
        if (s_ProfilerColorsType != null)
        {
            var defaultColorsField = s_ProfilerColorsType.GetField("s_DefaultColors", staticFlags);
            var colorBlindColorsField = s_ProfilerColorsType.GetField("s_ColorBlindSafeColors", staticFlags);

            s_DefaultColors = defaultColorsField?.GetValue(null) as Color[];
            s_ColorBlindSafeColors = colorBlindColorsField?.GetValue(null) as Color[];
        }
    }

    static void OnColorBlindConditionChanged()
    {
        if (s_ColorBlindConditionProperty != null)
            s_IsColorBlindMode = (int)s_ColorBlindConditionProperty.GetValue(null) != 0;
    }

    /// <summary>
    /// Returns true if color blind mode is enabled in Unity's accessibility settings.
    /// </summary>
    internal static bool IsColorBlindMode
    {
        get
        {
            InitializeColorBlindReflection();
            if (s_ColorBlindConditionProperty != null)
                s_IsColorBlindMode = (int)s_ColorBlindConditionProperty.GetValue(null) != 0;
            return s_IsColorBlindMode;
        }
    }

    /// <summary>
    /// Event fired when color blind mode setting changes.
    /// </summary>
    internal static event Action ColorBlindModeChanged
    {
        add
        {
            InitializeColorBlindReflection();
            var editorAssembly = typeof(Editor).Assembly;
            var userAccessibilitySettingsType = editorAssembly.GetType("UnityEditor.Accessibility.UserAccessiblitySettings");
            var changedEvent = userAccessibilitySettingsType?.GetEvent("colorBlindConditionChanged",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            changedEvent?.AddEventHandler(null, value);
        }
        remove
        {
            var editorAssembly = typeof(Editor).Assembly;
            var userAccessibilitySettingsType = editorAssembly.GetType("UnityEditor.Accessibility.UserAccessiblitySettings");
            var changedEvent = userAccessibilitySettingsType?.GetEvent("colorBlindConditionChanged",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            changedEvent?.RemoveEventHandler(null, value);
        }
    }

    /// <summary>
    /// Gets the color-blind safe equivalent for a given profiler category color.
    /// Returns null if no mapping is found or reflection failed.
    /// </summary>
    internal static Color? GetColorBlindSafeColor(Color defaultColor)
    {
        InitializeColorBlindReflection();

        if (s_DefaultColors == null || s_ColorBlindSafeColors == null)
            return null;

        int count = Math.Min(s_DefaultColors.Length, s_ColorBlindSafeColors.Length);
        for (int i = 0; i < count; i++)
        {
            if (ColorsApproximatelyEqual(defaultColor, s_DefaultColors[i]))
                return s_ColorBlindSafeColors[i];
        }
        return null;
    }

    static bool ColorsApproximatelyEqual(Color a, Color b)
    {
        const float tolerance = 0.01f;
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }

    #endregion

    #region Theme Colors

    // Colors read from USS for C# rendering code (text, mesh generation)
    [NoAutoStaticsCleanup] static Color s_BarTextColor;
    [NoAutoStaticsCleanup] static Color s_TimelineBackground;
    [NoAutoStaticsCleanup] static Color s_ThreadSeparatorLine;
    [NoAutoStaticsCleanup] static Color s_LinkColor;

    // Blending colors (hardcoded - used for color blending operations in rendering)
    [NoAutoStaticsCleanup] static Color s_SelectionBlendColor = Color.white;
    [NoAutoStaticsCleanup] static Color s_DeselectionBlendColor = Color.black;

    // CustomStyleProperties for reading USS variables
    static readonly CustomStyleProperty<Color> s_BarTextColorProperty = new CustomStyleProperty<Color>("--bar-text-color");
    static readonly CustomStyleProperty<Color> s_TimelineBackgroundProperty = new CustomStyleProperty<Color>("--timeline-background-color");
    static readonly CustomStyleProperty<Color> s_ThreadSeparatorProperty = new CustomStyleProperty<Color>("--thread-separator-color");
    static readonly CustomStyleProperty<Color> s_LinkColorProperty = new CustomStyleProperty<Color>("--link-color");

    /// <summary>
    /// Helper VisualElement that reads theme colors from USS.
    /// Add this to your visual tree to enable USS-based theme color reading.
    /// </summary>
    internal class ThemeColorReader : VisualElement
    {
        public ThemeColorReader()
        {
            AddToClassList("jobs-profiler-theme-colors");
            style.display = DisplayStyle.None;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_BarTextColorProperty, out var barTextColor))
                s_BarTextColor = barTextColor;

            if (evt.customStyle.TryGetValue(s_TimelineBackgroundProperty, out var timelineBg))
                s_TimelineBackground = timelineBg;

            if (evt.customStyle.TryGetValue(s_ThreadSeparatorProperty, out var threadSep))
                s_ThreadSeparatorLine = threadSep;

            if (evt.customStyle.TryGetValue(s_LinkColorProperty, out var linkColor))
                s_LinkColor = linkColor;

            // Update blending colors based on theme
            bool isProSkin = EditorGUIUtility.isProSkin;
            s_SelectionBlendColor = isProSkin ? Color.white : Color.black;
            s_DeselectionBlendColor = Color.black;
        }
    }

    // Colors for C# rendering code
    internal static Color BarTextColor => s_BarTextColor;
    internal static Color TimelineBackground => s_TimelineBackground;
    internal static Color ThreadSeparatorLine => s_ThreadSeparatorLine;
    internal static Color SelectionBlendColor => s_SelectionBlendColor;
    internal static Color DeselectionBlendColor => s_DeselectionBlendColor;

    /// <summary>
    /// Returns the link color as a hex string for use in rich text (e.g., "#4C7EFF").
    /// </summary>
    internal static string LinkColorHex => "#" + ColorUtility.ToHtmlStringRGB(s_LinkColor);

    #endregion

    #region Layout Constants

    /// <summary>
    /// Height of timeline bars in pixels. Matches Unity's profiler k_LineHeight.
    /// </summary>
    internal const float BarHeight = 16.0f;

    /// <summary>
    /// Font size for text on timeline bars in pixels.
    /// </summary>
    internal const float BarTextFontSize = 11.0f;

    #endregion
}
