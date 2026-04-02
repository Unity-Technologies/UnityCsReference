// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Multiplayer.Editor;
using UnityEditor;
using UnityEditor.Build.Profile;
using Unity.DedicatedServer.Editor.Internal;
using Unity.Multiplayer.Internal;

namespace Unity.DedicatedServer
{
    internal class CLISettingsSection : IMultiplayerBuildOptionsSection
    {
        public int Order => 100;

        private bool m_Expanded = false;

        public void DrawBuildOptions(BuildProfile profile)
        {
            if (!DedicatedServerMigrationUtility.ShouldEnableDedicatedServer())
                return;

            if (!InternalUtility.IsServerProfile(profile))
                return;

            m_Expanded = EditorGUILayout.Foldout(m_Expanded, "CLI Arguments defaults");

            if (m_Expanded)
            {
                EditorGUI.indentLevel++;
                CLIDefaults.OnGUI();
                EditorGUI.indentLevel--;
            }
        }
    }
}
