// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    internal static partial class ImportBlocksToggle
    {
        const string k_EnableImportBlocksSymbol = "ENABLE_IMPORT_BLOCKS";

        [AutoStaticsCleanupOnCodeReload]
        static bool? s_IsEnabled;

        internal static bool IsEnabled
        {
            get
            {
                if (s_IsEnabled.HasValue)
                    return s_IsEnabled.Value;

                // Import blocks are paused: the define is only honoured by editors running from a source checkout.
                // Skip the human-controlling check so batchmode test runs can still enable them.
                var defines = EditorUserBuildSettings.activeScriptCompilationDefines;
                bool isEnabled = Unsupported.IsSourceBuild(false)
                    && System.Array.Exists(defines, d => d == k_EnableImportBlocksSymbol);

                s_IsEnabled = isEnabled;
                return isEnabled;
            }
        }
    }
}
