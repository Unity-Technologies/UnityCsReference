// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;
using Unity.Multiplayer;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class EditorInstanceDescription : InstanceDescription
    {
        [SerializeField] internal MultiplayerRoleFlags m_Role;
        [SerializeField] string m_PlayerTag;

        [Tooltip("Starting scene of all editor instances, including main editor and all editor instances. If no scene is selected, " +
                 "editor instances will use the current scene as the initial scene.")]
        [SerializeField] [HideInInspector] private SceneAsset m_InitialScene;

        internal const string k_EditorInstanceTypeName = "EditorInstance";

        public MultiplayerRoleFlags RoleMask
        {
            get => m_Role;
            internal set => m_Role = value;
        }

        public string PlayerTag
        {
            get => m_PlayerTag;
            set => m_PlayerTag = value;
        }

        public SceneAsset InitialScene
        {
            get => m_InitialScene;
            set => m_InitialScene = value;
        }

        [SerializeField] [HideInInspector] public int PlayerInstanceIndex;

        public EditorInstanceDescription()
        {
            Name = "Editor";
            m_Role = MultiplayerRoleFlags.Client;
            PlayerInstanceIndex = 0;
            m_PlayerTag = "";
        }

        internal override string InstanceTypeName => k_EditorInstanceTypeName;

        internal override string BuildTargetType => InternalUtilities.GetBuildTargetType(EditorUserBuildSettings.activeBuildTarget);
        internal override string MultiplayerRole => m_Role.ToString();
    }
}
