// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Mathematics
{
    // Mirrors TransformType in Runtime/Math/TransformType.h
    internal enum TransformType : byte
    {
        kNoScaleTransform          = 0,
        kUniformScaleTransform     = 1 << 0,
        kNonUniformScaleTransform  = 1 << 1,
        kOddNegativeScaleTransform = 1 << 2,
    }

    public static partial class math
    {
        // Mirrors TransformInternal::CalculateTransformType in Runtime/Transform/TransformHierarchy.cpp. Native clears
        // bits with `&= ~(!flag * BIT)`; C# will not apply ! to an integer, so the bits are OR'd in positively instead.
        // Same result.
        [VisibleToOtherModules("UnityEngine.EntitiesModule")]
        internal static byte CalculateTransformType(float3 scale)
        {
            const float epsilon = 0.0001f;

            bool oddNegativeScale =
                math.chgsign(math.chgsign(math.chgsign(1.0f, scale.x), scale.y), scale.z) < 0.0f;

            // If there are any scale components different or if the scale is odd negative, then it's non-uniform
            bool nonUniformScale = math.any(math.abs(scale - scale.yzx) > epsilon) | oddNegativeScale;

            // The scale is uniform if it's not identity scale, but also if it's non-uniform then it can't be uniform.
            bool uniformScale = math.all(math.abs(scale - 1.0f) > epsilon) & !nonUniformScale;

            int transformType = 0;
            if (oddNegativeScale) transformType |= (int)TransformType.kOddNegativeScaleTransform;
            if (nonUniformScale)  transformType |= (int)TransformType.kNonUniformScaleTransform;
            if (uniformScale)     transformType |= (int)TransformType.kUniformScaleTransform;
            return (byte)transformType;
        }
    }
}
