// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/GameObjectUtility.bindings.h")]
    public sealed partial class GameObjectUtility
    {
        [System.Obsolete("GetNavMeshArea has been deprecated. To relate a GameObject to an area type and retrieve their relation later, create a NavMeshBuildMarkup and pass it into UnityEngine.AI.NavMeshBuilder.CollectSources().")]
        public static extern int GetNavMeshArea(GameObject go);

        [System.Obsolete("SetNavMeshArea has been deprecated. To relate a GameObject to an area type, create a NavMeshBuildMarkup and pass it into UnityEngine.AI.NavMeshBuilder.CollectSources().")]
        public static extern void SetNavMeshArea(GameObject go, int areaIndex);

        [System.Obsolete("GetNavMeshAreaFromName has been deprecated. Use NavMesh.GetAreaFromName instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.AI.NavMesh.GetAreaFromName(*)")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaFromName")]
        public static extern int GetNavMeshAreaFromName(string name);

        [System.Obsolete("GetNavMeshAreaNames has been deprecated. Use NavMesh.GetAreaNames instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.AI.NavMesh.GetAreaNames()")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaNames")]
        public static extern string[] GetNavMeshAreaNames();

        [System.Obsolete("GetNavMeshLayer has been deprecated. To relate a GameObject to an area type and retrieve their relation later, create a NavMeshBuildMarkup and pass it into UnityEngine.AI.NavMeshBuilder.CollectSources(). (UnityUpgradable) -> GetNavMeshArea(*)")]
        [NativeName("GetNavMeshArea")]
        public static extern int GetNavMeshLayer(GameObject go);

        [System.Obsolete("SetNavMeshLayer has been deprecated. To relate a GameObject to an area type, create a NavMeshBuildMarkup and pass it into UnityEngine.AI.NavMeshBuilder.CollectSources(). (UnityUpgradable) -> SetNavMeshArea(*)")]
        [NativeName("SetNavMeshArea")]
        public static extern void SetNavMeshLayer(GameObject go, int areaIndex);

        [System.Obsolete("GetNavMeshLayerFromName has been deprecated. Use NavMesh.GetAreaFromName instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.AI.NavMesh.GetAreaFromName(*)")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaFromName")]
        public static extern int GetNavMeshLayerFromName(string name);

        [System.Obsolete("GetNavMeshLayerNames has been deprecated. Use NavMesh.GetAreaNames instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.AI.NavMesh.GetAreaNames()")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaNames")]
        public static extern string[] GetNavMeshLayerNames();

        [System.Obsolete("use AnimatorUtility.OptimizeTransformHierarchy instead.")]
        static void OptimizeTransformHierarchy(GameObject go)
        {
            AnimatorUtility.OptimizeTransformHierarchy(go, null);
        }

        [System.Obsolete("use AnimatorUtility.DeoptimizeTransformHierarchy instead.")]
        static void DeoptimizeTransformHierarchy(GameObject go)
        {
            AnimatorUtility.DeoptimizeTransformHierarchy(go);
        }
    }
}
