// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.TerrainTools
{
    ///<summary>How <see cref="TerrainEditUtility.SculptHeights" /> changes the heights inside a region.</summary>
    public enum TerrainSculptOperation
    {
        ///<summary>Raises the heights by <see cref="TerrainSculptSettings.amount" />.</summary>
        Raise = 0,
        ///<summary>Lowers the heights by <see cref="TerrainSculptSettings.amount" />.</summary>
        Lower = 1,
        ///<summary>Sets the heights to <see cref="TerrainSculptSettings.height" />.</summary>
        Set = 2,
        ///<summary>Flattens the heights to the height of the region that <see cref="TerrainSculptSettings.flattenReference" /> picks.</summary>
        Flatten = 3,
        ///<summary>Averages each height with its neighbors within <see cref="TerrainSculptSettings.smoothRadius" />.</summary>
        Smooth = 4,
    }

    ///<summary>The height of the region that <see cref="TerrainSculptOperation.Flatten" /> flattens to.</summary>
    public enum TerrainFlattenReference
    {
        ///<summary>The mean height inside the region.</summary>
        Mean = 0,
        ///<summary>The lowest height inside the region.</summary>
        Minimum = 1,
        ///<summary>The highest height inside the region.</summary>
        Maximum = 2,
    }

    ///<summary>Settings for <see cref="TerrainEditUtility.SculptHeights" />.</summary>
    ///<remarks>Heights and distances are in meters. Heights are measured above the base of the Terrain, from 0 to <see cref="TerrainData.size" />.y.</remarks>
    public sealed class TerrainSculptSettings
    {
        ///<summary>Creates settings for an operation, with no falloff and no blend.</summary>
        ///<param name="operation">How to change the heights.</param>
        public TerrainSculptSettings(TerrainSculptOperation operation)
        {
            this.operation = operation;
        }

        ///<summary>How to change the heights.</summary>
        public TerrainSculptOperation operation { get; set; }

        ///<summary>The target height for <see cref="TerrainSculptOperation.Set" />. NaN by default. Raise, Lower and Smooth ignore it; Flatten requires it to be NaN, since it takes its height from the region.</summary>
        public float height { get; set; } = float.NaN;

        ///<summary>For <see cref="TerrainSculptOperation.Flatten" />: which height of the region to flatten to. The samples inside the region decide it, without the blend band and regardless of the falloff. <see cref="TerrainFlattenReference.Mean" /> by default.</summary>
        public TerrainFlattenReference flattenReference { get; set; } = TerrainFlattenReference.Mean;

        ///<summary>How far to raise or lower for <see cref="TerrainSculptOperation.Raise" /> and <see cref="TerrainSculptOperation.Lower" />. Must be greater than zero for those operations.</summary>
        public float amount { get; set; }

        ///<summary>The averaging radius for <see cref="TerrainSculptOperation.Smooth" />. 0 uses two heightmap cells. Must not exceed the diagonal of the Terrain.</summary>
        ///<remarks>Smoothing is a box average over the square of samples within this radius, applied twice (once per axis). It does not match the Smooth Height brush, which weighs neighbors by the brush.</remarks>
        public float smoothRadius { get; set; }

        ///<summary>The fraction of the region, from 0 to 1, over which the change fades at its edge. See <see cref="TerrainRegion.GetWeight" />. Cannot be combined with <see cref="blendDistance" />.</summary>
        public float falloff { get; set; }

        ///<summary>The distance outside the region over which the change eases back to the untouched ground. Nothing farther than this from the region changes. Cannot be combined with <see cref="falloff" />, and must not exceed the diagonal of the Terrain.</summary>
        ///<remarks>Inside the region the change applies fully; from the edge of the region outwards the weight eases from 1 to 0 over this distance. When no heightmap sample lies inside the region, as for a region off the Terrain or the intersection of two regions that do not overlap, there is no edge to ease back from and nothing changes.</remarks>
        public float blendDistance { get; set; }
    }

    ///<summary>How <see cref="TerrainEditUtility.PaintLayers" /> changes the weight of a layer.</summary>
    public enum TerrainLayerBlendMode
    {
        ///<summary>Moves the weight towards 1 by the opacity, like the Paint Texture brush: weight += opacity * (1 - weight).</summary>
        Blend = 0,
        ///<summary>Makes the weight equal to the opacity.</summary>
        Set = 1,
    }

    ///<summary>One rule of <see cref="TerrainEditUtility.PaintLayers" />: paint a layer where the height and the slope lie in their ranges.</summary>
    public sealed class TerrainLayerPaintRule
    {
        ///<summary>Creates a rule that paints a layer everywhere in the region at full opacity.</summary>
        ///<param name="layerIndex">The index of the layer in <see cref="TerrainData.terrainLayers" />.</param>
        public TerrainLayerPaintRule(int layerIndex)
        {
            this.layerIndex = layerIndex;
        }

        ///<summary>The index of the layer in <see cref="TerrainData.terrainLayers" />.</summary>
        public int layerIndex { get; set; }

        ///<summary>The opacity, from 0 to 1. See <see cref="TerrainLayerBlendMode" />. 1 by default.</summary>
        public float opacity { get; set; } = 1f;

        ///<summary>Paint only where the height, in meters above the base of the Terrain, lies within [x, y]. Unbounded by default.</summary>
        public Vector2 heightRange { get; set; } = new Vector2(float.NegativeInfinity, float.PositiveInfinity);

        ///<summary>Paint only where the slope, in degrees, lies within [x, y]. [0, 90] by default.</summary>
        ///<remarks>The slope is the angle of the height gradient, estimated from the heights one heightmap cell away on each side (one-sided at the edges of the Terrain). At a heightmap sample this is the central difference of its neighbors.</remarks>
        public Vector2 slopeRange { get; set; } = new Vector2(0f, 90f);
    }

    ///<summary>Settings for <see cref="TerrainEditUtility.PaintLayers" />.</summary>
    public sealed class TerrainPaintSettings
    {
        ///<summary>Creates settings that blend with a hard edge and no dominant layer filter.</summary>
        public TerrainPaintSettings() {}

        ///<summary>How the rules change the layer weights. <see cref="TerrainLayerBlendMode.Blend" /> by default.</summary>
        public TerrainLayerBlendMode blendMode { get; set; } = TerrainLayerBlendMode.Blend;

        ///<summary>The fraction of the region, from 0 to 1, over which the paint fades at its edge. See <see cref="TerrainRegion.GetWeight" />.</summary>
        public float falloff { get; set; }

        ///<summary>When set, paint only texels whose dominant layer before painting (the layer with the highest weight) is one of these layer indices. Null by default.</summary>
        public int[] dominantLayerFilter { get; set; }

        ///<summary>The distance in meters by which to grow the area that <see cref="dominantLayerFilter" /> selects, measured as the Euclidean distance between alphamap texels. 0 by default.</summary>
        public float dominantLayerExpansion { get; set; }
    }

    ///<summary>What <see cref="TerrainEditUtility.SculptHeights" /> or <see cref="TerrainEditUtility.PaintLayers" /> changed.</summary>
    public readonly struct TerrainEditResult
    {
        internal TerrainEditResult(bool applied, RectInt rect, int changedCount)
        {
            this.applied = applied;
            this.rect = rect;
            this.changedCount = changedCount;
        }

        ///<summary>True when the edit was written. False when it changed nothing, in which case nothing was written and nothing was recorded for undo.</summary>
        public bool applied { get; }

        ///<summary>The rectangle that the edit can change: heightmap samples for <see cref="TerrainEditUtility.SculptHeights" />, alphamap texels for <see cref="TerrainEditUtility.PaintLayers" />. It covers the region, widened to the cells it crosses, and the blend band. Nothing outside it changes.</summary>
        public RectInt rect { get; }

        ///<summary>The number of samples whose stored height changed, or of texels where a layer weight changed by at least half of the alphamap's 8-bit storage step.</summary>
        public int changedCount { get; }
    }
}
