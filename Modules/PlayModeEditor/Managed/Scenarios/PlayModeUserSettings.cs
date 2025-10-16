// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace Unity.PlayMode.Editor;

[FilePath(k_FilePath, FilePathAttribute.Location.ProjectFolder)]
class PlayModeUserSettings : ScriptableSingleton<PlayModeUserSettings>
{
    internal const string k_FilePath = "UserSettings/PlayModeUserSettings.asset";

    class SaveAssetsProcessor : AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            instance.SaveIfDirty();
            return paths;
        }
    }

    [SerializeField] private PlayModeScenario m_LastActiveConfiguration;

    public PlayModeScenario LastActiveConfiguration
    {
        get => m_LastActiveConfiguration;
        set
        {
            m_LastActiveConfiguration = value;
            EditorUtility.SetDirty(this);
        }
    }

    internal void SaveIfDirty()
    {
        if (EditorUtility.IsDirty(this))
            Save(true);
    }
}
