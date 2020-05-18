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
        internal static string[] s_ImportedAssets = {};

        [UsedImplicitly]
        private class AssetEvents : AssetPostprocessor
        {
            [UsedImplicitly]
            internal static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (!s_ImportRefreshEnabled)
                    return;

                s_ImportedAssets = s_ImportedAssets.Concat(importedAssets).Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths).Distinct()
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
            s_ImportedAssets = new string[] {};
        }

        [UsedImplicitly, RoleProvider(ProcessLevel.Master, ProcessEvent.AfterDomainReload)]
        private static void InitializeMaster()
        {
            s_ImportRefreshEnabled = true;
        }

        [UsedImplicitly, RoleProvider(ProcessLevel.Slave, ProcessEvent.AfterDomainReload)]
        private static void InitializeSlave()
        {
            EventService.RegisterEventHandler(nameof(DataServiceEvent.AUTO_REFRESH), (eventType, data) =>
            {
                string[] paths = data.Cast<string>().ToArray();
                Console.WriteLine($"Slave need to refresh the following assets: {String.Join(", ", paths)}");
                AssetDatabase.Refresh();
                if (paths.Any(p => p.EndsWith(".cs")))
                    EditorUtility.RequestScriptReload();
                InternalEditorUtility.RepaintAllViews();
                return paths;
            });
        }
    }
}
