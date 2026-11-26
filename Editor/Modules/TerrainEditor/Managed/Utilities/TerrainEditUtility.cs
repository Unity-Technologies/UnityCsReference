// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TerrainTools;

namespace UnityEditor.TerrainTools
{
    ///<summary>Edits a region of a Terrain on the CPU: change heights and paint layers by rules.</summary>
    ///<remarks>These methods work on one <see cref="TerrainData" />, without a Terrain component or a camera, and do not render anything. Positions are in meters on the X and Z axes of the Terrain's local space (see <see cref="TerrainRegion" />), heights are in meters above the base of the Terrain, and slopes are in degrees.
    ///
    ///Every edit widens its region to cover the cells of the grid it changes, changes nothing outside the region and its blend band, and reports what it changed. An edit that changes something is recorded in the current undo group under the name "Sculpt Terrain Heights" or "Paint Terrain Layers": call <see cref="Undo.IncrementCurrentGroup" /> first to make it an undo step of its own, and <see cref="Undo.SetCurrentGroupName" /> afterwards to rename it. An edit that changes nothing writes nothing and records nothing. <see cref="GetHeightmapWeights" /> and <see cref="GetAlphamapWeights" /> return the weights an edit of a region applies at each sample or texel, to read the area it covers.
    ///
    ///Heights are stored with 16-bit precision (a step of <see cref="TerrainData.size" />.y / 32766) and layer weights with 8-bit precision, so results are exact only to those steps. Alphamap texel i lies at i * size / (alphamapResolution - 1), so the edge texels lie on the edges of the Terrain, as the Terrain shaders and <see cref="PaintContext" /> sample them.
    ///
    ///The methods edit a single Terrain tile and do not blend across neighbors: changing the samples on the edge of a tile opens a seam with its neighbors unless they are edited to match. They do not save the TerrainData asset; the edited TerrainData is marked dirty. If the heightmap was changed on the GPU, for example through <see cref="PaintContext" /> or <see cref="TerrainData.CopyActiveRenderTextureToHeightmap" />, call <see cref="TerrainData.SyncHeightmap" /> first, otherwise these methods read, and <see cref="SculptHeights" /> overwrites, out-of-date heights.
    ///
    ///Each call does all of its work on the main thread and does not limit how long that takes, which can be seconds for a large region of a high-resolution Terrain. In Play mode the changes also persist in the TerrainData asset: to keep the asset unchanged, edit a copy of the TerrainData and assign it to the Terrain and its TerrainCollider.</remarks>
    public static class TerrainEditUtility
    {
        // Heights are stored as whole steps of size.y / 32766, and a height counts as changed when its stored step changes.
        const int k_HeightStorageSteps = 32766;
        // A layer weight counts as changed when it moves by at least half of the alphamap's 8-bit storage step.
        const float k_ChangedWeight = 0.5f / 255f;
        // Below this total weight of the other layers there is nothing to take weight from.
        const float k_EmptyOthers = 1e-3f;

        const string k_SculptUndoName = "Sculpt Terrain Heights";
        const string k_PaintUndoName = "Paint Terrain Layers";

        // ---- SculptHeights ----------------------------------------------------------------------------------------------

        ///<summary>Changes the heights of a Terrain inside a region.</summary>
        ///<remarks>The change applies fully inside the region and either fades at its edge over <see cref="TerrainSculptSettings.falloff" />, or eases back to the untouched ground over <see cref="TerrainSculptSettings.blendDistance" /> outside it. Heights are clamped to the range of the Terrain. Call this method on the main thread.</remarks>
        ///<param name="terrainData">The TerrainData to edit.</param>
        ///<param name="region">The region to change.</param>
        ///<param name="settings">How to change the heights.</param>
        ///<returns>What changed.</returns>
        public static TerrainEditResult SculptHeights(TerrainData terrainData, TerrainRegion region, TerrainSculptSettings settings)
        {
            CheckArguments(terrainData, region);
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            var op = settings.operation;
            if (op < TerrainSculptOperation.Raise || op > TerrainSculptOperation.Smooth)
                throw new ArgumentOutOfRangeException(nameof(settings), op, "Unknown sculpt operation.");
            if (settings.flattenReference < TerrainFlattenReference.Mean || settings.flattenReference > TerrainFlattenReference.Maximum)
                throw new ArgumentOutOfRangeException(nameof(settings), settings.flattenReference, "Unknown flatten reference.");
            TerrainRegion.CheckFalloff(settings.falloff);

            var size = terrainData.size;
            float diagonal = Mathf.Sqrt(size.x * size.x + size.z * size.z);
            float blend = settings.blendDistance;
            if (!(blend >= 0f && blend <= diagonal))
                throw new ArgumentOutOfRangeException(nameof(settings), blend, $"blendDistance must be between 0 and the diagonal of the Terrain ({diagonal} m).");
            if (blend > 0f && settings.falloff > 0f)
                throw new ArgumentException("falloff and blendDistance cannot be combined: falloff fades the change inside the region, blendDistance outside it.", nameof(settings));

            float terrainHeight = Mathf.Max(size.y, 1e-6f);
            float target = float.NaN, delta = 0f;
            switch (op)
            {
                case TerrainSculptOperation.Raise:
                case TerrainSculptOperation.Lower:
                    if (!(settings.amount > 0f && float.IsFinite(settings.amount)))
                        throw new ArgumentOutOfRangeException(nameof(settings), settings.amount, $"{op} needs an amount greater than zero, in meters.");
                    delta = (op == TerrainSculptOperation.Raise ? settings.amount : -settings.amount) / terrainHeight;
                    break;
                case TerrainSculptOperation.Set:
                    if (float.IsNaN(settings.height))
                        throw new ArgumentException("Set needs a height.", nameof(settings));
                    if (!(settings.height >= 0f && settings.height <= size.y))
                        throw new ArgumentOutOfRangeException(nameof(settings), settings.height, $"The height must be between 0 and the height of the Terrain ({size.y} m).");
                    target = settings.height / terrainHeight;
                    break;
                case TerrainSculptOperation.Flatten:
                    if (!float.IsNaN(settings.height))
                        throw new ArgumentException("Flatten takes its height from the region (flattenReference). To move the heights to a given height, use Set.", nameof(settings));
                    break;
                case TerrainSculptOperation.Smooth:
                    if (!(settings.smoothRadius >= 0f && settings.smoothRadius <= diagonal))
                        throw new ArgumentOutOfRangeException(nameof(settings), settings.smoothRadius, $"smoothRadius must be between 0 and the diagonal of the Terrain ({diagonal} m).");
                    break;
            }

            int res = terrainData.heightmapResolution;
            float cellX = size.x / (res - 1), cellZ = size.z / (res - 1);
            float minCell = Mathf.Max(Mathf.Min(cellX, cellZ), 1e-6f);
            var effective = region.ExpandToCoverCells(Mathf.Max(cellX, cellZ));
            float smoothRadius = op == TerrainSculptOperation.Smooth ? (settings.smoothRadius > 0f ? settings.smoothRadius : 2f * Mathf.Max(cellX, cellZ)) : 0f;
            // The samples the edit can change: the region and its blend band. Smoothing also reads the samples around them.
            var changeable = effective.GetPointWindow(size, res, PadFor(blend, minCell));
            // The blend band eases back from the samples inside the region. A region with no sample on the Terrain, such as
            // one off the Terrain or an empty intersection, has no edge to ease back from, so nothing changes.
            if (blend > 0f && !AnySampleInside(effective, effective.GetPointWindow(size, res, 0), cellX, cellZ))
                return new TerrainEditResult(false, changeable, 0);
            var window = effective.GetPointWindow(size, res, PadFor(blend, minCell) + PadFor(smoothRadius, minCell));
            int w = window.width, h = window.height;
            // Read once and edited in place: each sample only reads its own height (smoothing reads its own blurred copy).
            var heights = terrainData.GetHeights(window.x, window.y, w, h);

            // Weight of a sample: falloff inside the region, or the blend band outside it.
            float Weight(int x, int z, out bool inside)
            {
                var p = new Vector2((window.x + x) * cellX, (window.y + z) * cellZ);
                float f = effective.Evaluate(p, settings.falloff, blend, out inside, out float outside);
                if (!inside && blend > 0f && outside < blend)
                    f = Mathf.SmoothStep(1f, 0f, outside / blend);
                return f;
            }

            // Flatten needs the height of the region before it changes anything, so it alone takes two passes and keeps
            // the weights; the other operations weigh and change each sample in one pass.
            int affected = 0;
            float[,] weights = null;
            if (op == TerrainSculptOperation.Flatten)
            {
                weights = new float[h, w];
                var inside = new HeightAccumulator();
                var any = new HeightAccumulator();
                for (int z = 0; z < h; z++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float f = weights[z, x] = Weight(x, z, out bool isInside);
                        // The region's own samples (not the blend band, regardless of the falloff) define its mean, min and max.
                        if (isInside)
                            inside.Add(heights[z, x]);
                        if (f > 0f)
                        {
                            affected++;
                            any.Add(heights[z, x]);
                        }
                    }
                }
                if (affected == 0)
                    return new TerrainEditResult(false, changeable, 0);
                var reference = inside.count > 0 ? inside : any;
                target = settings.flattenReference == TerrainFlattenReference.Minimum ? reference.min
                    : settings.flattenReference == TerrainFlattenReference.Maximum ? reference.max
                    : reference.mean;
            }
            float[,] smoothed = op == TerrainSculptOperation.Smooth
                ? BoxBlur(heights, Mathf.Max(1, Mathf.RoundToInt(smoothRadius / cellX)), Mathf.Max(1, Mathf.RoundToInt(smoothRadius / cellZ)))
                : null;

            int changed = 0;
            int changedX0 = w, changedZ0 = h, changedX1 = -1, changedZ1 = -1;
            for (int z = 0; z < h; z++)
            {
                for (int x = 0; x < w; x++)
                {
                    float f = weights != null ? weights[z, x] : Weight(x, z, out _);
                    if (f <= 0f)
                        continue;
                    float current = heights[z, x];
                    float goal = smoothed != null ? smoothed[z, x] : float.IsNaN(target) ? current + delta : target;
                    float next = Mathf.Clamp01(Mathf.Lerp(current, goal, f));
                    heights[z, x] = next;
                    if (StorageStep(next) != StorageStep(current))
                    {
                        changed++;
                        changedX0 = Math.Min(changedX0, x); changedX1 = Math.Max(changedX1, x);
                        changedZ0 = Math.Min(changedZ0, z); changedZ1 = Math.Max(changedZ1, z);
                    }
                }
            }
            if (changed == 0)
                return new TerrainEditResult(false, changeable, 0);

            // Write only the rectangle of samples that changed: SetHeights rebuilds the collision and the LOD of whatever it is given.
            Undo.RegisterCompleteObjectUndo(terrainData, k_SculptUndoName);
            int cw = changedX1 - changedX0 + 1, ch = changedZ1 - changedZ0 + 1;
            if (cw == w && ch == h)
                terrainData.SetHeights(window.x, window.y, heights);
            else
            {
                var block = new float[ch, cw];
                for (int z = 0; z < ch; z++)
                    Array.Copy(heights, (changedZ0 + z) * w + changedX0, block, z * cw, cw);
                terrainData.SetHeights(window.x + changedX0, window.y + changedZ0, block);
            }
            return new TerrainEditResult(true, changeable, changed);
        }

        // Whether any grid point of the window lies inside the region. Stops at the first one.
        static bool AnySampleInside(TerrainRegion region, RectInt window, float spacingX, float spacingZ)
        {
            for (int z = window.y; z < window.yMax; z++)
                for (int x = window.x; x < window.xMax; x++)
                    if (region.Contains(new Vector2(x * spacingX, z * spacingZ)))
                        return true;
            return false;
        }

        // Count, sum, min and max of heights.
        struct HeightAccumulator
        {
            public int count;
            public double sum;
            public float min, max;

            public float mean => (float)(sum / count);

            public void Add(float value)
            {
                min = count == 0 ? value : Mathf.Min(min, value);
                max = count == 0 ? value : Mathf.Max(max, value);
                sum += value;
                count++;
            }
        }

        // ---- PaintLayers ------------------------------------------------------------------------------------------------

        ///<summary>Paints terrain layers inside a region by rules.</summary>
        ///<remarks>The rules apply in order to every alphamap texel in the region, and later rules win. A rule applies to a texel when its height and slope lie in the ranges of the rule and, when <see cref="TerrainPaintSettings.dominantLayerFilter" /> is set, when its dominant layer before painting is one of the filter layers.
        ///
        ///The weights of every texel keep summing to 1 within the alphamap's 8-bit precision: the other layers shrink or grow in proportion to their weights. When the other layers of a texel have no weight, as on a Terrain with a single layer, there is nothing to take weight from: the texel becomes fully the rule's layer whatever the opacity, as with the Paint Texture brush, so a rule cannot lower the only layer of a texel. Texels outside the region are not changed. Call this method on the main thread.</remarks>
        ///<param name="terrainData">The TerrainData to paint. It must have at least one terrain layer.</param>
        ///<param name="region">The region to paint.</param>
        ///<param name="rules">The rules, at least one. To paint a single layer everywhere in the region, pass one rule with that layer index.</param>
        ///<param name="settings">The blend mode, falloff and dominant layer filter. Null uses the defaults.</param>
        ///<returns>What changed.</returns>
        public static TerrainEditResult PaintLayers(TerrainData terrainData, TerrainRegion region, ReadOnlySpan<TerrainLayerPaintRule> rules, TerrainPaintSettings settings = null)
        {
            CheckArguments(terrainData, region);
            if (rules.Length == 0)
                throw new ArgumentException("Pass at least one rule.", nameof(rules));
            settings = settings ?? new TerrainPaintSettings();
            int layerCount = terrainData.alphamapLayers;
            if (layerCount == 0)
                throw new InvalidOperationException("The TerrainData has no terrain layers to paint. Add a layer first.");
            // Copied: the caller may change the rule objects while we work.
            var plan = new PaintRule[rules.Length];
            bool needHeights = false, needSlopes = false;
            for (int i = 0; i < plan.Length; i++)
            {
                var rule = rules[i] ?? throw new ArgumentException($"Rule {i} is null.", nameof(rules));
                if (rule.layerIndex < 0 || rule.layerIndex >= layerCount)
                    throw new ArgumentOutOfRangeException(nameof(rules), rule.layerIndex, $"Rule {i} paints layer {rule.layerIndex}, but the TerrainData has {layerCount} layer(s).");
                if (!(rule.opacity >= 0f && rule.opacity <= 1f))
                    throw new ArgumentOutOfRangeException(nameof(rules), rule.opacity, $"The opacity of rule {i} must be between 0 and 1.");
                // Written as !(min <= max) so that NaN fails too.
                if (!(rule.heightRange.x <= rule.heightRange.y))
                    throw new ArgumentException($"The heightRange of rule {i} must be [min, max] with max >= min.", nameof(rules));
                if (!(rule.slopeRange.x <= rule.slopeRange.y))
                    throw new ArgumentException($"The slopeRange of rule {i} must be [min, max] with max >= min.", nameof(rules));
                plan[i] = new PaintRule(rule);
                needHeights |= plan[i].byHeight;
                needSlopes |= plan[i].bySlope;
            }
            if (settings.blendMode < TerrainLayerBlendMode.Blend || settings.blendMode > TerrainLayerBlendMode.Set)
                throw new ArgumentOutOfRangeException(nameof(settings), settings.blendMode, "Unknown blend mode.");
            TerrainRegion.CheckFalloff(settings.falloff);
            float expansion = settings.dominantLayerExpansion;
            if (!(expansion >= 0f && float.IsFinite(expansion)))
                throw new ArgumentOutOfRangeException(nameof(settings), expansion, "dominantLayerExpansion must be zero or a finite number of meters.");
            var filterLayers = settings.dominantLayerFilter;
            if (filterLayers != null)
            {
                foreach (var l in filterLayers)
                    if (l < 0 || l >= layerCount)
                        throw new ArgumentOutOfRangeException(nameof(settings), l, $"dominantLayerFilter names layer {l}, but the TerrainData has {layerCount} layer(s).");
            }
            else if (expansion > 0f)
                throw new ArgumentException("dominantLayerExpansion only applies together with dominantLayerFilter.", nameof(settings));

            var size = terrainData.size;
            int res = terrainData.alphamapResolution;
            float texelX = size.x / (res - 1), texelZ = size.z / (res - 1);
            var effective = region.ExpandToCoverCells(Mathf.Max(texelX, texelZ));
            var window = effective.GetPointWindow(size, res, 0);
            int w = window.width, h = window.height;
            var maps = terrainData.GetAlphamaps(window.x, window.y, w, h);
            int channels = maps.GetLength(2);

            // The filter reads the dominant layer before painting, around the window by the expansion distance.
            bool[] filter = null;
            RectInt filterWindow = window;
            if (filterLayers != null)
            {
                filterWindow = effective.GetPointWindow(size, res, PadFor(expansion, Mathf.Max(Mathf.Min(texelX, texelZ), 1e-6f)));
                var filterMaps = filterWindow.Equals(window) ? maps : terrainData.GetAlphamaps(filterWindow.x, filterWindow.y, filterWindow.width, filterWindow.height);
                filter = DominantLayerMask(filterMaps, filterLayers, expansion, texelX, texelZ);
            }

            // Heights and slopes at the texels, read from the heightmap a row of texels at a time.
            var sampler = needHeights || needSlopes ? new TerrainHeightSampler(terrainData, window.x, w, texelX, needSlopes) : null;

            bool set = settings.blendMode == TerrainLayerBlendMode.Set;
            int changed = 0;
            var original = new float[channels];
            for (int z = 0; z < h; z++)
            {
                sampler?.ReadRow((window.y + z) * texelZ);
                for (int x = 0; x < w; x++)
                {
                    float f = effective.Evaluate(new Vector2((window.x + x) * texelX, (window.y + z) * texelZ), settings.falloff, 0f, out _, out _);
                    if (f <= 0f)
                        continue;
                    if (filter == null || filter[(window.y + z - filterWindow.y) * filterWindow.width + window.x + x - filterWindow.x])
                    {
                        for (int l = 0; l < channels; l++)
                            original[l] = maps[z, x, l];
                        bool painted = false;
                        float slope = float.NaN;
                        for (int k = 0; k < plan.Length; k++)
                        {
                            ref readonly var rule = ref plan[k];
                            if (rule.byHeight)
                            {
                                float height = sampler.Height(x);
                                if (height < rule.heightRange.x || height > rule.heightRange.y)
                                    continue;
                            }
                            if (rule.bySlope)
                            {
                                if (float.IsNaN(slope))
                                    slope = sampler.Slope(x);
                                if (slope < rule.slopeRange.x || slope > rule.slopeRange.y)
                                    continue;
                            }
                            int layer = rule.layer;
                            float before = maps[z, x, layer], others = 0f;
                            for (int l = 0; l < channels; l++)
                                if (l != layer)
                                    others += maps[z, x, l];
                            float after;
                            if (others > k_EmptyOthers)
                            {
                                after = Mathf.Clamp01(set ? before + f * (rule.opacity - before) : before + rule.opacity * f * (1f - before));
                                float scale = (1f - after) / others;
                                for (int l = 0; l < channels; l++)
                                    if (l != layer)
                                        maps[z, x, l] *= scale;
                            }
                            else
                            {
                                // Nothing to take weight from: the texel becomes fully this layer, as with the brush.
                                after = 1f;
                                for (int l = 0; l < channels; l++)
                                    if (l != layer)
                                        maps[z, x, l] = 0f;
                            }
                            maps[z, x, layer] = after;
                            painted = true;
                        }
                        // The texel changed when its weights after every rule differ from before: small changes of several
                        // rules can add up, and a later rule can undo an earlier one.
                        if (painted)
                            for (int l = 0; l < channels; l++)
                                if (Mathf.Abs(maps[z, x, l] - original[l]) >= k_ChangedWeight)
                                {
                                    changed++;
                                    break;
                                }
                    }
                }
            }

            if (changed == 0)
                return new TerrainEditResult(false, window, 0);

            // The alphamap pixels live in separate textures; recording only the TerrainData would not bring the paint back.
            var recorded = new List<UnityEngine.Object> { terrainData };
            recorded.AddRange(terrainData.alphamapTextures);
            Undo.RegisterCompleteObjectUndo(recorded.ToArray(), k_PaintUndoName);
            terrainData.SetAlphamaps(window.x, window.y, maps);
            return new TerrainEditResult(true, window, changed);
        }

        // A paint rule as the loop uses it, with whether its ranges restrict anything worked out once.
        readonly struct PaintRule
        {
            public readonly int layer;
            public readonly float opacity;
            public readonly Vector2 heightRange, slopeRange;
            public readonly bool byHeight, bySlope;

            public PaintRule(TerrainLayerPaintRule rule)
            {
                layer = rule.layerIndex;
                opacity = rule.opacity;
                heightRange = rule.heightRange;
                slopeRange = rule.slopeRange;
                byHeight = !float.IsNegativeInfinity(heightRange.x) || !float.IsPositiveInfinity(heightRange.y);
                bySlope = slopeRange.x > 0f || slopeRange.y < 90f;
            }
        }

        // ---- Weights ----------------------------------------------------------------------------------------------------

        ///<summary>Gets the weight that an edit of a region applies at each heightmap sample it covers.</summary>
        ///<remarks>These are the weights that <see cref="SculptHeights" /> applies with the same falloff and no blend distance: the region is widened to the heightmap cells it crosses in the same way, and sample (x, z) of the heightmap lies at (x, z) * size / (heightmapResolution - 1). A sample with a weight of 0 lies outside the region. Read the heights of the same rectangle with <see cref="TerrainData.GetHeights" />.</remarks>
        ///<param name="terrainData">The TerrainData whose heightmap grid to use.</param>
        ///<param name="region">The region.</param>
        ///<param name="falloff">The fraction of the region, from 0 to 1, over which the weight fades at its edge. See <see cref="TerrainRegion.GetWeight" />.</param>
        ///<param name="sampleRect">The rectangle of heightmap samples that the weights cover.</param>
        ///<returns>The weights, from 0 to 1, indexed [z, x] from the corner of <paramref name="sampleRect" />, as <see cref="TerrainData.GetHeights" /> returns heights.</returns>
        public static float[,] GetHeightmapWeights(TerrainData terrainData, TerrainRegion region, float falloff, out RectInt sampleRect)
        {
            CheckArguments(terrainData, region);
            TerrainRegion.CheckFalloff(falloff);
            var size = terrainData.size;
            int res = terrainData.heightmapResolution;
            float cellX = size.x / (res - 1), cellZ = size.z / (res - 1);
            var effective = region.ExpandToCoverCells(Mathf.Max(cellX, cellZ));
            sampleRect = effective.GetPointWindow(size, res, 0);
            return Weights(effective, sampleRect, cellX, cellZ, falloff);
        }

        ///<summary>Gets the weight that an edit of a region applies at each alphamap texel it covers.</summary>
        ///<remarks>These are the weights that <see cref="PaintLayers" /> applies with the same falloff: the region is widened to the alphamap texels it crosses in the same way, and texel (x, z) lies at (x, z) * size / (alphamapResolution - 1). A texel with a weight of 0 lies outside the region. Read the layer weights of the same rectangle with <see cref="TerrainData.GetAlphamaps" />.</remarks>
        ///<param name="terrainData">The TerrainData whose alphamap grid to use.</param>
        ///<param name="region">The region.</param>
        ///<param name="falloff">The fraction of the region, from 0 to 1, over which the weight fades at its edge. See <see cref="TerrainRegion.GetWeight" />.</param>
        ///<param name="texelRect">The rectangle of alphamap texels that the weights cover.</param>
        ///<returns>The weights, from 0 to 1, indexed [z, x] from the corner of <paramref name="texelRect" />, as <see cref="TerrainData.GetAlphamaps" /> returns layer weights.</returns>
        public static float[,] GetAlphamapWeights(TerrainData terrainData, TerrainRegion region, float falloff, out RectInt texelRect)
        {
            CheckArguments(terrainData, region);
            TerrainRegion.CheckFalloff(falloff);
            var size = terrainData.size;
            int res = terrainData.alphamapResolution;
            float texelX = size.x / (res - 1), texelZ = size.z / (res - 1);
            var effective = region.ExpandToCoverCells(Mathf.Max(texelX, texelZ));
            texelRect = effective.GetPointWindow(size, res, 0);
            return Weights(effective, texelRect, texelX, texelZ, falloff);
        }

        static float[,] Weights(TerrainRegion region, RectInt window, float spacingX, float spacingZ, float falloff)
        {
            var weights = new float[window.height, window.width];
            for (int z = 0; z < window.height; z++)
                for (int x = 0; x < window.width; x++)
                    weights[z, x] = region.Evaluate(new Vector2((window.x + x) * spacingX, (window.y + z) * spacingZ), falloff, 0f, out _, out _);
            return weights;
        }

        // ---- helpers ----------------------------------------------------------------------------------------------------

        static void CheckArguments(TerrainData terrainData, TerrainRegion region)
        {
            if (terrainData == null)
                throw new ArgumentNullException(nameof(terrainData));
            if (region == null)
                throw new ArgumentNullException(nameof(region));
        }


        static int DominantLayer(float[,,] maps, int z, int x, int layerCount)
        {
            int best = 0;
            for (int l = 1; l < layerCount; l++)
                if (maps[z, x, l] > maps[z, x, best])
                    best = l;
            return best;
        }

        // Grid points to read beyond a region to reach 'meters' past it, on a grid 'spacing' meters apart.
        static int PadFor(float meters, float spacing) => meters > 0f ? Mathf.CeilToInt(meters / spacing) + 1 : 0;

        // The 16-bit step a normalized height is stored as, rounded as TerrainData.SetHeights rounds it.
        static int StorageStep(float normalizedHeight) => Mathf.FloorToInt(normalizedHeight * k_HeightStorageSteps + 0.5f);

        // The angle in degrees of a height gradient.
        internal static float SlopeDegrees(float dx, float dz) => MathF.Atan(MathF.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;

        // Separable box blur with clamped edges: a running mean along each row, then down the columns a row at a time
        // with a running sum per column. The cost does not grow with the radius, and both passes read memory in order.
        internal static float[,] BoxBlur(float[,] source, int radiusX, int radiusZ)
        {
            int h = source.GetLength(0), w = source.GetLength(1);
            var result = new float[h, w];
            float nx = 2 * radiusX + 1, nz = 2 * radiusZ + 1;
            for (int z = 0; z < h; z++)
            {
                double s = 0;
                for (int k = -radiusX; k <= radiusX; k++)
                    s += source[z, Mathf.Clamp(k, 0, w - 1)];
                for (int x = 0; x < w; x++)
                {
                    result[z, x] = (float)(s / nx);
                    s += source[z, Math.Min(x + radiusX + 1, w - 1)] - source[z, Math.Max(x - radiusX, 0)];
                }
            }

            // The column pass writes each row over the row means, so it keeps the rows it still has to subtract.
            var sums = new double[w];
            for (int k = -radiusZ; k <= radiusZ; k++)
            {
                int row = Mathf.Clamp(k, 0, h - 1);
                for (int x = 0; x < w; x++)
                    sums[x] += result[row, x];
            }
            int keptRows = Math.Min(radiusZ + 1, h);
            var kept = new float[keptRows, w];
            for (int z = 0; z < h; z++)
            {
                Array.Copy(result, z * w, kept, z % keptRows * w, w);
                int add = Math.Min(z + radiusZ + 1, h - 1), remove = Math.Max(z - radiusZ, 0) % keptRows;
                for (int x = 0; x < w; x++)
                {
                    float mean = (float)(sums[x] / nz);
                    sums[x] += result[add, x] - kept[remove, x];
                    result[z, x] = mean;
                }
            }
            return result;
        }

        // Where the dominant layer of the given alphamap window is one of 'layers', grown outwards by 'expansion' meters
        // (exact Euclidean distance between texels). Indexed [z * width + x] over the window.
        internal static bool[] DominantLayerMask(float[,,] maps, int[] layers, float expansion, float spacingX, float spacingZ)
        {
            int h = maps.GetLength(0), w = maps.GetLength(1), channels = maps.GetLength(2);
            var allowed = new bool[channels];
            foreach (var l in layers)
                if (l >= 0 && l < channels)
                    allowed[l] = true;
            var inside = new bool[w * h];
            for (int z = 0; z < h; z++)
                for (int x = 0; x < w; x++)
                    inside[z * w + x] = allowed[DominantLayer(maps, z, x, channels)];
            if (expansion > 0f)
            {
                var distance = DistanceTransform(inside, w, h, spacingX, spacingZ);
                for (int i = 0; i < inside.Length; i++)
                    if (distance[i] <= expansion)
                        inside[i] = true;
            }
            return inside;
        }

        // Euclidean distance from every cell to the nearest inside cell (Felzenszwalb and Huttenlocher), with separate x and z spacing.
        static float[] DistanceTransform(bool[] inside, int w, int h, float spacingX, float spacingZ)
        {
            const float k_Far = 1e20f;
            int n = Math.Max(w, h);
            var g = new float[w * h];
            var f = new float[n];
            var d = new float[n];
            var v = new int[n];
            var bounds = new float[n + 1];
            for (int z = 0; z < h; z++)
            {
                for (int x = 0; x < w; x++)
                    f[x] = inside[z * w + x] ? 0f : k_Far;
                DistanceTransform1D(f, w, spacingX, d, v, bounds);
                for (int x = 0; x < w; x++)
                    g[z * w + x] = d[x];
            }
            // The column pass reads a column of g into f before writing it back, so it can overwrite g in place.
            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < h; z++)
                    f[z] = g[z * w + x];
                DistanceTransform1D(f, h, spacingZ, d, v, bounds);
                for (int z = 0; z < h; z++)
                    g[z * w + x] = Mathf.Sqrt(d[z]);
            }
            return g;
        }

        // 1D squared distance transform of the sampled function f (lower envelope of parabolas), samples 'spacing' apart.
        static void DistanceTransform1D(float[] f, int n, float spacing, float[] d, int[] v, float[] bounds)
        {
            int k = 0;
            v[0] = 0;
            bounds[0] = float.NegativeInfinity;
            bounds[1] = float.PositiveInfinity;
            for (int q = 1; q < n; q++)
            {
                float s = Intersect(f, q, v[k], spacing);
                while (s <= bounds[k])
                {
                    k--;
                    s = Intersect(f, q, v[k], spacing);
                }
                k++;
                v[k] = q;
                bounds[k] = s;
                bounds[k + 1] = float.PositiveInfinity;
            }
            k = 0;
            for (int q = 0; q < n; q++)
            {
                float qs = q * spacing;
                while (bounds[k + 1] < qs)
                    k++;
                float dx = qs - v[k] * spacing;
                d[q] = dx * dx + f[v[k]];
            }
        }

        static float Intersect(float[] f, int q, int p, float spacing)
        {
            float qs = q * spacing, ps = p * spacing;
            return ((f[q] + qs * qs) - (f[p] + ps * ps)) / (2f * qs - 2f * ps);
        }
    }

    // Heights in meters and slopes in degrees at a row of evenly spaced positions, read from the heightmap with
    // TerrainData.GetInterpolatedHeights (bilinear). The slope is the angle of the gradient from the heights one
    // heightmap cell away on each side, clamped to the Terrain; at a heightmap sample this is the central difference
    // of its neighbors.
    internal sealed class TerrainHeightSampler
    {
        // Rows of m_Heights: the positions, then the positions one heightmap cell west, east, south and north of them.
        const int k_Center = 0, k_West = 1, k_East = 2, k_South = 3, k_North = 4;

        readonly TerrainData m_Data;
        readonly Vector3 m_Size;
        readonly float m_FirstU, m_StepU, m_Cell;   // normalized: 1 is the size of the Terrain
        readonly float[,] m_Heights;
        readonly float[] m_SpanX;
        float m_SpanZ;

        // 'count' positions 'spacing' meters apart along x, from 'first' * spacing.
        public TerrainHeightSampler(TerrainData terrainData, int first, int count, float spacing, bool withSlopes)
        {
            m_Data = terrainData;
            m_Size = terrainData.size;
            m_StepU = spacing / m_Size.x;
            m_FirstU = first * m_StepU;
            m_Cell = 1f / (terrainData.heightmapResolution - 1);
            m_Heights = new float[withSlopes ? 5 : 1, count];
            if (!withSlopes)
                return;
            m_SpanX = new float[count];
            for (int i = 0; i < count; i++)
            {
                float u = m_FirstU + i * m_StepU;
                m_SpanX[i] = (Mathf.Min(u + m_Cell, 1f) - Mathf.Max(u - m_Cell, 0f)) * m_Size.x;
            }
        }

        // Reads the row of positions at 'z' meters.
        public void ReadRow(float z)
        {
            float v = z / m_Size.z;
            Read(k_Center, 0f, v);
            if (m_SpanX == null)
                return;
            Read(k_West, -m_Cell, v);
            Read(k_East, m_Cell, v);
            Read(k_South, 0f, v - m_Cell);
            Read(k_North, 0f, v + m_Cell);
            m_SpanZ = (Mathf.Min(v + m_Cell, 1f) - Mathf.Max(v - m_Cell, 0f)) * m_Size.z;
        }

        void Read(int row, float offsetU, float v) => m_Data.GetInterpolatedHeights(m_Heights, 0, row, m_FirstU + offsetU, v, m_Heights.GetLength(1), 1, m_StepU, 0f);

        public float Height(int i) => m_Heights[k_Center, i];

        public float Slope(int i)
        {
            float dx = m_SpanX[i] > 0f ? (m_Heights[k_East, i] - m_Heights[k_West, i]) / m_SpanX[i] : 0f;
            float dz = m_SpanZ > 0f ? (m_Heights[k_North, i] - m_Heights[k_South, i]) / m_SpanZ : 0f;
            return TerrainEditUtility.SlopeDegrees(dx, dz);
        }
    }
}
