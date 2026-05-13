// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    // Single place where the Length-pair sub-channel arithmetic lives.
    // Length is a (float value, LengthUnit unit) composite that appears as two animation
    // channels in several composites (Length itself, BackgroundPosition.offset,
    // BackgroundSize.x / .y). This helper removes the duplicated channel-index switches
    // from UIAnimationBinder.cs and from the recording bridge; callers only need to know
    // the sub-channel index (0 = value, 1 = unit), never the per-PropertyType layout.
    internal static class AnimationLengthChannel
    {
        // Total number of flat channels a LengthBlock contributes: one float value +
        // one discrete unit. Mirrors SubChannelGroup.LengthBlock expansion in the
        // UIElementsGenerator.
        public const int ChannelCount = 2;

        // Index of the .value sub-channel within a LengthBlock.
        public const int ValueSubChannel = 0;
        // Index of the .unit sub-channel within a LengthBlock.
        public const int UnitSubChannel = 1;

        // Reads a single Length sub-channel as the float representation used by the
        // animation curve. The .unit sub-channel is bit-cast from the LengthUnit int
        // value so integer enum values survive round-trip through float storage.
        public static float ReadFloat(in Length length, int subChannel)
        {
            return subChannel switch
            {
                ValueSubChannel => length.value,
                UnitSubChannel => BitConverter.Int32BitsToSingle((int)length.unit),
                _ => throw new ArgumentOutOfRangeException(nameof(subChannel)),
            };
        }

        // Produces a new Length with the given sub-channel replaced. The incoming
        // `value` for the unit sub-channel is the bit-cast of a LengthUnit int; this
        // mirrors the encoding used by Write in AnimationClip curves.
        public static Length Write(in Length length, int subChannel, float value)
        {
            return subChannel switch
            {
                ValueSubChannel => new Length(value, length.unit),
                UnitSubChannel => new Length(length.value, (LengthUnit)BitConverter.SingleToInt32Bits(value)),
                _ => throw new ArgumentOutOfRangeException(nameof(subChannel)),
            };
        }
    }
}
