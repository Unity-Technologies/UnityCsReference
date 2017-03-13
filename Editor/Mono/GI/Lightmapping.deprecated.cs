// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEngineInternal;

namespace UnityEditor
{
    public partial class Lightmapping
    {
        [System.Obsolete("lightmapSnapshot has been deprecated. Use lightingDataAsset instead (UnityUpgradable) -> lightingDataAsset", true)]
        public static LightmapSnapshot lightmapSnapshot
        {
            get { return null; }
            set {}
        }

        [System.Obsolete("BakeSelectedAsync has been deprecated. Use BakeAsync instead (UnityUpgradable) -> BakeAsync()", true)]
        public static bool BakeSelectedAsync() { return false; }

        [System.Obsolete("BakeSelected has been deprecated. Use Bake instead (UnityUpgradable) -> Bake()", true)]
        public static bool BakeSelected() { return false; }

        [System.Obsolete("BakeLightProbesOnlyAsync has been deprecated. Use BakeAsync instead (UnityUpgradable) -> BakeAsync()", true)]
        public static bool BakeLightProbesOnlyAsync() { return false; }

        [System.Obsolete("BakeLightProbesOnly has been deprecated. Use Bake instead (UnityUpgradable) -> Bake()", true)]
        public static bool BakeLightProbesOnly() { return false; }
    }
}

