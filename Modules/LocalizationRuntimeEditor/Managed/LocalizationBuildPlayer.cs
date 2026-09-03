// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.Localization.Editor;

// Preloaded for the duration of the build, so the settings ship and self-register at startup.
partial class LocalizationBuildPlayer : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    [AutoStaticsCleanup] // build scoped state; a reload means no build is in progress
    static LocalizationSettings s_Settings;
    [AutoStaticsCleanup] // build scoped state
    static bool s_RemoveFromPreloadedAssets;

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        s_RemoveFromPreloadedAssets = false;
        s_Settings = LocalizationEditorSettings.ActiveSettings;
        if (s_Settings == null)
            return;

        var settingsWasDirty = EditorUtility.IsDirty(s_Settings);
        foreach (var collection in AssetProviderEditors.GetKnownCollections())
            AssetProviderEditors.RegisterCollection(collection);
        if (!settingsWasDirty)
            EditorUtility.ClearDirty(s_Settings);

        var preloadedAssets = PlayerSettings.GetPreloadedAssets();
        if (Array.IndexOf(preloadedAssets, s_Settings) >= 0)
        {
            s_RemoveFromPreloadedAssets = true;
            EditorApplication.delayCall += RemoveFromPreloadedAssets;
            return;
        }

        var playerSettings = GetPlayerSettings();
        var wasDirty = playerSettings != null && EditorUtility.IsDirty(playerSettings);
        ArrayUtility.Add(ref preloadedAssets, s_Settings);
        PlayerSettings.SetPreloadedAssets(preloadedAssets);
        s_RemoveFromPreloadedAssets = true;

        if (!wasDirty && playerSettings != null)
            EditorUtility.ClearDirty(playerSettings);

        EditorApplication.delayCall += RemoveFromPreloadedAssets;
    }

    public void OnPostprocessBuild(BuildReport report) => RemoveFromPreloadedAssets();

    static void RemoveFromPreloadedAssets()
    {
        if (s_Settings == null || !s_RemoveFromPreloadedAssets)
            return;
        s_RemoveFromPreloadedAssets = false;

        var playerSettings = GetPlayerSettings();
        var wasDirty = playerSettings != null && EditorUtility.IsDirty(playerSettings);
        var preloadedAssets = PlayerSettings.GetPreloadedAssets();
        ArrayUtility.Remove(ref preloadedAssets, s_Settings);
        PlayerSettings.SetPreloadedAssets(preloadedAssets);
        s_Settings = null;

        if (!wasDirty && playerSettings != null)
            EditorUtility.ClearDirty(playerSettings);
    }

    static PlayerSettings GetPlayerSettings()
    {
        var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
        return settings != null && settings.Length > 0 ? settings[0] : null;
    }
}
