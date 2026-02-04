// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class VirtualEditorInstanceDescription : EditorInstanceDescription
    {
        internal const string k_VirtualEditorInstanceDescription = "VirtualEditor";
        [Serializable]
        public class AdvancedConfig
        {
            [InspectorName("Stream Logs To Main Editor")] public bool StreamLogsToMainEditor;
            public Color LogsColor = new(0.3643f, 0.581f, 0.8679f);
        }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        [SerializeField] private AdvancedConfig m_AdvancedConfiguration;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        public AdvancedConfig AdvancedConfiguration => m_AdvancedConfiguration;

        internal override string InstanceTypeName => k_VirtualEditorInstanceDescription;

        internal override string MultiplayerRole => m_Role.ToString();

        internal override string BuildTargetType => InternalUtilities.GetBuildTargetType(EditorUserBuildSettings.activeBuildTarget);
    }
}
