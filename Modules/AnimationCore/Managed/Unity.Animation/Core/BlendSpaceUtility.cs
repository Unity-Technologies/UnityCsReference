// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Animations
{
    /// <summary>
    /// Utility class that computes the weights of the animations in a one-dimensional blend space.
    /// </summary>
    /// <remarks>
    /// A blend space mixes several animations according to an input value. Each animation is tied to a value on a
    /// line, and `ComputeBlendSpace1DWeights` converts the input value into one weight per animation. When the input
    /// matches a value exactly, that animation gets a weight of 1. When the input falls between two values, those two
    /// animations share the weight in proportion to the distance, and every other animation gets a weight of 0. When
    /// the input is outside the range of the values and wrapping is off, the first or last animation gets a weight
    /// of 1.
    ///
    /// The following example uses the `Unity.Animations` and `Unity.Collections` namespaces. It computes the weights of
    /// walk, jog, and run streams for a speed, then blends the streams into one output stream.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BlendSpace1DExample.cs#BlendLocomotion}]]></code>
    /// </example>
    /// <seealso cref="AnimationStreamBlendingExtensions"/>
    [GenerateTestsForBurstCompatibility]
    public static class BlendSpaceUtility
    {
        /// <summary>
        /// Computes the weights of a 2D simple directional blend space.
        /// </summary>
        /// <remarks>
        /// Use this method when the animations sit in different directions around an optional center point, for
        /// example walk animations in four directions. For other layouts, use
        /// <see cref="ComputeBlendSpace2DFreeformWeights(float2, in FreeformBlendSpace2DData, NativeArray{float})"/>.
        ///
        /// Each animation has a position in the plane. An animation at `(0, 0)` is the center animation. On each side
        /// of the input direction, the method picks the animation whose direction is closest to the direction of the
        /// input, and blends the two picks with the center animation. The closer the input is to `(0, 0)`, the more
        /// weight the center animation gets. If no animation sits on one side of the input direction, the center gets
        /// all the weight. If no animation sits at `(0, 0)`, the weight of the center is divided equally among all
        /// animations.
        ///
        /// Only the direction of a position, not its distance from the center, selects an animation. If several
        /// animations sit in the same direction from the center, the method picks only one of them.
        ///
        /// Place at most one animation at `(0, 0)`. If two or more animations sit at `(0, 0)` and the input isn't
        /// `(0, 0)`, the method sets every weight to 0. This matches the **2D Simple Directional** blend tree.
        ///
        /// This method needs only the positions of the animations. The freeform method needs a
        /// <see cref="FreeformBlendSpace2DData"/> that you create once for the positions.
        /// </remarks>
        /// <param name="input">The position of the input in the blend space plane, in the same space as the positions
        /// in <paramref name="values"/>.</param>
        /// <param name="values">The position of each animation in the blend space.</param>
        /// <param name="outWeights">The computed weight of each animation.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the lengths of
        /// <paramref name="values"/> and <paramref name="outWeights"/> aren't equal.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ComputeBlendSpace2DSimpleDirectionalWeights(float2 input, NativeArray<float2> values, NativeArray<float> outWeights) =>
            ComputeBlendSpace2DSimpleDirectionalWeights(
                input,
                new UnsafeList<float2>((float2*)values.GetUnsafeReadOnlyPtr(), values.Length),
                new UnsafeList<float>((float*)outWeights.GetUnsafePtr(), outWeights.Length)
            );

        /// <summary>
        /// Computes the weights of a 2D freeform blend space.
        /// </summary>
        /// <remarks>
        /// The method uses <see cref="FreeformBlendSpace2DData.Mode"/> of <paramref name="data"/> to choose the
        /// algorithm. With <see cref="FreeformBlendSpace2DMode.Directional"/>, each animation has an angle and a
        /// magnitude relative to the input. With <see cref="FreeformBlendSpace2DMode.Cartesian"/>, the weight of each
        /// animation decreases with the distance from the input to its position. The method normalizes the weights so
        /// that they sum to 1. When no animation has any weight, all animations get the same weight. An example is the
        /// center of a directional blend space without a center animation.
        ///
        /// The method writes temporary data inside <paramref name="data"/> although the parameter is passed with `in`,
        /// so don't call it with the same data from several threads or jobs at the same time.
        /// </remarks>
        /// <param name="input">The position of the input in the blend space plane.</param>
        /// <param name="data">The precomputed blend space data. Its mode selects the algorithm.</param>
        /// <param name="outWeights">The array that receives the weight of each animation. Its length must equal
        /// <see cref="FreeformBlendSpace2DData.Count"/>.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and
        /// <paramref name="data"/> isn't created or was disposed. Also thrown if safety checks are enabled and the
        /// length of <paramref name="outWeights"/> differs from <see cref="FreeformBlendSpace2DData.Count"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ComputeBlendSpace2DFreeformWeights(float2 input, in FreeformBlendSpace2DData data, NativeArray<float> outWeights)
        {
            Core.ValidateFreeformDataIsCreated(data);
            // The weights use scratch memory of the data, so the data is passed by reference internally
            fixed (FreeformBlendSpace2DData* dataPtr = &data)
            {
                var weights = new UnsafeList<float>((float*)outWeights.GetUnsafePtr(), outWeights.Length);
                if (data.Mode == FreeformBlendSpace2DMode.Directional)
                    ComputeBlendSpace2DFreeformDirectionalWeights(input, ref *dataPtr, weights);
                else
                    ComputeBlendSpace2DFreeformCartesianWeights(input, ref *dataPtr, weights);
            }
        }

        internal static float ComputeBlendSpace2DFreeformDirectionalWeight(float2 input, ref FreeformBlendSpace2DData data, int i, int j)
        {
            int pairIndex = i + j * data.Count;
            float2 vecIJ = data.m_ChildPairVector[pairIndex];
            float2 vecIO = data.m_WorkspaceBlendVectors[i];
            vecIO.y *= data.m_ChildPairAvgMagInv[pairIndex];

            if (data.m_ChildValues[i].Equals(float2.zero))
                vecIJ.x = data.m_WorkspaceBlendVectors[j].x;
            else if (data.m_ChildValues[j].Equals(float2.zero))
                vecIJ.x = data.m_WorkspaceBlendVectors[i].x;
            else if (vecIJ.x == 0 || input.Equals(float2.zero))
                vecIO.x = vecIJ.x;

            return 1 - math.dot(vecIJ, vecIO) / math.dot(vecIJ, vecIJ);
        }

        internal static unsafe void ComputeBlendSpace2DFreeformDirectionalWeights(
            float2 input, ref FreeformBlendSpace2DData data, UnsafeList<float> outWeights, bool preCompute = false)
        {
            Core.ValidateBufferLengthsAreEqual(data.Count, outWeights.Length);

            UnsafeUtility.MemClear(outWeights.Ptr, UnsafeUtility.SizeOf<float>() * outWeights.Length);

            int count = data.Count;
            float magO = math.length(input);

            for (int i = 0; i < count; i++)
            {
                if (input.Equals(float2.zero) || data.m_ChildValues[i].Equals(float2.zero))
                    data.m_WorkspaceBlendVectors[i] = new float2(0, magO - data.m_ChildMagnitude[i]);
                else
                {
                    float2 value = data.m_ChildValues[i];
                    float angle = MathUtility.angle(value, input);
                    if (value.x * input.y - value.y * input.x < 0)
                        angle = -angle;
                    data.m_WorkspaceBlendVectors[i] = new float2(angle, magO - data.m_ChildMagnitude[i]);
                }
            }

            if (preCompute)
            {
                for (int i = 0; i < count; i++)
                {
                    // Fade out over 180 degrees away from example
                    float value = 1 - math.abs(data.m_WorkspaceBlendVectors[i].x) * MathUtility.k_InversePI;
                    data.m_WorkspaceTempCropArray[i] = -1;
                    for (int j = 0; j < count; j++)
                    {
                        if (i == j)
                            continue;

                        float newValue = ComputeBlendSpace2DFreeformDirectionalWeight(input, ref data, i, j);
                        if (newValue <= 0)
                        {
                            value = 0;
                            data.m_WorkspaceTempCropArray[i] = -1;
                            break;
                        }

                        // Used for determining neighbors
                        if (newValue < value)
                            data.m_WorkspaceTempCropArray[i] = j;
                        value = math.min(value, newValue);
                    }
                }

                return;
            }

            for (int i = 0; i < count; i++)
            {
                // Fade out over 180 degrees away from example
                float value = 1 - math.abs(data.m_WorkspaceBlendVectors[i].x) * MathUtility.k_InversePI;

                int startIndex = data.m_ChildNeighborStartIndexList[i];
                int endIndex = (i < count - 1) ? data.m_ChildNeighborStartIndexList[i + 1] : data.m_ChildNeighborConcatenatedList.Length;
                for (int jIndex = startIndex; jIndex < endIndex; jIndex++)
                {
                    int j = data.m_ChildNeighborConcatenatedList[jIndex];
                    float newValue = ComputeBlendSpace2DFreeformDirectionalWeight(input, ref data, i, j);
                    if (newValue <= 0)
                    {
                        value = 0;
                        break;
                    }

                    value = math.min(value, newValue);
                }

                outWeights[i] = value;
            }

            NormalizeFreeformWeights(outWeights);
        }

        internal static unsafe void ComputeBlendSpace2DFreeformCartesianWeights(
            float2 input, ref FreeformBlendSpace2DData data, UnsafeList<float> outWeights, bool preCompute = false)
        {
            Core.ValidateBufferLengthsAreEqual(data.Count, outWeights.Length);

            int count = data.Count;
            for (int i = 0; i < count; i++)
                data.m_WorkspaceBlendVectors[i] = input - data.m_ChildValues[i];

            if (preCompute)
            {
                for (int i = 0; i < count; i++)
                {
                    data.m_WorkspaceTempCropArray[i] = -1;
                    float2 vecIO = data.m_WorkspaceBlendVectors[i];
                    float value = 1;
                    for (int j = 0; j < count; j++)
                    {
                        if (i == j)
                            continue;

                        int pairIndex = i + j * count;
                        float newValue = 1 - math.dot(data.m_ChildPairVector[pairIndex], vecIO) * data.m_ChildPairAvgMagInv[pairIndex];
                        if (newValue <= 0)
                        {
                            value = 0;
                            data.m_WorkspaceTempCropArray[i] = -1;
                            break;
                        }

                        // Used for determining neighbors
                        if (newValue < value)
                            data.m_WorkspaceTempCropArray[i] = j;
                        value = math.min(value, newValue);
                    }
                }

                return;
            }

            for (int i = 0; i < count; i++)
            {
                float2 vecIO = data.m_WorkspaceBlendVectors[i];
                float value = 1;

                int startIndex = data.m_ChildNeighborStartIndexList[i];
                int endIndex = (i < count - 1) ? data.m_ChildNeighborStartIndexList[i + 1] : data.m_ChildNeighborConcatenatedList.Length;
                for (int jIndex = startIndex; jIndex < endIndex; jIndex++)
                {
                    int j = data.m_ChildNeighborConcatenatedList[jIndex];
                    if (i == j)
                        continue;

                    int pairIndex = i + j * count;
                    float newValue = 1 - math.dot(data.m_ChildPairVector[pairIndex], vecIO) * data.m_ChildPairAvgMagInv[pairIndex];
                    if (newValue < 0)
                    {
                        value = 0;
                        break;
                    }

                    value = math.min(value, newValue);
                }

                outWeights[i] = value;
            }

            NormalizeFreeformWeights(outWeights);
        }

        static void NormalizeFreeformWeights(UnsafeList<float> weights)
        {
            float summedWeight = 0;
            for (int i = 0; i < weights.Length; i++)
                summedWeight += weights[i];

            if (summedWeight > 0)
            {
                summedWeight = 1.0f / summedWeight;
                for (int i = 0; i < weights.Length; i++)
                    weights[i] *= summedWeight;
            }
            else
            {
                // No animation has weight, for example in the center without a center animation: share evenly
                float evenWeight = 1.0f / weights.Length;
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = evenWeight;
            }
        }

        internal static unsafe void ComputeBlendSpace2DSimpleDirectionalWeights(float2 input, UnsafeList<float2> values, UnsafeList<float> outWeights)
        {
            Core.ValidateBufferLengthsAreEqual(values.Length, outWeights.Length);

            var length = values.Length;
            UnsafeUtility.MemClear(outWeights.Ptr, UnsafeUtility.SizeOf<float>() * outWeights.Length);

            // Handle fallback
            if (length < 2)
            {
                if (length == 1)
                    outWeights[0] = 1f;
                return;
            }

            // Handle special case when sampled exactly in the middle
            if (math.all(input == float2.zero))
            {
                // If we have a center motion, give that one all the weight
                for (int i = 0; i < length; i++)
                {
                    if (math.all(values[i] == float2.zero))
                    {
                        outWeights[i] = 1;
                        return;
                    }
                }

                // Otherwise divide weight evenly
                float sharedWeight = 1.0f / length;
                for (int i = 0; i < length; i++)
                    outWeights[i] = sharedWeight;
                return;
            }

            int indexA = -1;
            int indexB = -1;
            int indexCenter = -1;
            float maxDotForNegCross = -100000.0f;
            float maxDotForPosCross = -100000.0f;
            for (int i = 0; i < length; i++)
            {
                if (math.all(values[i] == float2.zero))
                {
                    if (indexCenter >= 0)
                        return;
                    indexCenter = i;
                    continue;
                }
                var posNormalized = math.normalize(values[i]);
                float dot = math.dot(posNormalized, input);
                float det = posNormalized.x * input.y - posNormalized.y * input.x;
                if (det > 0)
                {
                    if (dot > maxDotForPosCross)
                    {
                        maxDotForPosCross = dot;
                        indexA = i;
                    }
                }
                else
                {
                    if (dot > maxDotForNegCross)
                    {
                        maxDotForNegCross = dot;
                        indexB = i;
                    }
                }
            }

            float centerWeight = 0.0F;

            float2 a = default;
            float2 b = default;
            float triangleDet = 0.0f;
            bool isInsideTriangle = indexA >= 0 && indexB >= 0;
            if (isInsideTriangle)
            {
                a = values[indexA];
                b = values[indexB];
                triangleDet = b.y * a.x - b.x * a.y;        // Simplified from: (b.y-0)*(a.x-0) + (0-b.x)*(a.y-0);

                // Values 180 degrees or more apart give no triangle around the sampling point
                isInsideTriangle = triangleDet > 0.0f;
            }

            if (!isInsideTriangle)
            {
                // Fallback if sampling point is not inside a triangle
                centerWeight = 1;
            }
            else
            {
                // Calculate weights using barycentric coordinates
                // (formulas from http://en.wikipedia.org/wiki/Barycentric_coordinate_system_%28mathematics%29 )
                float wA = (b.y * input.x - b.x * input.y) / triangleDet; // Simplified from: ((b.y-0)*(l.x-0) + (0-b.x)*(l.y-0)) / triangleDet;
                float wB = (a.x * input.y - a.y * input.x) / triangleDet; // Simplified from: ((0-a.y)*(l.x-0) + (a.x-0)*(l.y-0)) / triangleDet;
                centerWeight = 1 - wA - wB;

                // Clamp to be inside triangle
                if (centerWeight < 0)
                {
                    centerWeight = 0;
                    float sum = wA + wB;
                    wA /= sum;
                    wB /= sum;
                }
                else if (centerWeight > 1)
                {
                    centerWeight = 1;
                    wA = 0;
                    wB = 0;
                }

                // Give weight to the two vertices on the periphery that are closest
                outWeights[indexA] = wA;
                outWeights[indexB] = wB;
            }

            if (indexCenter >= 0)
            {
                outWeights[indexCenter] = centerWeight;
            }
            else
            {
                // Give weight to all children when input is in the center
                float sharedWeight = 1.0f / length;
                for (int i = 0; i < length; i++)
                    outWeights[i] += sharedWeight * centerWeight;
            }
        }

        /// <summary>
        /// Compute 1D blend space weights given an input, wrap information and animation stream values.
        /// </summary>
        /// <param name="input">The input value to convert into weights, in the same space as
        /// <paramref name="values"/>.</param>
        /// <param name="values">The blend space value of each animation stream, in ascending order. The method doesn't
        /// check the order, and returns incorrect weights if the values aren't sorted.</param>
        /// <param name="wrap">If `true`, the input wraps between <paramref name="min"/> and
        /// <paramref name="max"/>.</param>
        /// <param name="min">The lower bound of the wrap range. The method uses it only if
        /// <paramref name="wrap"/> is `true`.</param>
        /// <param name="max">The upper bound of the wrap range. The method uses it only if
        /// <paramref name="wrap"/> is `true`.</param>
        /// <param name="outWeights">The array that receives the weight of each animation stream.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the lengths of
        /// <paramref name="values"/> and <paramref name="outWeights"/> aren't equal.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ComputeBlendSpace1DWeights(float input, NativeArray<float> values, bool wrap, float min, float max, NativeArray<float> outWeights) =>
            ComputeBlendSpace1DWeights(
                input,
                new UnsafeList<float>((float*)values.GetUnsafeReadOnlyPtr(), values.Length),
                wrap, min, max,
                new UnsafeList<float>((float*)outWeights.GetUnsafePtr(), outWeights.Length)
            );

        internal static unsafe void ComputeBlendSpace1DWeights(float input, UnsafeList<float> values, bool wrap, float min, float max, UnsafeList<float> outWeights)
        {
            if (values.Length == 0)
                return;

            Core.ValidateBufferLengthsAreEqual(values.Length, outWeights.Length);
            if (wrap)
            {
                input = math.select(min, max, input < min) + ((input - min) % (max - min));
            }

            int exactMatchIndex = -1;
            int upperBoundIndex = -1;
            int lowerBoundIndex = -1;
            for (int i = 0; i < values.Length; ++i)
            {
                if (input == values[i])
                {
                    exactMatchIndex = i;
                    break;
                }

                if (input < values[i])
                {
                    upperBoundIndex = i;
                    break;
                }
            }

            // if we had an exact match, we don't have upperBoundIndex
            if (exactMatchIndex == -1)
            {
                if (upperBoundIndex == -1 // if upperBoundIndex is not defined, it means the value was higher than the last item
                    || upperBoundIndex == 0 //if the upper bound is the first element, it either means we're looping with the last one, or we're before the first value
                )
                {
                    if (!wrap)
                    {
                        // if we're not in a looping case, then the value is outside the boundaries,
                        // so a single animation should be playing
                        exactMatchIndex = math.select(values.Length - 1, 0, upperBoundIndex == 0);
                    }
                    else
                    {
                        // if we're looping, the high value is element 0 and the low value is the
                        // last element because we're between the looping range
                        lowerBoundIndex = values.Length - 1;
                        upperBoundIndex = 0;
                    }
                }
                else
                {
                    lowerBoundIndex = upperBoundIndex - 1;
                }
            }

            UnsafeUtility.MemClear(outWeights.Ptr, UnsafeUtility.SizeOf<float>() * outWeights.Length);
            if (exactMatchIndex != -1)
            {
                outWeights[exactMatchIndex] = 1f;
            }
            else
            {
                float lowerBoundValue = values[lowerBoundIndex];
                float upperBoundValue = values[upperBoundIndex];

                float ratioBetweenValues;
                if (lowerBoundIndex < upperBoundIndex)
                {
                    ratioBetweenValues = (input - lowerBoundValue) / (upperBoundValue - lowerBoundValue);
                }
                else
                {
                    float rangeBetweenValues = (max - lowerBoundValue) + (upperBoundValue - min);

                    // if input is higher than upperBoundValue, it means the value is between the last
                    // and the max of the looping value range otherwise, input would be between 0
                    // and upperBoundValue since the upper bound is the first element and lower bound is
                    // the last in that case
                    if (input > upperBoundValue)
                    {
                        ratioBetweenValues = (input - lowerBoundValue) / rangeBetweenValues;
                    }
                    else
                    {
                        ratioBetweenValues = (input - min + max - lowerBoundValue) / rangeBetweenValues;
                    }
                }

                // Clamp to 0 and 1 when ratio is smaller than 0.1% of pose to be blended
                ratioBetweenValues = math.select(ratioBetweenValues, 0f, ratioBetweenValues < 1e-3f);
                outWeights[lowerBoundIndex] = 1.0f - ratioBetweenValues;
                outWeights[upperBoundIndex] = ratioBetweenValues;
            }
        }
    }
}
