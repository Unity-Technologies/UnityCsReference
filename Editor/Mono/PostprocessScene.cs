// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.Build;
using System.Text;
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
                InternalStaticBatchingUtility.Combine(null, true, true, new EditorStaticBatcherGOSorter(scene));
            }
        }

        internal class EditorStaticBatcherGOSorter : InternalStaticBatchingUtility.StaticBatcherGOSorter
        {
            Scene scene;

            public EditorStaticBatcherGOSorter(Scene scene)
            {
                this.scene = scene;
            }

            private static long GetStableHash(UnityEngine.Object instance, string guid)
            {
                var lfid = UnityEditor.Unsupported.GetFileIDHint(instance);

                using (var md5Hash = System.Security.Cryptography.MD5.Create())
                {
                    var bytes = Encoding.ASCII.GetBytes(guid);
                    md5Hash.TransformBlock(bytes, 0, bytes.Length, null, 0);
                    bytes = BitConverter.GetBytes(lfid);
                    md5Hash.TransformFinalBlock(bytes, 0, bytes.Length);
                    return BitConverter.ToInt64(md5Hash.Hash, 0);
                }
            }

            public override long GetMaterialId(Renderer renderer)
            {
                if (renderer == null || renderer.sharedMaterial == null)
                    return 0;

                string path = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
                string guid = AssetDatabase.AssetPathToGUID(path);
                return GetStableHash(renderer.sharedMaterial, guid);
            }

            public override long GetRendererId(Renderer renderer)
            {
                if (renderer == null)
                    return -1;

                string guid = AssetDatabase.AssetPathToGUID(scene.path);
                return GetStableHash(renderer, guid);
            }
        }
    }
}
