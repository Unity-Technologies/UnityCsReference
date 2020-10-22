// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using System.Text;
using UnityEditorInternal;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    internal class UnityBuildPostprocessor : IProcessSceneWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, Build.Reporting.BuildReport report)
        {
            int staticBatching, dynamicBatching;
            PlayerSettings.GetBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, out staticBatching, out dynamicBatching);
            if (staticBatching != 0)
            {
                using (StaticBatchingUtility.s_CombineMarker.Auto())
                    InternalStaticBatchingUtility.Combine(null, true, true, new EditorStaticBatcherGOSorter(scene));
            }
        }

        internal class EditorStaticBatcherGOSorter : InternalStaticBatchingUtility.StaticBatcherGOSorter
        {
            readonly ulong scenePathHash;

            public EditorStaticBatcherGOSorter(Scene scene)
            {
                scenePathHash = Hash128.Compute(AssetDatabase.AssetPathToGUID(scene.path)).u64_0;
            }

            Dictionary<int, ulong> assetHashCache = new Dictionary<int, ulong>();
            ulong GetStableAssetHash(UnityEngine.Object obj)
            {
                if (obj == null)
                    return 0;
                int id = obj.GetInstanceID();
                if (assetHashCache.TryGetValue(id, out var hash))
                    return hash;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(id, out var guid, out long _))
                    guid = string.Empty;
                hash = Hash128.Compute(guid).u64_0;
                assetHashCache.Add(id, hash);
                return hash;
            }

            static long GetStableHash(UnityEngine.Object obj, ulong assetHash)
            {
                var fileIdHint = Unsupported.GetFileIDHint(obj);
                return (long)(fileIdHint * 1181783497276652981UL + assetHash);
            }

            public override long GetMaterialId(Renderer renderer)
            {
                if (renderer == null)
                    return 0;
                var mat = renderer.sharedMaterial;
                if (mat == null)
                    return 0;
                return GetStableHash(mat, GetStableAssetHash(mat));
            }

            public override long GetRendererId(Renderer renderer)
            {
                if (renderer == null)
                    return -1;
                return GetStableHash(renderer, scenePathHash);
            }
        }
    }
}
