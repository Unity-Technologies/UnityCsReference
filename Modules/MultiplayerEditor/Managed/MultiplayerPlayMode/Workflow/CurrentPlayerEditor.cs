// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class CurrentPlayerEditor : CurrentPlayerApi
    {
        static SystemDataStore m_SystemDataStore;

        override public bool IsMainEditor
        {
            get
            {
                // If Multiplayer Play Mode is not installed, it's always the main editor.
                if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                    return true;

                return !VirtualProjectsEditor.IsClone;
            }
        }

        public CurrentPlayerEditor()
        {
            m_SystemDataStore = VirtualProjectsEditor.IsClone
                ? SystemDataStore.GetClone()
                : SystemDataStore.GetMain();

            bool hasPlayer;
            PlayerStateJson player;
            if (VirtualProjectsEditor.IsClone)
            {
                hasPlayer = Filters.FindFirstPlayerWithVirtualProjectsIdentifier(m_SystemDataStore.LoadAllPlayerJson(),
                    VirtualProjectsEditor.CloneIdentifier, out player);
                Debug.Assert(hasPlayer, $"Could not find player using virtual project {VirtualProjectsEditor.CloneIdentifier}");
            }
            else
            {
                hasPlayer = Filters.FindFirstPlayerWithPlayerType(m_SystemDataStore.LoadAllPlayerJson(), PlayerType.Main, out player);
                Debug.Assert(hasPlayer, "Could not find player for the main editor");
            }

            SetTags(player.Tags);
        }

        override public void ReportResult(
            bool condition,
            string message = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (VirtualProjectsEditor.IsClone)
            {
                var vpContext = EditorContexts.CloneContext;
                var m = new TestResultMessage(VirtualProjectsEditor.CloneIdentifier, callingFilePath, lineNumber, condition, message);
                vpContext.MessagingService.Broadcast(m);
            }
            else
            {
                var m = new TestResultMessage(null, callingFilePath, lineNumber, condition, message);
                if (!m.ResultCondition)
                {
                    VirtualProjectWorkflow.WorkflowMainEditorContext.TestFailure = m;
                }
            }
        }
    }
}
