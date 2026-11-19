// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Animations
{
    /// <summary>
    /// The freeform blend space algorithms that <see cref="FreeformBlendSpace2DData"/> supports.
    /// </summary>
    /// <remarks>
    /// Choose the mode that matches the layout of your animations, and pass it to the constructor of
    /// <see cref="FreeformBlendSpace2DData"/>.
    /// <see cref="BlendSpaceUtility.ComputeBlendSpace2DFreeformWeights"/> uses the same mode to compute the weights.
    /// </remarks>
    /// <seealso cref="FreeformBlendSpace2DData"/>
    public enum FreeformBlendSpace2DMode
    {
        /// <summary>
        /// Uses the angle and the magnitude of the input to weight the animations, for example for locomotion with
        /// several speeds in each direction.
        /// </summary>
        Directional,

        /// <summary>
        /// Uses the position of the input in the plane to weight the animations, so the weight of an animation
        /// decreases with its distance from the input.
        /// </summary>
        Cartesian,
    }

    /// <summary>
    /// The precomputed data that the freeform 2D blend space function of <see cref="BlendSpaceUtility"/> needs.
    /// </summary>
    /// <remarks>
    /// The constructor precomputes the geometry of the blend space once for a set of animation positions and for one
    /// <see cref="FreeformBlendSpace2DMode"/>.
    ///
    /// Reuse the same instance for every call of
    /// <see cref="BlendSpaceUtility.ComputeBlendSpace2DFreeformWeights"/>, as long as the animation positions
    /// don't change. The data holds unmanaged memory. Call <see cref="Dispose"/> when you no longer need it.
    ///
    /// The weight function also writes temporary data inside the instance. Don't use the same instance from several
    /// threads or jobs at the same time.
    ///
    /// The following example uses the `Unity.Collections` and `Unity.Mathematics` namespaces. It computes the weights
    /// of several streams for a 2D input, then mixes the streams with a <see cref="Core.PoseMixer"/>.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BlendSpace2DExample.cs#BlendFreeform2D}]]></code>
    /// </example>
    /// <seealso cref="BlendSpaceUtility"/>
    /// <seealso cref="FreeformBlendSpace2DMode"/>
    [GenerateTestsForBurstCompatibility]
    public struct FreeformBlendSpace2DData : IDisposable
    {
        internal UnsafeList<float2> m_ChildValues;
        internal UnsafeList<float> m_ChildMagnitude;
        internal UnsafeList<float2> m_ChildPairVector;
        internal UnsafeList<float> m_ChildPairAvgMagInv;
        internal UnsafeList<int> m_ChildNeighborConcatenatedList;
        internal UnsafeList<int> m_ChildNeighborStartIndexList;
        internal UnsafeList<float2> m_WorkspaceBlendVectors;
        internal UnsafeList<int> m_WorkspaceTempCropArray;
        FreeformBlendSpace2DMode m_Mode;

        /// <summary>
        /// The algorithm this data was precomputed for.
        /// </summary>
        public FreeformBlendSpace2DMode Mode => m_Mode;

        /// <summary>
        /// The number of animations in the blend space.
        /// </summary>
        public int Count => m_ChildValues.Length;

        /// <summary>
        /// Whether the data holds allocated memory.
        /// </summary>
        public bool IsCreated => m_ChildValues.IsCreated;

        /// <summary>
        /// Precomputes the data of a freeform blend space.
        /// </summary>
        /// <remarks>
        /// The constructor copies <paramref name="values"/>, so you can dispose the source array after the call. It
        /// stores data for each pair of animations, so the memory use grows with the square of the number of
        /// animations. Call <see cref="Dispose"/> to release the memory.
        /// </remarks>
        /// <param name="values">The position of each animation in the blend space.</param>
        /// <param name="mode">The freeform algorithm to precompute the data for.</param>
        /// <param name="allocator">The allocator for the data. Call <see cref="Dispose"/> to release it.</param>
        public unsafe FreeformBlendSpace2DData(NativeArray<float2> values, FreeformBlendSpace2DMode mode, Allocator allocator)
        {
            int count = values.Length;
            m_Mode = mode;
            m_ChildValues = new UnsafeList<float2>(count, allocator);
            m_ChildValues.AddRange(values.GetUnsafeReadOnlyPtr(), count);
            m_ChildMagnitude = new UnsafeList<float>(count, allocator);
            m_ChildMagnitude.Resize(count);
            m_ChildPairVector = new UnsafeList<float2>(count * count, allocator);
            m_ChildPairVector.Resize(count * count);
            m_ChildPairAvgMagInv = new UnsafeList<float>(count * count, allocator);
            m_ChildPairAvgMagInv.Resize(count * count);
            m_ChildNeighborConcatenatedList = new UnsafeList<int>(count, allocator);
            m_ChildNeighborStartIndexList = new UnsafeList<int>(count, allocator);
            m_ChildNeighborStartIndexList.Resize(count);
            m_WorkspaceBlendVectors = new UnsafeList<float2>(count, allocator);
            m_WorkspaceBlendVectors.Resize(count);
            m_WorkspaceTempCropArray = new UnsafeList<int>(count, allocator);
            m_WorkspaceTempCropArray.Resize(count);

            PreComputeFreeform(mode, ref this);
        }

        /// <summary>
        /// Releases the memory that the data holds.
        /// </summary>
        /// <remarks>
        /// Call this method when you no longer need the data. After you call it, <see cref="IsCreated"/> is false.
        /// If safety checks are enabled, passing the data to a weight function of
        /// <see cref="BlendSpaceUtility"/> then throws an exception. Without safety checks, the function reads released
        /// memory, and the result is undefined.
        /// </remarks>
        public void Dispose()
        {
            m_ChildValues.Dispose();
            m_ChildMagnitude.Dispose();
            m_ChildPairVector.Dispose();
            m_ChildPairAvgMagInv.Dispose();
            m_ChildNeighborConcatenatedList.Dispose();
            m_ChildNeighborStartIndexList.Dispose();
            m_WorkspaceBlendVectors.Dispose();
            m_WorkspaceTempCropArray.Dispose();
        }

        internal static unsafe void PreComputeFreeform(FreeformBlendSpace2DMode mode, ref FreeformBlendSpace2DData data)
        {
            int count = data.Count;
            if (mode == FreeformBlendSpace2DMode.Directional)
            {
                for (int i = 0; i < count; i++)
                    data.m_ChildMagnitude[i] = math.length(data.m_ChildValues[i]);

                for (int i = 0; i < count; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        int pairIndex = i + j * count;
                        float magI = data.m_ChildMagnitude[i];
                        float magJ = data.m_ChildMagnitude[j];

                        float magSum = magJ + magI;
                        data.m_ChildPairAvgMagInv[pairIndex] = magSum > 0 ? 2.0f / magSum : 0;
                        float mag = (magJ - magI) * data.m_ChildPairAvgMagInv[pairIndex];

                        if (magJ == 0 || magI == 0)
                            data.m_ChildPairVector[pairIndex] = new float2(0, mag);
                        else
                        {
                            float2 valueI = data.m_ChildValues[i];
                            float2 valueJ = data.m_ChildValues[j];
                            float angle = MathUtility.angle(valueI, valueJ);
                            if (valueI.x * valueJ.y - valueI.y * valueJ.x < 0)
                                angle = -angle;
                            data.m_ChildPairVector[pairIndex] = new float2(angle, mag);
                        }
                    }
                }
            }
            else if (mode == FreeformBlendSpace2DMode.Cartesian)
            {
                for (int i = 0; i < count; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        int pairIndex = i + j * count;
                        float2 delta = data.m_ChildValues[j] - data.m_ChildValues[i];
                        data.m_ChildPairAvgMagInv[pairIndex] = 1 / math.dot(delta, delta);
                        data.m_ChildPairVector[pairIndex] = delta;
                    }
                }
            }
            else
                throw new NotImplementedException($"Freeform precompute is not implemented for this mode: {mode}.");

            var weights = new UnsafeList<float>(count, Allocator.Temp);
            var neighbors = new UnsafeList<bool>(count * count, Allocator.Temp);
            weights.Resize(count);
            neighbors.Resize(count * count);
            UnsafeUtility.MemClear(weights.Ptr, sizeof(float) * count);
            UnsafeUtility.MemClear(neighbors.Ptr, sizeof(bool) * count * count);

            // Sample the weights over the bounds of the animation positions, grown by half on each side
            float2 minValues = new float2(10000.0f, 10000.0f);
            float2 maxValues = new float2(-10000.0f, -10000.0f);
            for (int c = 0; c < count; c++)
            {
                minValues = math.min(minValues, data.m_ChildValues[c]);
                maxValues = math.max(maxValues, data.m_ChildValues[c]);
            }

            float2 range = (maxValues - minValues) * 0.5f;
            minValues -= range;
            maxValues += range;

            for (int i = 0; i <= 100; i++)
            {
                for (int j = 0; j <= 100; j++)
                {
                    float x = i * 0.01f;
                    float y = j * 0.01f;
                    var input = new float2(minValues.x * (1 - x) + maxValues.x * x, minValues.y * (1 - y) + maxValues.y * y);
                    if (mode == FreeformBlendSpace2DMode.Directional)
                        BlendSpaceUtility.ComputeBlendSpace2DFreeformDirectionalWeights(input, ref data, weights, true);
                    else
                        BlendSpaceUtility.ComputeBlendSpace2DFreeformCartesianWeights(input, ref data, weights, true);

                    for (int c = 0; c < count; c++)
                    {
                        int neighbor = data.m_WorkspaceTempCropArray[c];
                        if (neighbor >= 0)
                            neighbors[c * count + neighbor] = true;
                    }
                }
            }

            for (int c = 0; c < count; c++)
            {
                data.m_ChildNeighborStartIndexList[c] = data.m_ChildNeighborConcatenatedList.Length;
                for (int d = 0; d < count; d++)
                {
                    if (neighbors[c * count + d])
                        data.m_ChildNeighborConcatenatedList.Add(d);
                }
            }

            weights.Dispose();
            neighbors.Dispose();
        }
    }
}
