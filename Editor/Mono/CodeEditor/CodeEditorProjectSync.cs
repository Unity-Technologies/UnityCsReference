// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.CodeEditor;
using UnityEngine.Scripting;

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

        [MenuItem("Assets/Open C# Project", secondaryPriority = 1)]
        static void SyncAndOpenSolution()
        {
            // Ensure that the mono islands are up-to-date
            AssetDatabase.Refresh();
            CodeEditor.Editor.CurrentCodeEditor.SyncAll();

            CodeEditor.Editor.CurrentCodeEditor.OpenProject();
        }
    }
}
