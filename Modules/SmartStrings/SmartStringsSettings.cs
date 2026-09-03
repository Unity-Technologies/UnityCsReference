// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace Unity.SmartStrings;

/// <summary>
/// Project-wide Smart Strings settings. The active instance is included in player builds as a
/// preloaded asset and exposes the default <see cref="SmartFormatter"/> used to format Smart Strings.
/// </summary>
[HelpURL("smart-strings/smart-strings-settings")]
public class SmartStringsSettings : ScriptableObject
{
    // Key used to store the active settings as an EditorBuildSettings config object.
    internal const string ConfigName = "com.unity.smartstrings.settings";

    // Must NOT use a cleanup attribute: the codegen roots a UnityEngine.Object-derived type for the
    // linker (keeping all its methods; see PanelRenderer.bindings.cs). A destroyed instance compares
    // == null via the UnityEngine.Object fake-null, so a stale wrapper re-resolves on next access.
    [NoAutoStaticsCleanup]
    static SmartStringsSettings s_Instance;

    // The editor calls this at play-mode transitions to mimic the domain reload skipped when reload is disabled.
    [VisibleToOtherModules("UnityEditor.SmartStringsModule")]
    internal static void ResetStaticsForPlayMode()
    {
        Smart.ResetStatics();
        Extensions.PersistentVariablesSource.ResetStatics();
    }

    [SerializeField]
    SmartFormatter m_SmartFormatter;

    /// <summary>
    /// The default <see cref="SmartFormatter"/> used to format Smart Strings in this project.
    /// </summary>
    public SmartFormatter SmartFormatter
    {
        get => m_SmartFormatter ??= Smart.CreateDefaultSmartFormat();
        set => m_SmartFormatter = value;
    }

    /// <summary>
    /// The active <see cref="SmartStringsSettings"/> for the project, or <see langword="null"/> if none is set.
    /// </summary>
    public static SmartStringsSettings Instance
    {
        get => s_Instance;
        set => s_Instance = value;
    }

    /// <summary>
    /// <see langword="true"/> if an active <see cref="SmartStringsSettings"/> instance is available.
    /// </summary>
    public static bool HasSettings => !ReferenceEquals(s_Instance, null);

    void OnEnable()
    {
        if (ReferenceEquals(s_Instance, null))
            s_Instance = this;
    }
}
