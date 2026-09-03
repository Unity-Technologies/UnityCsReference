// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Editor-side owner of the experimental "generate accessibility hierarchies" setting: persists
    /// it under ProjectSettings/ and applies it to the runtime bridge on play mode transitions.
    /// Players never see this class — the build processor ships the value as a flag TextAsset that
    /// the bridge reads at the first panel attach.
    /// </summary>
    /// <remarks>
    /// The setting is stored as JSON rather than a serialized object: ScriptableSingleton-style
    /// files reference their managed class through the built-in MonoScript registry or the editor
    /// class identifier, and neither resolves classes from engine/editor *module* assemblies on
    /// load — the file saves fine but deserializes as a typeless fallback object, silently
    /// resetting the setting every session. Burst and SceneTemplate persist module settings as
    /// JSON under ProjectSettings/ for the same reason.
    /// </remarks>
    internal static partial class UIToolkitAccessibilitySettingsEditor
    {
        internal const string settingsPath = "ProjectSettings/UIToolkitAccessibilitySettings.json";

        [Serializable]
        class SettingsData
        {
            public bool generateHierarchies;
        }

        // Reset on code reload; the lazy getter below re-reads the JSON on the next access.
        [AutoStaticsCleanupOnCodeReload]
        static SettingsData s_Data;

        static SettingsData data
        {
            get
            {
                if (s_Data != null)
                    return s_Data;

                try
                {
                    if (File.Exists(settingsPath))
                        s_Data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(settingsPath));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not read {settingsPath}; falling back to defaults. {e.Message}");
                }

                s_Data ??= new SettingsData();
                return s_Data;
            }
        }

        internal static bool generateHierarchies
        {
            get => data.generateHierarchies;
            set
            {
                if (data.generateHierarchies == value)
                    return;

                data.generateHierarchies = value;
                File.WriteAllText(settingsPath, JsonUtility.ToJson(data, prettyPrint: true));

                // The runtime flag applies live; outside play mode, the bridge does nothing anyway.
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    UITKAccessibilityBridge.featureEnabled = value;
            }
        }

        [OnEnteringPlayMode]
        static void OnEnteringPlayMode()
        {
            // The editor has no build step to ship the configuration through, so entering play
            // mode applies the persisted setting directly (players read the TextAsset the build
            // processor ships).
            UITKAccessibilityBridge.featureEnabled = generateHierarchies;
        }
    }
}
