// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Animations
{
    /// <summary>
    /// Buffer containing the animated data for a rig.
    /// All transform, float, integer, and quaternion data is packed in this buffer.
    /// To read or write to it, use <see cref="AnimationStream"/>.
    /// </summary>
    public struct AnimatedData
    {
        internal float Value;
    }

    /// <summary>
    /// Buffer containing the weighted values of a weight mask.
    /// To read or write to it, use <see cref="AnimationStreamMask"/>.
    /// </summary>
    public struct WeightData
    {
        internal float Value;
    }
}
