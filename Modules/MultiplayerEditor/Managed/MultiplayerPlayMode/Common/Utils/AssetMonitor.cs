// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Compilation;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class AssetMonitor : AssetPostprocessor
    {
        static bool s_HasChanged = false;

        public static bool HasChanges => s_HasChanged;

        public static void Reset()
        {
            s_HasChanged = false;
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            UnityEditor.Compilation.CompilationPipeline.compilationFinished += OnCompilationFinished;
            UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (importedAssets.Length > 0 || deletedAssets.Length > 0 || movedAssets.Length > 0)
            {
                MppmLog.Debug($"Changes detected, importedAssets: {importedAssets.Length}, deletedAssets: {deletedAssets.Length}, movedAssets: {movedAssets.Length}, movedFromAssetPaths: {movedFromAssetPaths.Length}, didDomainReload: {didDomainReload}");
                s_HasChanged = true;
            }
        }

        private static void OnAssemblyCompilationFinished(string assembly, CompilerMessage[] compilerMessages)
        {
            s_HasChanged = true;
            MppmLog.Debug("Assembly compilation finished for: " + assembly);
        }

        private static void OnCompilationFinished(object result)
        {
            s_HasChanged = true;
            MppmLog.Debug("Compilation finished for: " + result);
        }
    }
}
