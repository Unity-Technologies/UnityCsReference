// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.SmartStrings.PersistentVariables;
using UnityEditor;

namespace Unity.SmartStrings.Editor;

// Editor access to the active SmartStringsSettings for the project. The active settings are stored as an
// EditorBuildSettings config object and included in player builds.
static class SmartStringsEditorSettings
{
    [InitializeOnLoadMethod]
    static void Initialize()
    {
        // Make the active settings available to runtime code while in the editor.
        var settings = ActiveSettings;
        if (settings != null)
            SmartStringsSettings.Instance = settings;

        EditorApplication.update += VariableEditorHooks.SendPending;

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    // With Domain Reload disabled, statics persist across play-mode transitions; reset them to match a reload.
    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state is PlayModeStateChange.ExitingEditMode or PlayModeStateChange.EnteredEditMode)
            SmartStringsSettings.ResetStaticsForPlayMode();
    }

    /// <summary>
    /// The active <see cref="SmartStringsSettings"/> for the project, included in player builds.
    /// </summary>
    public static SmartStringsSettings ActiveSettings
    {
        get
        {
            EditorBuildSettings.TryGetConfigObject(SmartStringsSettings.ConfigName, out SmartStringsSettings settings);
            return settings;
        }
        set
        {
            if (value == null)
                EditorBuildSettings.RemoveConfigObject(SmartStringsSettings.ConfigName);
            else
                EditorBuildSettings.AddConfigObject(SmartStringsSettings.ConfigName, value, true);
            SmartStringsSettings.Instance = value;
        }
    }
}
