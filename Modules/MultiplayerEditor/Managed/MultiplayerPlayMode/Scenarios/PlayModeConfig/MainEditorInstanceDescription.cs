// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Multiplayer.PlayMode.Editor;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class MainEditorInstanceDescription : EditorInstanceDescription
    {
        internal const string k_MainEditorInstanceTypeName = "MainEditor";

        internal override string InstanceTypeName => k_MainEditorInstanceTypeName;

        internal override string MultiplayerRole => m_Role.ToString();

        internal override string BuildTargetType => InternalUtilities.GetBuildTargetType(EditorUserBuildSettings.activeBuildTarget);
    }
}
