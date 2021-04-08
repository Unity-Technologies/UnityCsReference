// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CodeEditor;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class CodeEditorProjectSync : AssetPostprocessor
    {
        class BuildTargetChangedHandler : Build.IActiveBuildTargetChanged
        {
            public int callbackOrder => 0;

            public void OnActiveBuildTargetChanged(BuildTarget oldTarget, BuildTarget newTarget)
            {
                CodeEditor.Editor.CurrentCodeEditor.SyncAll();
            }
        }

        [RequiredByNativeCode]
        public static void SyncEditorProject()
        {
            CodeEditor.Editor.CurrentCodeEditor.SyncAll();
        }

        // For the time being this doesn't use the callback
        public static void PostprocessSyncProject(
            string[] importedAssets,
            string[] addedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            CodeEditor.Editor.CurrentCodeEditor.SyncIfNeeded(addedAssets, deletedAssets, movedAssets, movedFromAssetPaths, importedAssets);
        }

        [MenuItem("Assets/Open C# Project")]
        static void SyncAndOpenSolution()
        {
            // Ensure that the mono islands are up-to-date
            AssetDatabase.Refresh();
            #pragma warning disable 618
            if (ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorPath) == ScriptEditorUtility.ScriptEditor.Other)
            {
                CodeEditor.Editor.CurrentCodeEditor.SyncAll();
            }
            else
            {
                SyncVS.Synchronizer.Sync();
            }

            OpenProjectFileUnlessInBatchMode();
        }

        static void OpenProjectFileUnlessInBatchMode()
        {
            if (InternalEditorUtility.inBatchMode)
                return;

            #pragma warning disable 618
            if (ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorPath) == ScriptEditorUtility.ScriptEditor.Other)
            {
                CodeEditor.Editor.CurrentCodeEditor.OpenProject();
            }
            else
            {
                InternalEditorUtility.OpenFileAtLineExternal("", -1, -1);
            }
        }
    }
}
