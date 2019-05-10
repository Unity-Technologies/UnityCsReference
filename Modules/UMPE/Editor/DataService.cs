// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.MPE
{
    enum DataServiceEvent
    {
        AUTO_REFRESH
    }

    internal static class DataService
    {
        internal static bool s_ImportRefreshEnabled = false;
        internal static bool s_AboutToRefresh = false;
        internal static string[] s_ImportedAssets = {};

        internal class AssetEvents : AssetPostprocessor
        {
            internal static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (!s_ImportRefreshEnabled)
                    return;

                s_ImportedAssets = s_ImportedAssets.Concat(importedAssets).Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths).Distinct()
                    .ToArray();

                if (s_AboutToRefresh)
                    return;

                s_AboutToRefresh = true;
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

        [RoleProvider(ProcessLevel.UMP_MASTER, ProcessEvent.UMP_EVENT_AFTER_DOMAIN_RELOAD)]
        public static void InitializeMaster()
        {
            s_ImportRefreshEnabled = true;
        }

        [RoleProvider(ProcessLevel.UMP_SLAVE, ProcessEvent.UMP_EVENT_AFTER_DOMAIN_RELOAD)]
        public static void InitializeSlave()
        {
            EventService.On(nameof(DataServiceEvent.AUTO_REFRESH), (eventType, data) =>
            {
                string[] paths = ((IList)data).Cast<string>().ToArray();
                Debug.Log($"Slave need to refresh the following assets: {String.Join(", ", paths)}");
                AssetDatabase.Refresh();
                return paths;
            });
        }
    }
}
