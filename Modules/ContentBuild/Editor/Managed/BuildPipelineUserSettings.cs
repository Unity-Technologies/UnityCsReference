// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Build
{
    [FilePath(AssetPath, FilePathAttribute.Location.ProjectFolder)]
    internal sealed class BuildPipelineUserSettings : ScriptableSingleton<BuildPipelineUserSettings>
    {
        internal const string AssetPath = "UserSettings/BuildPipelineSettings.asset";
        public const int DefaultBuildHistoryLimit = 50;

        [SerializeField] int m_BuildHistoryLimit = DefaultBuildHistoryLimit;
        [SerializeField] string m_BuildHistoryDirectory = "";

        public int BuildHistoryLimit
        {
            get => m_BuildHistoryLimit;
            set
            {
                int clamped = Mathf.Max(0, value);
                if (clamped == m_BuildHistoryLimit)
                    return;
                m_BuildHistoryLimit = clamped;
                Save(true);
            }
        }

        // Empty means "use the default"; resolution happens in BuildHistory.BuildHistoryDirectory.
        public string BuildHistoryDirectoryRaw
        {
            get => m_BuildHistoryDirectory;
            set
            {
                string v = value ?? "";
                if (v == m_BuildHistoryDirectory)
                    return;
                m_BuildHistoryDirectory = v;
                Save(true);
            }
        }

        // C++ keeps an in-memory cache of the resolved directory; seed it from the C# asset
        // at Editor startup so native readers don't see a stale default.
        [InitializeOnLoadMethod]
        static void PushDirectoryToNativeAtStartup()
        {
            var _ = BuildHistory.BuildHistoryDirectory;
        }
    }
}
