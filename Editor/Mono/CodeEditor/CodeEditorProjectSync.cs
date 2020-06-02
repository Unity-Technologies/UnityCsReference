// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.CodeEditor;
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
                CodeEditor.Editor.Current.SyncAll();
            }
        }

        [RequiredByNativeCode]
        public static void SyncEditorProject()
        {
            CodeEditor.Editor.Current.SyncAll();
        }

        // For the time being this doesn't use the callback
        public static void PostprocessSyncProject(
            string[] importedAssets,
            string[] addedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            CodeEditor.Editor.Current.SyncIfNeeded(addedAssets, deletedAssets, movedAssets, movedFromAssetPaths, importedAssets);
        }

        [MenuItem("Assets/Open C# Project")]
        static void SyncAndOpenSolution()
        {
            // Ensure that the mono islands are up-to-date
            AssetDatabase.Refresh();
            #pragma warning disable 618
            if (ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation) == ScriptEditorUtility.ScriptEditor.Other
                || ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation) == ScriptEditorUtility.ScriptEditor.SystemDefault)
            {
                CodeEditor.Editor.Current.SyncAll();
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
            if (ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation) == ScriptEditorUtility.ScriptEditor.Other
                || ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation) == ScriptEditorUtility.ScriptEditor.SystemDefault)
            {
                CodeEditor.Editor.Current.OpenProject();
            }
            else
            {
                InternalEditorUtility.OpenFileAtLineExternal("", -1, -1);
            }
        }
    }
}
