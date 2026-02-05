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

    [SerializeField] private PlayModeConfiguration m_LastActiveConfiguration;

    public PlayModeConfiguration LastActiveConfiguration
    {
        get => m_LastActiveConfiguration;
        set
        {
            m_LastActiveConfiguration = value;
            Save(true);
        }
    }
}
