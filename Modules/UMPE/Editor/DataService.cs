// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditorInternal;

namespace UnityEditor.MPE
{
    enum DataServiceEvent
    {
        AUTO_REFRESH
    }

    static class DataService
    {
        internal static bool s_ImportRefreshEnabled = false;
        internal static bool s_AboutToRefresh = false;
        internal static string[] s_ImportedAssets = Array.Empty<string>();

        [UsedImplicitly]
        private class AssetEvents : AssetPostprocessor
        {
            [UsedImplicitly]
            internal static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (!s_ImportRefreshEnabled)
                    return;

                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                s_ImportedAssets = s_ImportedAssets.Concat(importedAssets).Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths).Distinct()
#pragma warning restore UA2001
                    .ToArray();

                if (s_AboutToRefresh)
                    return;

                s_AboutToRefresh = true;
                EditorApplication.update -= EmitRefresh;
                EditorApplication.update += EmitRefresh;
            }
        }

        private static void EmitRefresh()
        {
            EditorApplication.update -= EmitRefresh;

            EventService.Emit(nameof(DataServiceEvent.AUTO_REFRESH), s_ImportedAssets);
            s_AboutToRefresh = false;
            s_ImportedAssets = Array.Empty<string>();
        }

        [UsedImplicitly, RoleProvider(ProcessLevel.Main, ProcessEvent.AfterDomainReload)]
        private static void InitializeMaster()
        {
            s_ImportRefreshEnabled = true;
        }

        [UsedImplicitly, RoleProvider(ProcessLevel.Secondary, ProcessEvent.AfterDomainReload)]
        private static void InitializeSlave()
        {
            EventService.RegisterEventHandler(nameof(DataServiceEvent.AUTO_REFRESH), (eventType, data) =>
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                string[] paths = data.Cast<string>().ToArray();
#pragma warning restore UA2001
                Console.WriteLine($"Secondary process need to refresh the following assets: {String.Join(", ", paths)}");
                AssetDatabase.Refresh();
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (paths.Any(p => p.EndsWith(".cs")))
#pragma warning restore UA2001
                    EditorUtility.RequestScriptReload();
                InternalEditorUtility.RepaintAllViews();
                return paths;
            });
        }
    }
}
