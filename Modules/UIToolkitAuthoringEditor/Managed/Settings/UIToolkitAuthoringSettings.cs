// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.UIToolkit.Editor;

[Flags]
internal enum UIHierarchyDisplayOptions
{
    None = 0,
    Typename = 1,
    UssClasses = 2
}

internal static class UIToolkitAuthoringSettings
{
    private const string k_EnableHierarchyIntegration = "UIAuthoring.EnableHierarchyIntegration";
    private const string k_DisplayOptions = "UIAuthoring.DisplayOptions";
    private const UIHierarchyDisplayOptions DefaultDisplayOptions = UIHierarchyDisplayOptions.Typename | UIHierarchyDisplayOptions.UssClasses;

    [NoAutoStaticsCleanup]
    internal static event Action<bool> HierarchyIntegrationChanged;

    [NoAutoStaticsCleanup]
    internal static event Action<UIHierarchyDisplayOptions> DisplayOptionsChanged;

    [NoAutoStaticsCleanup]
    public static bool EnableHierarchyIntegration
    {
        get
        {
            var value = EditorUserSettings.GetConfigValue(k_EnableHierarchyIntegration);
            return !string.IsNullOrEmpty(value) && Convert.ToBoolean(value);
        }
        set
        {
            var currentValue = EnableHierarchyIntegration;
            if (currentValue == value)
                return;
            EditorUserSettings.SetConfigValue(k_EnableHierarchyIntegration, value.ToString());
            HierarchyIntegrationChanged?.Invoke(value);
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
}
