// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Profiling;

namespace UnityEngine
{
    public sealed partial class StaticBatchingUtility
    {
        internal static ProfilerMarker s_CombineMarker = new ProfilerMarker("StaticBatching.Combine");

        public static void Combine(GameObject staticBatchRoot)
        {
            using (s_CombineMarker.Auto())
                CombineRoot(staticBatchRoot);
        }

        public static void Combine(GameObject[] gos, GameObject staticBatchRoot)
        {
            using (s_CombineMarker.Auto())
                StaticBatchingHelper.CombineMeshes(gos, staticBatchRoot);
        }

        private static void CombineRoot(GameObject staticBatchRoot)
        {
            MeshFilter[] filters;
            GameObject[] gos;
            if (staticBatchRoot == null)
                filters = (MeshFilter[])Object.FindObjectsByType(typeof(MeshFilter), FindObjectsSortMode.None);
            else
                filters = staticBatchRoot.GetComponentsInChildren<MeshFilter>();

            gos = new GameObject[filters.Length];
            for (int i = 0; i < filters.Length; i++)
                gos[i] = filters[i].gameObject;

            StaticBatchingHelper.CombineMeshes(gos, staticBatchRoot);
        }
    }
} // namespace UnityEngine
