// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.UIToolkit.Editor;

[Flags]
internal enum UIHierarchyDisplayOptions
{
    None = 0,
    Typename = 1,
    UssClasses = 2
}

internal enum NewVisualTreeAssetLocation
{
    [InspectorName("Ask for location")] AskForLocation,
    [InspectorName("Default location")] DefaultLocation,
}

internal enum AutoOpenMode
{
    Never,
    [InspectorName("When entering the UI Stage from the Main Stage")]
    FromMainStage,
    [InspectorName("When entering the UI Stage from any Stage")]
    Always
}

internal enum RectangleSelectionMode
{
    // An element is picked if its bounds overlap the marquee at all (default).
    AnyOverlap = 0,
    // An element is picked only if its bounds are entirely inside the marquee.
    FullyContained = 1,
}

internal static class UIToolkitAuthoringSettings
{
    private const string k_EnableInSceneUIAuthoring = "UIAuthoring.EnableHierarchyIntegration";
    private const string k_DisplayOptions = "UIAuthoring.DisplayOptions";
    private const string k_NewVisualTreeAssetLocation = "UIAuthoring.NewVisualTreeAssetLocation";
    private const UIHierarchyDisplayOptions DefaultDisplayOptions = UIHierarchyDisplayOptions.Typename | UIHierarchyDisplayOptions.UssClasses;
    private const NewVisualTreeAssetLocation DefaultNewVisualTreeAssetLocation = NewVisualTreeAssetLocation.AskForLocation;
    private const string k_AutoOpenUIViewportWindow = "UIAuthoring.AutoOpenUIViewportWindow";
    private const string k_AutoOpenStyleSheetsWindow = "UIAuthoring.AutoOpenStyleSheetsWindow";
    private const AutoOpenMode DefaultAutoOpenMode = AutoOpenMode.FromMainStage;
    private const string k_RectangleSelectionMode = "UIAuthoring.RectangleSelectionMode";
    private const RectangleSelectionMode DefaultRectangleSelectionMode = RectangleSelectionMode.AnyOverlap;

    [NoAutoStaticsCleanup]
    internal static event Action<bool> EnableInSceneAuthoringChanged;

    [NoAutoStaticsCleanup]
    internal static event Action<UIHierarchyDisplayOptions> DisplayOptionsChanged;

    [NoAutoStaticsCleanup]
    internal static event Action<AutoOpenMode> AutoOpenUIViewportWindowChanged;

    [NoAutoStaticsCleanup]
    internal static event Action<AutoOpenMode> AutoOpenStyleSheetsWindowChanged;

    [NoAutoStaticsCleanup]
    internal static event Action<RectangleSelectionMode> RectangleSelectionModeChanged;

    public static bool EnableInSceneUIAuthoring
    {
        get
        {
            var value = EditorUserSettings.GetConfigValue(k_EnableInSceneUIAuthoring);
            return !string.IsNullOrEmpty(value) && Convert.ToBoolean(value);
        }
        set
        {
            var currentValue = EnableInSceneUIAuthoring;
            if (currentValue == value)
                return;
            EditorUserSettings.SetConfigValue(k_EnableInSceneUIAuthoring, value.ToString());
            EnableInSceneAuthoringChanged?.Invoke(value);
        }
    }

    [NoAutoStaticsCleanup]
    public static UIHierarchyDisplayOptions DisplayOptions
    {
        get
        {
            var value = EditorUserSettings.GetConfigValue(k_DisplayOptions);
            if (string.IsNullOrEmpty(value) || !Enum.TryParse(typeof(UIHierarchyDisplayOptions), value, out var options))
                return DefaultDisplayOptions;

            return (UIHierarchyDisplayOptions)options;
        }
        set
        {
            var currentValue = DisplayOptions;
            if (currentValue == value)
                return;
            EditorUserSettings.SetConfigValue(k_DisplayOptions, value.ToString());
            DisplayOptionsChanged?.Invoke(value);
        }
    }

    [NoAutoStaticsCleanup]
    public static NewVisualTreeAssetLocation NewVisualTreeAssetLocation
    {
        get
        {
            var value = EditorUserSettings.GetConfigValue(k_NewVisualTreeAssetLocation);
            if (string.IsNullOrEmpty(value) || !Enum.TryParse<NewVisualTreeAssetLocation>(value, out var location))
                return DefaultNewVisualTreeAssetLocation;
            return location;
        }
        set
        {
            if (NewVisualTreeAssetLocation == value)
                return;
            EditorUserSettings.SetConfigValue(k_NewVisualTreeAssetLocation, value.ToString());
        }
    }

    public static AutoOpenMode AutoOpenUIViewportWindow
    {
        get => GetAutoOpenMode(k_AutoOpenUIViewportWindow);
        set
        {
            var currentValue = AutoOpenUIViewportWindow;
            if (currentValue == value)
                return;
            EditorUserSettings.SetConfigValue(k_AutoOpenUIViewportWindow, value.ToString());
            AutoOpenUIViewportWindowChanged?.Invoke(value);
        }
    }

    [NoAutoStaticsCleanup]
    public static AutoOpenMode AutoOpenStyleSheetsWindow
    {
        get => GetAutoOpenMode(k_AutoOpenStyleSheetsWindow);
        set
        {
            var currentValue = AutoOpenStyleSheetsWindow;
            if (currentValue == value)
                return;
            EditorUserSettings.SetConfigValue(k_AutoOpenStyleSheetsWindow, value.ToString());
            AutoOpenStyleSheetsWindowChanged?.Invoke(value);
        }
    }

    private static AutoOpenMode GetAutoOpenMode(string key)
    {
        var value = EditorUserSettings.GetConfigValue(key);
        if (string.IsNullOrEmpty(value) || !Enum.TryParse<AutoOpenMode>(value, out var mode))
            return DefaultAutoOpenMode;
        return mode;
    }

    public static RectangleSelectionMode RectangleSelectionMode
    {
        get
        {
            var value = EditorUserSettings.GetConfigValue(k_RectangleSelectionMode);
            if (string.IsNullOrEmpty(value) || !Enum.TryParse(typeof(RectangleSelectionMode), value, out var mode))
                return DefaultRectangleSelectionMode;

            return (RectangleSelectionMode)mode;
        }
        set
        {
            var currentValue = RectangleSelectionMode;
            if (currentValue == value)
                return;
            EditorUserSettings.SetConfigValue(k_RectangleSelectionMode, value.ToString());
            RectangleSelectionModeChanged?.Invoke(value);
        }
    }
}
