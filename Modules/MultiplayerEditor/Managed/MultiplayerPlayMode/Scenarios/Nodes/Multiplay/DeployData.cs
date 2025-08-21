// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Multiplayer.PlayMode.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [FilePath(Constants.k_VirtualProjectsFolder + "Multiplay/DeployData.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class DeployData : ScriptableSingleton<DeployData>
    {
        [Serializable]
        private struct BuildData
        {
            public long BuildId;
            public long Version;
            public Hash128 BuildHash;
        }

        [SerializeField] private List<BuildData> m_BuildData = new();

        public bool FindBuildIdAndVersionForHash(Hash128 buildHash, out long buildId, out long version)
        {
            foreach (var buildData in m_BuildData)
            {
                if (buildData.BuildHash.Equals(buildHash))
                {
                    buildId = buildData.BuildId;
                    version = buildData.Version;
                    return true;
                }
            }

            buildId = -1;
            version = -1;
            return false;
        }

        public void AssignHashToBuildId(long buildId, long version, Hash128 buildHash)
        {
            for (var i = 0; i < m_BuildData.Count; i++)
            {
                var buildData = m_BuildData[i];
                if (buildData.BuildId == buildId)
                {
                    buildData.Version = version;
                    buildData.BuildHash = buildHash;
                    m_BuildData[i] = buildData;
                    return;
                }
            }

            m_BuildData.Add(new BuildData { BuildId = buildId, Version = version, BuildHash = buildHash });

            Save(true);
        }

        public void Clear()
        {
            m_BuildData.Clear();
            Save(true);
        }
    }
}
