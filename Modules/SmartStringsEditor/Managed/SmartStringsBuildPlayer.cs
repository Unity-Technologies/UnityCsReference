// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.SmartStrings.Editor;

// Adds the active SmartStringsSettings to the player's preloaded assets for the duration of the build,
// so the settings ship in the player and self-register at startup (via SmartStringsSettings.OnEnable).
class SmartStringsBuildPlayer : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    SmartStringsSettings m_Settings;
    bool m_RemoveFromPreloadedAssets;

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        m_RemoveFromPreloadedAssets = false;
        m_Settings = SmartStringsEditorSettings.ActiveSettings;
        // A non-persistent settings object would serialize as a null {fileID: 0} preloaded asset.
        if (m_Settings == null || !EditorUtility.IsPersistent(m_Settings))
            return;

        var preloadedAssets = PlayerSettings.GetPreloadedAssets();
        if (Array.IndexOf(preloadedAssets, m_Settings) >= 0)
            return;

        ArrayUtility.Add(ref preloadedAssets, m_Settings);
        SetPreloadedAssetsPreservingDirty(preloadedAssets);
        m_RemoveFromPreloadedAssets = true;
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (m_Settings == null || !m_RemoveFromPreloadedAssets)
            return;

        var preloadedAssets = PlayerSettings.GetPreloadedAssets();
        ArrayUtility.Remove(ref preloadedAssets, m_Settings);
        SetPreloadedAssetsPreservingDirty(preloadedAssets);
        m_Settings = null;
    }

    // Sets the preloaded assets without persisting the temporary change to ProjectSettings.
    static void SetPreloadedAssetsPreservingDirty(UnityEngine.Object[] preloadedAssets)
    {
        var playerSettings = GetPlayerSettings();
        var wasDirty = playerSettings != null && EditorUtility.IsDirty(playerSettings);
        PlayerSettings.SetPreloadedAssets(preloadedAssets);
        if (!wasDirty && playerSettings != null)
            EditorUtility.ClearDirty(playerSettings);
    }

    static PlayerSettings GetPlayerSettings()
    {
        var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
        return settings != null && settings.Length > 0 ? settings[0] : null;
    }
}
