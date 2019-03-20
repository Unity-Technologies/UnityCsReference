// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Rendering;

namespace UnityEngine
{
    partial class Terrain
    {
        [Obsolete("Enum type MaterialType is not used any more.", false)]
        public enum MaterialType
        {
            BuiltInStandard = 0,
            BuiltInLegacyDiffuse,
            BuiltInLegacySpecular,
            Custom
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("splatmapDistance is deprecated, please use basemapDistance instead. (UnityUpgradable) -> basemapDistance", true)]
        public float splatmapDistance
        {
            get { return basemapDistance; }
            set { basemapDistance = value; }
        }

        [Obsolete("castShadows is deprecated, please use shadowCastingMode instead.")]
        public bool castShadows
        {
            get { return shadowCastingMode != ShadowCastingMode.Off; }
            set { shadowCastingMode = value ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off; }
        }

        [Obsolete("Property materialType is not used any more. Set materialTemplate directly.", false)]
        public MaterialType materialType
        {
            get { return MaterialType.Custom; }
            set {}
        }

        [Obsolete("Property legacySpecular is not used any more. Set materialTemplate directly.", false)]
        public Color legacySpecular
        {
            get { return Color.gray; }
            set {}
        }

        [Obsolete("Property legacyShininess is not used any more. Set materialTemplate directly.", false)]
        public float legacyShininess
        {
            get { return 0.078125f; }
            set {}
        }

        [Obsolete("Use TerrainData.SyncHeightmap to notify all Terrain instances using the TerrainData.", false)]
        public void ApplyDelayedHeightmapModification()
        {
            terrainData?.SyncHeightmap();
        }
    }
}
